using System;
using System.Net;
using NWebDav.Server.Handlers;
using NWebDav.Server.Http;

namespace NWebDav.Server
{
    public interface IRequestHandlerFactory
    {
        IRequestHandler GetRequestHandler(IHttpContext httpContext);
    }
}
