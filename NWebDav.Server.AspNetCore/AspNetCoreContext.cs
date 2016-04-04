using System;
using Microsoft.AspNet.Http;

using NWebDav.Server.Http;

namespace NWebDav.Server.AspNetCore
{
    public partial class AspNetCoreContext : IHttpContext
    {
        private readonly HttpContext _httpContext;

        public AspNetCoreContext(HttpContext httpContext)
        {
            _httpContext = httpContext;

            Request = new AspNetCoreRequest(_httpContext.Request);
            Response = new AspNetCoreResponse(_httpContext.Response);
            Session = new AspNetCoreSession(_httpContext.User);
        }

        public IHttpRequest Request { get; }
        public IHttpResponse Response { get; }
        public IHttpSession Session { get; }

        public void Close()
        {
            // Context is closed automatically
        }
    }
}
