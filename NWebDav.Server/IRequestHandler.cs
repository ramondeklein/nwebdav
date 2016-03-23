using System;
using System.Net;
using System.Threading.Tasks;
using NWebDav.Server.Stores;

namespace NWebDav.Server
{
    public interface IRequestHandler
    {
        Task<bool> HandleRequestAsync(HttpListenerContext httpListenerContext, IStore store);
    }
}
