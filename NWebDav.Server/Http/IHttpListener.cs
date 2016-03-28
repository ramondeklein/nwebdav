using System;
using System.Threading.Tasks;

namespace NWebDav.Server.Http
{
    public interface IHttpListener
    {
        Task<IHttpContext> GetContextAsync();
    }
}
