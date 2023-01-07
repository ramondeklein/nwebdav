using NWebDav.Server.Http;
using System.IO;
using System.Net;

namespace NWebDav.Server.HttpListener
{
    /// <inheritdoc cref="IHttpResponse"/>
    internal sealed class HttpResponse : IHttpResponse
    {
        private readonly HttpListenerResponse _response;

        public HttpResponse(HttpListenerResponse response)
        {
            _response = response;
        }

        /// <inheritdoc/>
        public int StatusCode
        {
            get => _response.StatusCode;
            set => _response.StatusCode = value;
        }

        /// <inheritdoc/>
        public string? StatusDescription
        {
            get => _response.StatusDescription;
            set => _response.StatusDescription = value;
        }

        /// <inheritdoc/>
        public Stream OutputStream => _response.OutputStream;

        /// <inheritdoc/>
        public void SetHeaderValue(string header, string value)
        {
            switch (header)
            {
                case "Content-Length":
                    _response.ContentLength64 = long.Parse(value);
                    break;

                case "Content-Type":
                    _response.ContentType = value;
                    break;

                default:
                    _response.Headers[header] = value;
                    break;
            }
        }
    }
}