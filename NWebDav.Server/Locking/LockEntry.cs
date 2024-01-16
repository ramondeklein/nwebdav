using System.Xml.Linq;

using NWebDav.Server.Helpers;

namespace NWebDav.Server.Locking;

public readonly record struct LockEntry(LockScope Scope, LockType Type)
{
    public XElement ToXml() =>
        new XElement(WebDavNamespaces.DavNs + "lockentry",
            new XElement(WebDavNamespaces.DavNs + "lockscope", new XElement(WebDavNamespaces.DavNs + XmlHelper.GetXmlValue(Scope))),
            new XElement(WebDavNamespaces.DavNs + "locktype", new XElement(WebDavNamespaces.DavNs + XmlHelper.GetXmlValue(Type))));
}