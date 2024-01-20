using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using NWebDav.Server.Helpers;

namespace NWebDav.Server.Handlers;

internal class UriResultCollection
{
    private readonly record struct UriResult(Uri Uri, DavStatusCode Result)
    {
        public XElement GetXmlResponse() =>
            new XElement(WebDavNamespaces.DavNs + "response",
                new XElement(WebDavNamespaces.DavNs + "href", UriHelper.ToEncodedString(Uri)),
                new XElement(WebDavNamespaces.DavNs + "status", $"HTTP/1.1 {(int)Result} {DavStatusCodeHelper.GetStatusDescription(Result)}"));
    }

    private readonly IList<UriResult> _results = new List<UriResult>();

    public bool HasItems => _results.Any();

    public void AddResult(Uri uri, DavStatusCode result) => _results.Add(new UriResult(uri, result));

    public XElement GetXmlMultiStatus()
    {
        var xMultiStatus = new XElement(WebDavNamespaces.DavNs + "multistatus");
        foreach (var result in _results)
            xMultiStatus.Add(result.GetXmlResponse());
        return xMultiStatus;
    }
}
