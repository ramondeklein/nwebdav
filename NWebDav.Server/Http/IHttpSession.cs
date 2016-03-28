using System;
using System.Security.Principal;

namespace NWebDav.Server.Http
{
    public interface IHttpSession
    {
        IPrincipal Principal { get; }
    }
}