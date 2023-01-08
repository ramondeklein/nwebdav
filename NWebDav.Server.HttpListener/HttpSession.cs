using NWebDav.Server.Http;
using System.Security.Principal;

namespace NWebDav.Server.HttpListener
{
    /// <inheritdoc cref="IHttpSession"/>
    internal sealed class HttpSession : IHttpSession
    {
        public HttpSession(IPrincipal? principal)
        {
            Principal = principal;
        }

        /// <inheritdoc/>
        public IPrincipal? Principal { get; }
    }
}
