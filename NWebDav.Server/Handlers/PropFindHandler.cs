using Microsoft.Extensions.Logging;
using NWebDav.Server.Enums;
using NWebDav.Server.Helpers;
using NWebDav.Server.Http;
using NWebDav.Server.Props;
using NWebDav.Server.Stores;
using SecureFolderFS.Sdk.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NWebDav.Server.Handlers
{
    /// <summary>
    /// Implementation of the PROPFIND method.
    /// </summary>
    /// <remarks>
    /// The specification of the WebDAV PROPFIND method can be found in the
    /// <see href="http://www.webdav.org/specs/rfc2518.html#METHOD_PROPFIND">
    /// WebDAV specification
    /// </see>.
    /// </remarks>
    public sealed class PropFindHandler : IRequestHandler
    {
        private struct PropertyEntry
        {
            public Uri Uri { get; }
            public IStoreItem Entry { get; }

            public PropertyEntry(Uri uri, IStoreItem entry)
            {
                Uri = uri;
                Entry = entry;
            }
        }

        [Flags]
        private enum PropertyMode
        {
            None = 0,
            PropertyNames = 1,
            AllProperties = 2,
            SelectedProperties = 4
        }

        /// <summary>
        /// Handle a PROPFIND request.
        /// </summary>
        /// <inheritdoc/>
        public async Task HandleRequestAsync(IHttpContext context, IStore store, IStorageService storageService, ILogger? logger = null, CancellationToken cancellationToken = default)
        {
            // Obtain request and response
            var request = context.Request;
            var response = context.Response;

            // Determine the list of properties that need to be obtained
            var propertyList = new List<XName>();
            var propertyMode = await GetRequestedPropertiesAsync(request, propertyList, logger, cancellationToken).ConfigureAwait(false);

            // Generate the list of items from which we need to obtain the properties
            var entries = new List<PropertyEntry>();

            // Obtain entry
            var topEntry = await store.GetItemAsync(request.Url, context).ConfigureAwait(false);
            if (topEntry == null)
            {
                response.SetStatus(HttpStatusCode.NotFound);
                return;
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
                        case EnumerationDepthMode.Rejected:
                            response.SetStatus(HttpStatusCode.Forbidden, "Not allowed to obtain properties with infinite depth.");
                            return;

                        case EnumerationDepthMode.Assume0:
                            depth = 0;
                            break;

                        case EnumerationDepthMode.Assume1:
                            depth = 1;
                            break;
                    }
                }

                // Add all the entries
                await AddEntriesAsync(topCollection, depth, context, request.Url, entries).ConfigureAwait(false);
            }
            else
            {
                // It should be an item, so just use this item
                entries.Add(new PropertyEntry(request.Url, topEntry));
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
                                await AddPropertyAsync(context, xResponse, xPropStatValues, propertyManager, entry.Entry, propertyName, addedProperties, logger).ConfigureAwait(false);
                        }

                        if ((propertyMode & PropertyMode.SelectedProperties) != 0)
                        {
                            foreach (var propertyName in propertyList)
                                await AddPropertyAsync(context, xResponse, xPropStatValues, propertyManager, entry.Entry, propertyName, addedProperties, logger).ConfigureAwait(false);
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
            await response.SendResponseAsync(HttpStatusCode.MultiStatus, xDocument, logger, cancellationToken).ConfigureAwait(false);
        }

        private async Task AddPropertyAsync(IHttpContext context, XElement xResponse, XElement xPropStatValues, IPropertyManager propertyManager, IStoreItem item, XName propertyName, IList<XName> addedProperties, ILogger? logger)
        {
            if (!addedProperties.Contains(propertyName))
            {
                addedProperties.Add(propertyName);
                try
                {
                    // Check if the property is supported
                    if (propertyManager.Properties.Any(p => p.Name == propertyName))
                    {
                        var value = await propertyManager.GetPropertyAsync(context, item, propertyName).ConfigureAwait(false);
                        if (value is IEnumerable<XElement>)
                            value = ((IEnumerable<XElement>) value).Cast<object>().ToArray();

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
                        logger?.LogWarning($"Property {propertyName} is not supported on item {item.Name}.");
                        xResponse.Add(new XElement(WebDavNamespaces.DavNs + "propstat",
                            new XElement(WebDavNamespaces.DavNs + "prop", new XElement(propertyName, null)),
                            new XElement(WebDavNamespaces.DavNs + "status", "HTTP/1.1 404 Not Found"),
                            new XElement(WebDavNamespaces.DavNs + "responsedescription", $"Property {propertyName} is not supported.")));
                    }
                }
                catch (Exception ex)
                {
                    logger?.LogError($"Property {propertyName} on item {item.Name} raised an exception.", ex);
                    xResponse.Add(new XElement(WebDavNamespaces.DavNs + "propstat",
                        new XElement(WebDavNamespaces.DavNs + "prop", new XElement(propertyName, null)),
                        new XElement(WebDavNamespaces.DavNs + "status", "HTTP/1.1 500 Internal server error"),
                        new XElement(WebDavNamespaces.DavNs + "responsedescription", $"Property {propertyName} on item {item.Name} raised an exception.")));
                }
            }
        }

        private static async Task<PropertyMode> GetRequestedPropertiesAsync(IHttpRequest request, ICollection<XName> properties, ILogger? logger, CancellationToken cancellationToken)
        {
            // Create an XML document from the stream
            var xDocument = await request.LoadXmlDocumentAsync(logger, cancellationToken).ConfigureAwait(false);
            if (xDocument == null || xDocument?.Root == null || xDocument.Root.Name != WebDavNamespaces.DavNs + "propfind")
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

        private async Task AddEntriesAsync(IStoreCollection collection, int depth, IHttpContext context, Uri uri, IList<PropertyEntry> entries)
        {
            // Add the collection to the list
            entries.Add(new PropertyEntry(uri, collection));

            // If we have enough depth, then add the children
            if (depth > 0)
            {
                // Add all child collections
                foreach (var childEntry in await collection.GetItemsAsync(context).ConfigureAwait(false))
                {
                    var subUri = UriHelper.Combine(uri, childEntry.Name);
                    if (childEntry is IStoreCollection subCollection)
                        await AddEntriesAsync(subCollection, depth - 1, context, subUri, entries).ConfigureAwait(false);
                    else
                        entries.Add(new PropertyEntry(subUri, childEntry));
                }
            }
        }
    }
}



