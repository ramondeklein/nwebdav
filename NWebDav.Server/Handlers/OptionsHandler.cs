using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using NWebDav.Server.Helpers;
using NWebDav.Server.Http;
using NWebDav.Server.Stores;

namespace NWebDav.Server.Handlers
{
    [Verb("OPTIONS")]
    public class OptionsHandler : IRequestHandler
    {
        private static readonly string[] s_verbs = { "COPY", "DELETE", "GET", "HEAD", "LOCK", "MKCOL", "MOVE", "OPTIONS", "PROPFIND", "PROPPATCH", "PUT", "UNLOCK" };

        public Task<bool> HandleRequestAsync(IHttpContext httpContext, IStore store)
        {
            // Obtain response
            var response = httpContext.Response;

            // We're currently a DAV class 1 server
            response.SetHeaderValue("DAV", "1");

            // Set the Allow/Public headers
            response.SetHeaderValue("Allow", string.Join(" ", s_verbs));
            response.SetHeaderValue("Public", string.Join(" ", s_verbs));

            // Finished
            response.SendResponse(DavStatusCode.Ok);
            return Task.FromResult(true);
        }
    }
}
