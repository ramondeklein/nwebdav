using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace NWebDav.Server
{
    public class NWebDavOptions
    {
        public bool RequireAuthentication { get; set; }
        public IDictionary<string, Type> Handlers { get; } = new Dictionary<string, Type>();
        public Func<HttpContext, bool> Filter { get; set; } = _ => true;
    }
}