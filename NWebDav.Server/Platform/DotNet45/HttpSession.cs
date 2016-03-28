using System;
using System.Security.Principal;
using NWebDav.Server.Http;

namespace NWebDav.Server.Platform.DotNet45
{
    public partial class HttpListenerAdapter
    {
        private class HttpSession : IHttpSession
        {
            internal HttpSession(IPrincipal principal)
            {
                Principal = principal;
            }

            public IPrincipal Principal { get; }
        }
    }
}