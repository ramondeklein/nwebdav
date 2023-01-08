using NWebDav.Server.Helpers;
using NWebDav.Server.Http;
using NWebDav.Server.Stores;
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
    /// Implementation of the PROPPATCH method.
    /// </summary>
    /// <remarks>
    /// The specification of the WebDAV PROPFIND method can be found in the
    /// <see href="http://www.webdav.org/specs/rfc2518.html#METHOD_PROPPATCH">
    /// WebDAV specification
    /// </see>.
    /// </remarks>
    public class PropPatchHandler : IRequestHandler
    {
        private class PropSetCollection : List<PropSetCollection.PropSet>
        {
            public class PropSet
            {
                public XName Name { get; }
                public object Value { get; }
                public HttpStatusCode Result { get; set; }

                public PropSet(XName name, object value)
                {
                    Name = name;
                    Value = value;
                }

                public XElement GetXmlResponse()
                {
                    var statusText = $"HTTP/1.1 {(int)Result} {Result.ToString()}";
                    return new XElement(WebDavNamespaces.DavNs + "propstat",
                        new XElement(WebDavNamespaces.DavNs + "prop", new XElement(Name)),
                        new XElement(WebDavNamespaces.DavNs + "status", statusText));
                }
            }

            private readonly IList<PropSet> _propertySetters = new List<PropSet>();

            public PropSetCollection(XElement xPropertyUpdate)
            {
                // The document should contain a 'propertyupdate' root element
                if (xPropertyUpdate == null || xPropertyUpdate.Name != WebDavNamespaces.DavNs + "propertyupdate")
                    throw new Exception("Invalid root element (expected 'propertyupdate')");

                // Check all descendants
                foreach (var xElement in xPropertyUpdate.Elements())
                {
                    // The descendant should be a 'set' or 'remove' entry
                    if (xElement.Name != WebDavNamespaces.DavNs + "set" && xElement.Name != WebDavNamespaces.DavNs + "remove")
                        throw new Exception("Expected 'set' or 'remove' entry");

                    // Obtain the properties
                    foreach (var xProperty in xElement.Descendants(WebDavNamespaces.DavNs + "prop"))
                    {
                        // Determine the actual property element
                        var xActualProperty = xProperty.Elements().FirstOrDefault();
                        if (xActualProperty != null)
                        {
                            // Determine the new property value
                            object newValue;
                            if (xElement.Name == WebDavNamespaces.DavNs + "set")
                            {
                                // If the descendant is XML, then use the XElement, otherwise use the string
                                newValue = xActualProperty.HasElements ? (object)xActualProperty.Elements().FirstOrDefault() : xActualProperty.Value;
                            }
                            else
                            {
                                newValue = null;
                            }

                            // Add the property
                            _propertySetters.Add(new PropSet(xActualProperty.Name, newValue));
                        }
                    }
                }
            }

            public XElement GetXmlMultiStatus(Uri uri)
            {
                var xResponse = new XElement(WebDavNamespaces.DavNs + "response", new XElement(WebDavNamespaces.DavNs + "href", UriHelper.ToEncodedString(uri)));
                var xMultiStatus = new XElement(WebDavNamespaces.DavNs + "multistatus", xResponse);
                foreach (var result in _propertySetters.Where(ps => ps.Result != HttpStatusCode.OK))
                    xResponse.Add(result.GetXmlResponse());
                return xMultiStatus;
            }
        }

        /// <summary>
        /// Handle a PROPPATCH request.
        /// </summary>
        /// <inheritdoc/>
        public async Task<bool> HandleRequestAsync(IHttpContext context, IStore store, CancellationToken cancellationToken = default)
        {
            // Obtain request and response
            var request = context.Request;
            var response = context.Response;

            // Obtain item
            var item = await store.GetItemAsync(request.Url, context).ConfigureAwait(false);
            if (item == null)
            {
                response.SetStatus(HttpStatusCode.NotFound);
                return true;
            }

            // Read the property set/remove items from the request
            PropSetCollection propSetCollection;
            try
            {
                // Create an XML document from the stream
                var xDoc = await request.LoadXmlDocumentAsync().ConfigureAwait(false);

                // Create an XML document from the stream
                propSetCollection = new PropSetCollection(xDoc.Root);
            }
            catch (Exception)
            {
                response.SetStatus(HttpStatusCode.BadRequest);
                return true;
            }

            // Scan each property
            foreach (var propSet in propSetCollection)
            {
                // Set the property
                HttpStatusCode result;
                try
                {
                    result = await item.PropertyManager.SetPropertyAsync(context, item, propSet.Name, propSet.Value).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    result = HttpStatusCode.Forbidden;
                }

                propSet.Result = result;
            }

            // Obtain the status document
            var xDocument = new XDocument(propSetCollection.GetXmlMultiStatus(request.Url));

            // Stream the document
            await response.SendResponseAsync(HttpStatusCode.MultiStatus, xDocument).ConfigureAwait(false);
            return true;
        }
    }
}
