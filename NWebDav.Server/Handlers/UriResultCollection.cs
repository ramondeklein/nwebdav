using NWebDav.Server.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Xml.Linq;

namespace NWebDav.Server.Handlers
{
    internal class UriResultCollection
    {
        private struct UriResult
        {
            private Uri Uri { get; }
            private HttpStatusCode Result { get; }

            public UriResult(Uri uri, HttpStatusCode result)
            {
                Uri = uri;
                Result = result;
            }

            public XElement GetXmlResponse()
            {
                var statusText = $"HTTP/1.1 {(int)Result} {Result.ToString()}";
                return new XElement(WebDavNamespaces.DavNs + "response",
                    new XElement(WebDavNamespaces.DavNs + "href", UriHelper.ToEncodedString(Uri)),
                    new XElement(WebDavNamespaces.DavNs + "status", statusText));
            }
        }

        private readonly IList<UriResult> _results = new List<UriResult>();

        public bool HasItems => _results.Any();

        public void AddResult(Uri uri, HttpStatusCode result)
        {
            _results.Add(new UriResult(uri, result));
        }

        public XElement GetXmlMultiStatus()
        {
            var xMultiStatus = new XElement(WebDavNamespaces.DavNs + "multistatus");
            foreach (var result in _results)
                xMultiStatus.Add(result.GetXmlResponse());
            return xMultiStatus;
        }
    }
}
