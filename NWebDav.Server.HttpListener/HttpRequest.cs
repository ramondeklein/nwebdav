using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using NWebDav.Server.Http;

namespace NWebDav.Server.HttpListener
{
    public class HttpRequest : IHttpRequest
    {
        private readonly HttpListenerRequest _request;
        private readonly CancellationToken _cancellationToken;

        internal HttpRequest(HttpListenerRequest request, CancellationToken cancellationToken)
        {
            _request = request;
            _cancellationToken = cancellationToken;
        }

        public string HttpMethod => _request.HttpMethod;
        public Uri Url => _request.Url;
        public string RemoteEndPoint => _request.UserHostName;
        public IEnumerable<string> Headers => _request.Headers.AllKeys;
        public string GetHeaderValue(string header) => _request.Headers[header];
        public Stream Stream => _request.InputStream;
        public CancellationToken CancellationToken => _cancellationToken;
    }
}