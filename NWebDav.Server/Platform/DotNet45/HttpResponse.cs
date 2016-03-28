using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NWebDav.Server.Http;

namespace NWebDav.Server.Platform.DotNet45
{
    public partial class HttpListenerAdapter
    {
        private class HttpResponse : IHttpResponse
        {
            private readonly HttpListenerResponse _response;

            internal HttpResponse(HttpListenerResponse response)
            {
                _response = response;

                // Set initial response
                Status = _response.StatusCode;
                StatusDescription = _response.StatusDescription;
            }

            public int Status { get; set; }
            public string StatusDescription { get; set; }

            public void SetHeaderValue(string header, string value)
            {
                switch (header)
                {
                    case "Content-Length":
                        _response.ContentLength64 = long.Parse(value);
                        break;

                    default:
                        _response.Headers[header] = value;
                        break;
                }
            }

            public Stream Stream => _response.OutputStream;
        }
    }
}