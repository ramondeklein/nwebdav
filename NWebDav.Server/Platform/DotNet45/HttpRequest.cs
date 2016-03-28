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
        private class HttpRequest : IHttpRequest
        {
            private readonly HttpListenerRequest _request;

            internal HttpRequest(HttpListenerRequest request)
            {
                _request = request;
            }

            public string HttpMethod => _request.HttpMethod;
            public Uri Url => _request.Url;
            public IPEndPoint RemoteEndPoint => _request.RemoteEndPoint;
            public IEnumerable<string> Headers => _request.Headers.AllKeys;
            public string GetHeaderValue(string header) => _request.Headers[header];
            public Stream Stream => _request.InputStream;
        }
    }
}