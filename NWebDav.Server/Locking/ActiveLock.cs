using System;
using System.Globalization;
using System.Xml.Linq;

using NWebDav.Server.Helpers;

namespace NWebDav.Server.Locking;

public readonly record struct ActiveLock(LockType Type, LockScope Scope, int Depth, XElement Owner, int Timeout, Uri LockToken, Uri LockRoot)
{
    public XElement ToXml() =>
        new XElement(WebDavNamespaces.DavNs + "activelock",
            new XElement(WebDavNamespaces.DavNs + "locktype", new XElement(WebDavNamespaces.DavNs + XmlHelper.GetXmlValue(Type))),
            new XElement(WebDavNamespaces.DavNs + "lockscope", new XElement(WebDavNamespaces.DavNs + XmlHelper.GetXmlValue(Scope))),
            new XElement(WebDavNamespaces.DavNs + "depth", Depth == int.MaxValue ? "infinity" : Depth.ToString(CultureInfo.InvariantCulture)),
            new XElement(WebDavNamespaces.DavNs + "owner", Owner),
            new XElement(WebDavNamespaces.DavNs + "timeout", Timeout == -1 ? "Infinite" : "Second-" + Timeout.ToString(CultureInfo.InvariantCulture)),
            new XElement(WebDavNamespaces.DavNs + "locktoken", new XElement(WebDavNamespaces.DavNs + "href", LockToken.AbsoluteUri)),
            new XElement(WebDavNamespaces.DavNs + "lockroot", new XElement(WebDavNamespaces.DavNs + "href", LockRoot.AbsoluteUri)));
}