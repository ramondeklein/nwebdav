using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace NWebDav.Server.Http
{
    public interface IHttpRequest
    {
        string HttpMethod { get; }
        Uri Url { get; }
        IPEndPoint RemoteEndPoint { get; }
        IEnumerable<string> Headers { get; }
        string GetHeaderValue(string header);
        Stream Stream { get; }
    }
}