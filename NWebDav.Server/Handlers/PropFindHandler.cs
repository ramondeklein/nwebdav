using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Xml.Linq;
using NWebDav.Server.Handlers;
using NWebDav.Server.Helpers;
using NWebDav.Server.Props;

namespace NWebDav.Server.Handlers
{
    [Verb("PROPFIND")]
    public class PropFindHandler : IRequestHandler
    {
        private struct PropertyEntry
        {
            public Uri Uri { get; }
            public IStoreCollectionEntry Entry { get; }

            public PropertyEntry(Uri uri, IStoreCollectionEntry entry)
            {
                Uri = uri;
                Entry = entry;
            }
        }

        [Flags]
        public enum PropertyMode
        {
            None = 0,
            PropertyNames = 1,
            AllProperties = 2,
            SelectedProperties = 4
        }

        public async Task<bool> HandleRequestAsync(HttpListenerContext httpListenerContext, IStoreResolver storeResolver)
        {
            // Obtain request and response
            var request = httpListenerContext.Request;
            var response = httpListenerContext.Response;
            var principal = httpListenerContext.User;

            // Determine the list of properties that need to be obtained
            var propertyList = new List<XName>();
            var propertyMode = GetRequestedProperties(request, propertyList);

            // Generate the list of items from which we need to obtain the properties
            var entries = new List<PropertyEntry>();

            // Obtain collection
            var collection = await storeResolver.GetCollectionAsync(request.Url, principal).ConfigureAwait(false);
            if (collection != null)
            {
                // Determine depth
                var depth = request.GetDepth();

                // Check if the collection supports Infinite depth for properties
                if (depth > 1 && !collection.AllowInfiniteDepthProperties)
                {
                    response.SendResponse(DavStatusCode.Forbidden, "Not allowed to obtain properties with infinite depth.");
                    return true;
                }

                // Add all the entries
                await AddEntriesAsync(collection, depth, principal, request.Url, entries).ConfigureAwait(false);
            }
            else
            {
                // Find the item
                var item = await storeResolver.GetItemAsync(request.Url, principal).ConfigureAwait(false);
                if (item == null)
                {
                    response.SendResponse(DavStatusCode.NotFound);
                    return true;
                }

                // Add the item to the list
                entries.Add(new PropertyEntry(request.Url, item));
            }

            // Obtain the status document
            var xMultiStatus = new XElement(WebDavNamespaces.DavNs + "multistatus");
            var xDocument = new XDocument(xMultiStatus);

            // Add all the properties
            foreach (var entry in entries)
            {
                // Create the property
                var xResponse = new XElement(WebDavNamespaces.DavNs + "response",
                    new XElement(WebDavNamespaces.DavNs + "href", entry.Uri.AbsoluteUri));

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
                        var xPropStatErrors = new XElement(WebDavNamespaces.DavNs + "propstat");
                        var addedProperties = new List<XName>();
                        if ((propertyMode & PropertyMode.AllProperties) != 0)
                        {
                            foreach (var propertyName in propertyManager.Properties.Where(p => !p.IsExpensive).Select(p => p.Name))
                                AddProperty(xPropStatValues, xPropStatErrors, propertyManager, entry.Entry, propertyName, addedProperties);
                        }

                        if ((propertyMode & PropertyMode.SelectedProperties) != 0)
                        {
                            foreach (var propertyName in propertyList)
                                AddProperty(xPropStatValues, xPropStatErrors, propertyManager, entry.Entry, propertyName, addedProperties);
                        }

                        // Add the values (if any)
                        if (xPropStatValues.HasElements)
                            xResponse.Add(xPropStatValues);

                        // Add the errors (if any)
                        if (xPropStatErrors.HasElements)
                            xResponse.Add(xPropStatValues);
                    }
                }

                // Add the property
                xMultiStatus.Add(xResponse);
            }

            // Stream the document
            await response.SendResponseAsync(DavStatusCode.MultiStatus, xDocument).ConfigureAwait(false);

            // Finished writing
            return true;
        }

        private void AddProperty(XElement xPropStatValues, XElement xPropStatErrors, IPropertyManager propertyManager, IStoreCollectionEntry entry, XName propertyName, IList<XName> addedProperties)
        {
            if (!addedProperties.Contains(propertyName))
            {
                try
                {
                    addedProperties.Add(propertyName);
                    var value = propertyManager.GetProperty(entry, propertyName);
                    xPropStatValues.Add(new XElement(propertyName, value));
                }
                catch (Exception)
                {
                    // TODO
                }
            }
        }

        private PropertyMode GetRequestedProperties(HttpListenerRequest request, IList<XName> properties)
        {
            // If there is no input stream, then request all properties
            if (request.InputStream == null || request.InputStream == Stream.Null)
                return PropertyMode.AllProperties;

            // Create an XML document from the stream
            var xDocument = XDocument.Load(request.InputStream);
            if (xDocument.Root == null || xDocument.Root.Name != WebDavNamespaces.DavNs + "propfind")
            {
                // TODO: Log
                return PropertyMode.AllProperties;
            }

            // Obtain the propfind node
            var xPropFind = xDocument.Root;

            // If there is no child-node, then return all properties
            var xProps = xPropFind.Descendants();
            if (!xProps.Any())
                return PropertyMode.AllProperties;

            // Add all entries to the list
            var propertyMode = PropertyMode.None;
            foreach (var xProp in xPropFind.Descendants())
            {
                // Check if we should fetch all property names
                if (xProp.Name == WebDavNamespaces.DavNs + "propname")
                {
                    propertyMode = PropertyMode.PropertyNames;
                }
                else if (xProp.Name == WebDavNamespaces.DavNs + "propall")
                {
                    propertyMode = PropertyMode.AllProperties;
                }
                else if (xProp.Name == WebDavNamespaces.DavNs + "include")
                {
                    // Include properties
                    propertyMode = PropertyMode.AllProperties | PropertyMode.SelectedProperties;

                    // Include all specified properties
                    foreach (var xSubProp in xPropFind.Descendants())
                        properties.Add(xSubProp.Name);
                }
                else
                {
                    propertyMode = PropertyMode.SelectedProperties;
                    properties.Add(xProp.Name);
                }
            }

            return propertyMode;
        }

        private async Task AddEntriesAsync(IStoreCollection collection, int depth, IPrincipal principal, Uri uri, IList<PropertyEntry> entries)
        {
            // Add the collection to the list
            entries.Add(new PropertyEntry(uri, collection));

            // If we have enough depth, then add the childs
            if (depth > 0)
            {
                // Add all child collections
                foreach (var childEntry in await collection.GetEntriesAsync(principal).ConfigureAwait(false))
                {
                    var subUri = new Uri(uri, childEntry.Name);
                    var subCollection = childEntry as IStoreCollection;
                    if (subCollection != null)
                        await AddEntriesAsync(subCollection, depth - 1, principal, subUri, entries).ConfigureAwait(false);
                    else
                        entries.Add(new PropertyEntry(subUri, childEntry));
                }
            }
        }
    }
}



