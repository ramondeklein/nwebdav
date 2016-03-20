using System;
using System.Net;
using System.Threading.Tasks;

namespace NWebDav.Server
{
    public interface IRequestHandler
    {
        Task<bool> HandleRequestAsync(HttpListenerContext httpListenerContext, IStoreResolver storeResolver);
    }
}
