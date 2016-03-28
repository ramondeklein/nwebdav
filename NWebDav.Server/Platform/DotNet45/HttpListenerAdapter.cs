using System;
using System.Net;
using System.Threading.Tasks;
using NWebDav.Server.Http;

namespace NWebDav.Server.Platform.DotNet45
{
    public partial class HttpListenerAdapter : IHttpListener
    {
        private readonly HttpListener _httpListener;

        public HttpListenerAdapter(HttpListener httpListener)
        {
            if (httpListener == null)
                throw new ArgumentNullException(nameof(httpListener));
            _httpListener = httpListener;
        }

        public async Task<IHttpContext> GetContextAsync()
        {
            var httpListenerContext = await _httpListener.GetContextAsync();
            return new HttpContext(httpListenerContext);
        }
    }
}
