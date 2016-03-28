using System;
using System.IO;

namespace NWebDav.Server.Http
{
    public interface IHttpResponse
    {
        int Status { get; set; }
        string StatusDescription { get; set; }
        void SetHeaderValue(string header, string value);
        Stream Stream { get; }
    }
}
