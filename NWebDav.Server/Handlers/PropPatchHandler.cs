using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Linq;
using NWebDav.Server.Helpers;
using NWebDav.Server.Stores;

namespace NWebDav.Server.Handlers
{
    [Verb("PROPPATCH")]
    public class PropPatchHandler : IRequestHandler
    {
        private class PropSetCollection : List<PropSetCollection.PropSet>
        {
            public class PropSet
            {
                public XName Name { get; }
                public object Value { get; }
                public DavStatusCode Result { get; set; }

                public PropSet(XName name, object value)
                {
                    Name = name;
                    Value = value;
                }

                public XElement GetXmlResponse()
                {
                    var statusText = $"HTTP/1.1 {(int)Result} {EnumHelper.GetEnumValue(Result)}";
                    return new XElement(WebDavNamespaces.DavNs + "propstat",
                        new XElement(WebDavNamespaces.DavNs + "prop", new XElement(Name)),
                        new XElement(WebDavNamespaces.DavNs + "status", statusText));
                }
            }

            public IList<PropSet> PropertySetters { get; } = new List<PropSet>();

            public PropSetCollection(Stream stream)
            {
                // Create an XML document from the stream
                var xDoc = XDocument.Load(stream);

                // The document should contain a 'propertyupdate' root element
                var xRoot = xDoc.Root;
                if (xRoot?.Name != WebDavNamespaces.DavNs + "propertyupdate")
                    throw new Exception("Invalid root element (expected 'propertyupdate')");
                
                // Check all descendants
                foreach (var xElement in xRoot.Descendants())
                {
                    // The descendant should be a 'set' or 'remove' entry
                    if (xElement.Name != WebDavNamespaces.DavNs + "set" && xElement.Name != WebDavNamespaces.DavNs + "remove")
                        throw new Exception("Expected 'set' or 'remove' entry");

                    // Obtain the properties
                    foreach (var xProperty in xElement.Descendants(WebDavNamespaces.DavNs + "prop"))
                    {
                        // Determine the actual property element
                        var xActualProperty = xProperty.Descendants().FirstOrDefault();
                        if (xActualProperty != null)
                        {
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

                            // Add the property
                            PropertySetters.Add(new PropSet(xActualProperty.Name, newValue));
                        }
                    }
                }
            }

            public XElement GetXmlMultiStatus(Uri uri)
            {
                var xResponse = new XElement(WebDavNamespaces.DavNs + "response", new XElement(WebDavNamespaces.DavNs + "href", uri));
                var xMultiStatus = new XElement(WebDavNamespaces.DavNs + "multistatus", xResponse);
                foreach (var result in PropertySetters.Where(ps => ps.Result != DavStatusCode.OK))
                    xResponse.Add(result.GetXmlResponse());
                return xMultiStatus;
            }
        }

        public async Task<bool> HandleRequestAsync(HttpListenerContext httpListenerContext, IStore store)
        {
            // Obtain request and response
            var request = httpListenerContext.Request;
            var response = httpListenerContext.Response;
            var principal = httpListenerContext.User;

            // Obtain item
            var item = await store.GetItemAsync(request.Url, principal).ConfigureAwait(false);
            if (item == null)
            {
                response.SendResponse(DavStatusCode.NotFound);
                return true;
            }

            // Read the property set/remove items from the request
            PropSetCollection propSetCollection;
            try
            {
                // Create an XML document from the stream
                propSetCollection = new PropSetCollection(request.InputStream);
            }
            catch (Exception)
            {
                response.SendResponse(DavStatusCode.BadRequest);
                return true;
            }

            // Scan each property
            foreach (var propSet in propSetCollection)
            {
                // Set the property
                DavStatusCode result;
                try
                {
                    result = item.PropertyManager.SetProperty(item, propSet.Name, propSet.Value);
                }
                catch (Exception)
                {
                    result = DavStatusCode.Forbidden;
                }

                propSet.Result = result;
            }

            // Obtain the status document
            var xDocument = new XDocument(propSetCollection.GetXmlMultiStatus(request.Url));

            // Stream the document
            await response.SendResponseAsync(DavStatusCode.MultiStatus, xDocument).ConfigureAwait(false);
            return true;
        }
    }
}



