using System;

namespace NWebDav.Server.Http
{
    public interface IHttpContext
    {
        IHttpRequest Request { get; }
        IHttpResponse Response { get; }
        IHttpSession Session { get; }

        void Close();
    }
}