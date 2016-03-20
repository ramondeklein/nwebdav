using System;
using System.Net;
using NWebDav.Server.Handlers;

namespace NWebDav.Server
{
    public interface IRequestHandlerFactory
    {
        IRequestHandler GetRequestHandler(HttpListenerContext httpListenerContext);
    }
}
