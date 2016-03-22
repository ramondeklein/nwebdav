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
    [Verb("PROPPATCH")]
    public class PropPatchHandler : IRequestHandler
    {
        public class PropertyResultCollection
        {
            private struct PropertyResult
            {
                public XName Name { get; }
                public DavStatusCode Result { get; }

                public PropertyResult(XName name, DavStatusCode result)
                {
                    Name = name;
                    Result = result;
                }

                public XElement GetXmlResponse()
                {
                    var statusText = $"HTTP/1.1 {(int)Result} {ResponseHelper.GetStatusDescription(Result)}";
                    return new XElement(WebDavNamespaces.DavNs + "propstat",
                        new XElement(WebDavNamespaces.DavNs + "prop", new XElement(Name)),
                        new XElement(WebDavNamespaces.DavNs + "status", statusText));
                }
            }

            private readonly IList<PropertyResult> _results = new List<PropertyResult>();

            public bool HasItems => _results.Any();

            public void AddResult(XName name, DavStatusCode result)
            {
                _results.Add(new PropertyResult(name, result));
            }

            public XElement GetXmlMultiStatus(Uri uri)
            {
                var xResponse = new XElement(WebDavNamespaces.DavNs + "response", new XElement(WebDavNamespaces.DavNs + "href", uri));
                var xMultiStatus = new XElement(WebDavNamespaces.DavNs + "multistatus", xResponse);
                foreach (var result in _results)
                    xResponse.Add(result.GetXmlResponse());
                return xMultiStatus;
            }
        }

        public async Task<bool> HandleRequestAsync(HttpListenerContext httpListenerContext, IStoreResolver storeResolver)
        {
            // Obtain request and response
            var request = httpListenerContext.Request;
            var response = httpListenerContext.Response;
            var principal = httpListenerContext.User;

            // Obtain item
            var item = await storeResolver.GetItemAsync(request.Url, principal).ConfigureAwait(false);
            if (item == null)
            {
                response.SendResponse(DavStatusCode.NotFound);
                return true;
            }

            // Obtain the root element
            XElement xRoot;
            try
            {
                // Create an XML document from the stream
                var xRequestDocument = XDocument.Load(request.InputStream);

                // The document should contain a 'propertyupdate' element
                if (xRequestDocument.Root?.Name != WebDavNamespaces.DavNs + "propertyupdate")
                    throw new Exception("Invalid root element");

                // Save the root document
                xRoot = xRequestDocument.Root;
            }
            catch (Exception)
            {
                response.SendResponse(DavStatusCode.BadRequest);
                return true;
            }

            // Scan each property
            var errors = new PropertyResultCollection();
            foreach (var xElement in xRoot.Descendants())
            {
                // Determine the property and value
                if (xElement.Name == WebDavNamespaces.DavNs + "set" || xElement.Name == WebDavNamespaces.DavNs + "remove")
                {
                    // Obtain the properties
                    foreach (var xProperty in xElement.Descendants(WebDavNamespaces.DavNs + "prop"))
                    {
                        // Determine the actual property element
                        var xActualProperty = xProperty.Descendants().FirstOrDefault();
                        if (xActualProperty != null)
                        {
                            // Determine the property name
                            var propName = xActualProperty.Name;

                            // Determine the new property value
                            object newValue;
                            if (xElement.Name == WebDavNamespaces.DavNs + "set")
                            {
                                // If the descendant is XML, then use the XElement, otherwise use the string
                                newValue = xActualProperty.HasElements ? (object)xActualProperty.Descendants().FirstOrDefault() : xActualProperty.Value;
                            }
                            else
                            {
                                newValue = null;
                            }

                            // Set the property
                            DavStatusCode result;
                            try
                            {
                                result = item.PropertyManager.SetProperty(item, propName, newValue);
                            }
                            catch (Exception)
                            {
                                result = DavStatusCode.Forbidden;
                            }

                            // Log the error
                            if (result != DavStatusCode.OK)
                                errors.AddResult(propName, result);
                        }
                    }
                }
            }

            // Obtain the status document
            var xDocument = new XDocument(errors.GetXmlMultiStatus(request.Url));

            // Stream the document
            await response.SendResponseAsync(DavStatusCode.MultiStatus, xDocument).ConfigureAwait(false);
            return true;
        }
    }
}



