using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NWebDav.Server.Helpers;
using NWebDav.Server.Props;
using NWebDav.Server.Stores;

namespace NWebDav.Server.Handlers;

/// <summary>
/// Implementation of the PROPFIND method.
/// </summary>
/// <remarks>
/// The specification of the WebDAV PROPFIND method can be found in the
/// <see href="http://www.webdav.org/specs/rfc2518.html#METHOD_PROPFIND">
/// WebDAV specification
/// </see>.
/// </remarks>
public class PropFindHandler : IRequestHandler
{
    private readonly IXmlReaderWriter _xmlReaderWriter;
    private readonly IStore _store;
    private readonly ILogger<PropFindHandler> _logger;

    private readonly record struct PropertyEntry(Uri Uri, IStoreItem Entry);

    [Flags]
    private enum PropertyMode
    {
        None = 0,
        PropertyNames = 1,
        AllProperties = 2,
        SelectedProperties = 4
    }

    public PropFindHandler(IXmlReaderWriter xmlReaderWriter, IStore store, ILogger<PropFindHandler> logger)
    {
        _xmlReaderWriter = xmlReaderWriter;
        _store = store;
        _logger = logger;
    }

    /// <summary>
    /// Handle a PROPFIND request.
    /// </summary>
    /// <param name="httpContext">
    /// The HTTP context of the request.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous PROPFIND operation. The task
    /// will always return <see langword="true"/> upon completion.
    /// </returns>
    public async Task<bool> HandleRequestAsync(HttpContext httpContext)
    {
        // Obtain request and response
        var request = httpContext.Request;
        var response = httpContext.Response;

        // Determine the list of properties that need to be obtained
        var propertyList = new List<XName>();
        var propertyMode = await GetRequestedPropertiesAsync(request, propertyList, httpContext.RequestAborted).ConfigureAwait(false);

        // Generate the list of items from which we need to obtain the properties
        var entries = new List<PropertyEntry>();

        // Obtain entry
        var topEntry = await _store.GetItemAsync(request.GetUri(), httpContext.RequestAborted).ConfigureAwait(false);
        if (topEntry == null)
        {
            response.SetStatus(DavStatusCode.NotFound);
            return true;
        }

        // Check if the entry is a collection
        if (topEntry is IStoreCollection topCollection)
        {
            // Determine depth
            var depth = request.GetDepth();

            // Check if the collection supports Infinite depth for properties
            if (depth > 1)
            {
                switch (topCollection.InfiniteDepthMode)
                {
                    case InfiniteDepthMode.Rejected:
                        response.SetStatus(DavStatusCode.Forbidden, "Not allowed to obtain properties with infinite depth.");
                        return true;
                    case InfiniteDepthMode.Assume0:
                        depth = 0;
                        break;
                    case InfiniteDepthMode.Assume1:
                        depth = 1;
                        break;
                }
            }

            // Add all the entries
            await AddEntriesAsync(topCollection, depth, request.GetUri(), entries, httpContext.RequestAborted).ConfigureAwait(false);
        }
        else
        {
            // It should be an item, so just use this item
            entries.Add(new PropertyEntry(request.GetUri(), topEntry));
        }

        // Obtain the status document
        var xMultiStatus = new XElement(WebDavNamespaces.DavNs + "multistatus");
        var xDocument = new XDocument(xMultiStatus);

        // Add all the properties
        foreach (var entry in entries)
        {
            // Create the property
            var xResponse = new XElement(WebDavNamespaces.DavNs + "response",
                new XElement(WebDavNamespaces.DavNs + "href", UriHelper.ToEncodedString(entry.Uri)));

            // Create tags for property values
            var xPropStatValues = new XElement(WebDavNamespaces.DavNs + "propstat");

            // Check if the entry supports properties
            var propertyManager = entry.Entry.PropertyManager;
            if (propertyManager != null)
            {
                // Handle based on the property mode
                if (propertyMode == PropertyMode.PropertyNames)
                {
                    // Add all properties
                    foreach (var property in propertyManager.Properties)
                        xPropStatValues.Add(new XElement(property.Name));

                    // Add the values
                    xResponse.Add(xPropStatValues);
                }
                else
                {
                    var addedProperties = new List<XName>();
                    if ((propertyMode & PropertyMode.AllProperties) != 0)
                    {
                        foreach (var propertyName in propertyManager.Properties.Where(p => !p.IsExpensive).Select(p => p.Name))
                            await AddPropertyAsync(xResponse, xPropStatValues, propertyManager, entry.Entry, propertyName, addedProperties, httpContext.RequestAborted).ConfigureAwait(false);
                    }

                    if ((propertyMode & PropertyMode.SelectedProperties) != 0)
                    {
                        foreach (var propertyName in propertyList)
                            await AddPropertyAsync(xResponse, xPropStatValues, propertyManager, entry.Entry, propertyName, addedProperties, httpContext.RequestAborted).ConfigureAwait(false);
                    }

                    // Add the values (if any)
                    if (xPropStatValues.HasElements)
                        xResponse.Add(xPropStatValues);
                }
            }

            // Add the status
            xPropStatValues.Add(new XElement(WebDavNamespaces.DavNs + "status", "HTTP/1.1 200 OK"));

            // Add the property
            xMultiStatus.Add(xResponse);
        }

        // Stream the document
        await _xmlReaderWriter.SendResponseAsync(response, DavStatusCode.MultiStatus, xDocument).ConfigureAwait(false);

        // Finished writing
        return true;
    }

    private async Task AddPropertyAsync(XElement xResponse, XElement xPropStatValues, IPropertyManager propertyManager, IStoreItem item, XName propertyName, List<XName> addedProperties, CancellationToken cancellationToken)
    {
        if (!addedProperties.Contains(propertyName))
        {
            addedProperties.Add(propertyName);
            try
            {
                // Check if the property is supported
                if (propertyManager.Properties.Any(p => p.Name == propertyName))
                {
                    var value = await propertyManager.GetPropertyAsync(item, propertyName, cancellationToken: cancellationToken).ConfigureAwait(false);
                    if (value is IEnumerable<XElement> elements)
                        value = elements.Cast<object>().ToArray();

                    // Make sure we use the same 'prop' tag to add all properties
                    var xProp = xPropStatValues.Element(WebDavNamespaces.DavNs + "prop");
                    if (xProp == null)
                    {
                        xProp = new XElement(WebDavNamespaces.DavNs + "prop");
                        xPropStatValues.Add(xProp);
                    }

                    xProp.Add(new XElement(propertyName, value));
                }
                else
                {
                    _logger.LogWarning($"Property {propertyName} is not supported on item {item.Name}.");
                    xResponse.Add(new XElement(WebDavNamespaces.DavNs + "propstat",
                        new XElement(WebDavNamespaces.DavNs + "prop", new XElement(propertyName)),
                        new XElement(WebDavNamespaces.DavNs + "status", "HTTP/1.1 404 Not Found"),
                        new XElement(WebDavNamespaces.DavNs + "responsedescription", $"Property {propertyName} is not supported.")));
                }
            }
            catch (Exception exc)
            {
                _logger.LogError(exc, $"Property {propertyName} on item {item.Name} raised an exception.");
                xResponse.Add(new XElement(WebDavNamespaces.DavNs + "propstat",
                    new XElement(WebDavNamespaces.DavNs + "prop", new XElement(propertyName)),
                    new XElement(WebDavNamespaces.DavNs + "status", "HTTP/1.1 500 Internal server error"),
                    new XElement(WebDavNamespaces.DavNs + "responsedescription", $"Property {propertyName} on item {item.Name} raised an exception.")));
            }
        }
    }

    private async Task<PropertyMode> GetRequestedPropertiesAsync(HttpRequest request, List<XName> properties, CancellationToken cancellationToken)
    {
        // Create an XML document from the stream
        var xDocument = await _xmlReaderWriter.LoadXmlDocumentAsync(request, cancellationToken).ConfigureAwait(false);
        if (xDocument?.Root == null || xDocument.Root.Name != WebDavNamespaces.DavNs + "propfind")
            return PropertyMode.AllProperties;

        // Obtain the propfind node
        var xPropFind = xDocument.Root;

        // If there is no child-node, then return all properties
        var xProps = xPropFind.Elements();
        if (!xProps.Any())
            return PropertyMode.AllProperties;

        // Add all entries to the list
        var propertyMode = PropertyMode.None;
        foreach (var xProp in xPropFind.Elements())
        {
            // Check if we should fetch all property names
            if (xProp.Name == WebDavNamespaces.DavNs + "propname")
            {
                propertyMode = PropertyMode.PropertyNames;
            }
            else if (xProp.Name == WebDavNamespaces.DavNs + "allprop")
            {
                propertyMode = PropertyMode.AllProperties;
            }
            else if (xProp.Name == WebDavNamespaces.DavNs + "include")
            {
                // Include properties
                propertyMode = PropertyMode.AllProperties | PropertyMode.SelectedProperties;

                // Include all specified properties
                foreach (var xSubProp in xProp.Elements())
                    properties.Add(xSubProp.Name);
            }
            else
            {
                propertyMode = PropertyMode.SelectedProperties;

                // Include all specified properties
                foreach (var xSubProp in xProp.Elements())
                    properties.Add(xSubProp.Name);
            }
        }

        return propertyMode;
    }

    private async Task AddEntriesAsync(IStoreCollection collection, int depth, Uri uri, IList<PropertyEntry> entries, CancellationToken cancellationToken)
    {
        // Add the collection to the list
        entries.Add(new PropertyEntry(uri, collection));

        // If we have enough depth, then add the children
        if (depth > 0)
        {
            // Add all child collections
            await foreach (var childEntry in collection.GetItemsAsync(cancellationToken).ConfigureAwait(false))
            {
                var subUri = UriHelper.Combine(uri, childEntry.Name);
                if (childEntry is IStoreCollection subCollection)
                    await AddEntriesAsync(subCollection, depth - 1, subUri, entries, cancellationToken).ConfigureAwait(false);
                else
                    entries.Add(new PropertyEntry(subUri, childEntry));
            }
        }
    }
}
