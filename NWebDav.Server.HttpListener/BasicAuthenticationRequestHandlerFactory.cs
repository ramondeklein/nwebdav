using System;
using System.Net;

using NWebDav.Server.Handlers;
using NWebDav.Server.Helpers;
using NWebDav.Server.Http;

namespace NWebDav.Server.HttpListener
{
    public class BasicAuthenticationRequestHandlerFactory : AuthenticatedRequestHandlerFactory
    {
        private readonly IBasicAuthentication _basicAuthentication;

        public BasicAuthenticationRequestHandlerFactory(IBasicAuthentication basicAuthentication, IRequestHandlerFactory baseRequestHandlerFactory = null) : base(baseRequestHandlerFactory)
        {
            _basicAuthentication = basicAuthentication;
        }

        protected override bool OnBeginRequest(IHttpContext httpContext)
        {
            // Obtain the basic identity
            var basicIdentity = httpContext.Session.Principal.Identity as HttpListenerBasicIdentity;
            if (basicIdentity == null || !_basicAuthentication.CheckCredentials(basicIdentity.Name, basicIdentity.Password))
            {
                var response = httpContext.Response;
                response.SendResponse(DavStatusCode.BadRequest, "Invalid credentials specified");
                return false;
            }

            // Authorized
            return true;
        }

        protected override void OnEndRequest(IHttpContext httpContext)
        {
            // NOP
        }
    }

    public interface IBasicAuthentication
    {
        bool CheckCredentials(string name, string password);
    }
}
