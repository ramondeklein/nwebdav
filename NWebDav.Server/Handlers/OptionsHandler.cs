using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using NWebDav.Server.Helpers;
using NWebDav.Server.Stores;

namespace NWebDav.Server.Handlers
{
    [Verb("OPTIONS")]
    public class OptionsHandler : IRequestHandler
    {
        private static readonly string[] Verbs = { "COPY", "DELETE", "GET", "HEAD", "LOCK", "MKCOL", "MOVE", "OPTIONS", "PROPFIND", "PROPPATCH", "PUT", "UNLOCK" };

        public Task<bool> HandleRequestAsync(HttpListenerContext httpListenerContext, IStore store)
        {
            // Obtain response
            var response = httpListenerContext.Response;

            // We're currently a DAV class 1 server
            response.AddHeader("DAV", "1");

            // Set the Allow/Public headers
            response.AppendHeader("Allow", string.Join(" ", Verbs));
            response.AppendHeader("Public", string.Join(" ", Verbs));

            // Finished
            response.SendResponse(DavStatusCode.OK);
            return Task.FromResult(true);
        }
    }
}
