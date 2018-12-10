using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Web;
using NWebDav.Server.Http;

namespace NWebDav.Server.AspNet
{
    public partial class AspNetContext
    {
        private class AspNetRequest : IHttpRequest
        {
            private readonly HttpRequest _httpRequest;

            public AspNetRequest(HttpRequest httpRequest, CancellationToken cancellationToken)
            {
                _httpRequest = httpRequest;
                CancellationToken = cancellationToken;
            }

            public string GetHeaderValue(string header)
            {
                return _httpRequest.Headers[header];
            }

            public string HttpMethod => _httpRequest.HttpMethod;
            public Uri Url => _httpRequest.Url;
            public string RemoteEndPoint => _httpRequest.UserHostName;
            public IEnumerable<string> Headers => _httpRequest.Headers.AllKeys;
            public Stream Stream => _httpRequest.InputStream;
            public CancellationToken CancellationToken { get; }
        }
    }
}