using System.IO;
using System.Web;
using NWebDav.Server.Http;

namespace NWebDav.Server.AspNet
{
    public partial class AspNetContext
    {
        private class AspNetResponse : IHttpResponse
        {
            private readonly HttpResponse _httpResponse;

            public AspNetResponse(HttpResponse httpResponse)
            {
                _httpResponse = httpResponse;
            }

            public void SetHeaderValue(string header, string value)
            {
                switch (header.ToLowerInvariant())
                {
                    case "content-type":
                        _httpResponse.ContentType = value;
                        break;

                    default:
                _httpResponse.Headers.Set(header, value);
                        break;
                }
            }

            public int Status
            {
                get { return _httpResponse.StatusCode; }
                set { _httpResponse.StatusCode = value; }
            }

            public string StatusDescription
            {
                get { return _httpResponse.StatusDescription; }
                set { _httpResponse.StatusDescription = value; }
            }

            public Stream Stream => _httpResponse.OutputStream;
        }
    }
}