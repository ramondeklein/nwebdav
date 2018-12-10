using System.Net;
using System.Threading;
using NWebDav.Server.Http;

namespace NWebDav.Server.HttpListener
{
    public class HttpContext : HttpBaseContext
    {
        private static readonly IHttpSession s_nullSession = new HttpSession(null);

        public HttpContext(HttpListenerContext httpListenerContext, CancellationToken cancellationToken) : base(httpListenerContext.Request, httpListenerContext.Response, cancellationToken)
        {
        }

        public override IHttpSession Session => s_nullSession;
    }
}
