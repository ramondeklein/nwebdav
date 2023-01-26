using NWebDav.Server.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace NWebDav.Server.HttpListener
{
    /// <inheritdoc cref="IHttpRequest"/>
    internal sealed class HttpRequest : IHttpRequest
    {
        private readonly HttpListenerRequest _request;

        public HttpRequest(HttpListenerRequest request)
        {
            _request = request;
        }

        /// <inheritdoc/>
        public string HttpMethod => _request.HttpMethod;

        /// <inheritdoc/>
        public Uri? Url => _request.Url;

        /// <inheritdoc/>
        public string? RemoteEndPoint => _request.UserHostName;

        /// <inheritdoc/>
        public IEnumerable<string?> Headers => _request.Headers.AllKeys;

        /// <inheritdoc/>
        public string? GetHeaderValue(string header) => _request.Headers[header];

        /// <inheritdoc/>
        public Stream? InputStream => _request.InputStream == Stream.Null ? null : _request.InputStream;
    }
}