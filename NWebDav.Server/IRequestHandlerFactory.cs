using System;
using NWebDav.Server.Http;

namespace NWebDav.Server
{
    public interface IRequestHandlerFactory
    {
        IRequestHandler GetRequestHandler(IHttpContext httpContext);
    }
}
