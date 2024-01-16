using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;

namespace NWebDav.Server.Helpers;

public interface IXmlReaderWriter
{
    Task<XDocument?> LoadXmlDocumentAsync(HttpRequest request, CancellationToken cancellationToken);
    Task SendResponseAsync(HttpResponse response, DavStatusCode statusCode, XDocument xDocument);
}
