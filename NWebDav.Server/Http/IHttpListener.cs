using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Principal;
using System.Threading.Tasks;

namespace NWebDav.Server.Http
{
    public interface IHttpListener
    {
        Task<IHttpContext> GetContextAsync();
    }

    public interface IHttpContext
    {
        IHttpRequest Request { get; }
        IHttpResponse Response { get; }
        IHttpSession Session { get; }

        void Close();
    }

    public interface IHttpRequest
    {
        string HttpMethod { get; }
        Uri Url { get; }
        IPEndPoint RemoteEndPoint { get; }
        IEnumerable<string> Headers { get; }
        string GetHeaderValue(string header);
        Stream Stream { get; }
    }

    public interface IHttpResponse
    {
        int Status { get; set; }
        string StatusDescription { get; set; }
        void SetHeaderValue(string header, string value);
        Stream Stream { get; }
    }

    public interface IHttpSession
    {
        IPrincipal Principal { get; }
    }
}
