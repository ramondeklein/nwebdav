using NWebDav.Server.Http;
using System.Net;
using System.Threading.Tasks;

namespace NWebDav.Server.HttpListener
{
    /// <inheritdoc cref="IHttpContext"/>
    public abstract class HttpBaseContext : IHttpContext
    {
        private readonly HttpListenerResponse _response;

        /// <inheritdoc/>
        public IHttpRequest Request { get; }

        /// <inheritdoc/>
        public IHttpResponse Response { get; }

        /// <inheritdoc/>
        public abstract IHttpSession Session { get; }

        protected HttpBaseContext(HttpListenerRequest request, HttpListenerResponse response)
        {
            // Assign properties
            Request = new HttpRequest(request);
            Response = new HttpResponse(response);

            // Save response
            _response = response;
        }

        /// <inheritdoc/>
        public virtual ValueTask DisposeAsync()
        {
            // Close the response
            _response.Close();

            return default;
        }
    }
}
