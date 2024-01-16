using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using NWebDav.Server.Helpers;
using NWebDav.Server.Stores;

namespace NWebDav.Server.Handlers;

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
        private readonly List<PropSet> _propertySetters = new();

        public record PropSet(XName Name, object Value)
        {
            public DavStatusCode Result { get; set; }

            public XElement GetXmlResponse() =>
                new XElement(WebDavNamespaces.DavNs + "propstat",
                    new XElement(WebDavNamespaces.DavNs + "prop", new XElement(Name)),
                    new XElement(WebDavNamespaces.DavNs + "status", $"HTTP/1.1 {(int)Result} {Result.GetStatusDescription()}"));
        }

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
                            newValue = xActualProperty.HasElements ? xActualProperty.Elements().FirstOrDefault() : xActualProperty.Value;
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
            foreach (var result in _propertySetters.Where(ps => ps.Result != DavStatusCode.Ok))
                xResponse.Add(result.GetXmlResponse());
            return xMultiStatus;
        }
    }

    private readonly IXmlReaderWriter _xmlReaderWriter;
    private readonly IStore _store;

    public PropPatchHandler(IXmlReaderWriter xmlReaderWriter, IStore store)
    {
        _xmlReaderWriter = xmlReaderWriter;
        _store = store;
    }

    /// <summary>
    /// Handle a PROPPATCH request.
    /// </summary>
    /// <param name="httpContext">
    /// The HTTP context of the request.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous PROPPATCH operation. The task
    /// will always return <see langword="true"/> upon completion.
    /// </returns>
    public async Task<bool> HandleRequestAsync(HttpContext httpContext)
    {
        // Obtain request and response
        var request = httpContext.Request;
        var response = httpContext.Response;

        // Obtain item
        var item = await _store.GetItemAsync(request.GetUri(), httpContext.RequestAborted).ConfigureAwait(false);
        if (item == null)
        {
            response.SetStatus(DavStatusCode.NotFound);
            return true;
        }

        // Read the property set/remove items from the request
        PropSetCollection propSetCollection;
        try
        {
            // Create an XML document from the stream
            var xDoc = await _xmlReaderWriter.LoadXmlDocumentAsync(request, httpContext.RequestAborted).ConfigureAwait(false);

            // Create an XML document from the stream
            propSetCollection = new PropSetCollection(xDoc.Root);
        }
        catch (Exception)
        {
            response.SetStatus(DavStatusCode.BadRequest);
            return true;
        }

        // Scan each property
        foreach (var propSet in propSetCollection)
        {
            // Set the property
            DavStatusCode result;
            try
            {
                result = await item.PropertyManager.SetPropertyAsync(item, propSet.Name, propSet.Value, httpContext.RequestAborted).ConfigureAwait(false);
            }
            catch (Exception)
            {
                result = DavStatusCode.Forbidden;
            }

            propSet.Result = result;
        }

        // Obtain the status document
        var xDocument = new XDocument(propSetCollection.GetXmlMultiStatus(request.GetUri()));

        // Stream the document
        await _xmlReaderWriter.SendResponseAsync(response, DavStatusCode.MultiStatus, xDocument).ConfigureAwait(false);
        return true;
    }
}
