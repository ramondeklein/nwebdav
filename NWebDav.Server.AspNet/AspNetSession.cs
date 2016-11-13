using System.Security.Principal;
using System.Web;
using NWebDav.Server.Http;

namespace NWebDav.Server.AspNet
{
    public partial class AspNetContext
    {
        private class AspNetSession : IHttpSession
        {
            public IPrincipal Principal { get; }

            public AspNetSession(HttpContext httpContext)
            {
                Principal = httpContext.User;
            }
        }
    }
}