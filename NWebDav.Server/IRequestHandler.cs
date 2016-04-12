using System;
using System.Threading.Tasks;

using NWebDav.Server.Http;
using NWebDav.Server.Stores;

namespace NWebDav.Server
{
    public interface IRequestHandler
    {
        Task<bool> HandleRequestAsync(IHttpContext httpContext, IStore store);
    }
}
