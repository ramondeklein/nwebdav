using NWebDav.Server.Http;
using System.Net;

namespace NWebDav.Server.HttpListener
{
    /// <inheritdoc cref="IHttpContext"/>
    public sealed class HttpContext : HttpBaseContext
    {
        private static IHttpSession NullSession { get; } = new HttpSession(null);

        public HttpContext(HttpListenerContext httpListenerContext)
            : base(httpListenerContext.Request, httpListenerContext.Response)
        {
        }

        /// <inheritdoc/>
        public override IHttpSession Session => NullSession;
    }
}
