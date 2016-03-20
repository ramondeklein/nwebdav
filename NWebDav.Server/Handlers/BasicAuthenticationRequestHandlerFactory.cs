using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace NWebDav.Server.Handlers
{
    public class BasicAuthenticationRequestHandlerFactory : AuthenticatedRequestHandlerFactory
    {
        private readonly IBasicAuthentication _basicAuthentication;

        public BasicAuthenticationRequestHandlerFactory(IBasicAuthentication basicAuthentication, IRequestHandlerFactory baseRequestHandlerFactory = null) : base(baseRequestHandlerFactory)
        {
            _basicAuthentication = basicAuthentication;
        }

        protected override bool OnBeginRequest(HttpListenerContext httpListenerContext)
        {
            // Obtain the basic identity
            var basicIdentity = httpListenerContext.User?.Identity as HttpListenerBasicIdentity;
            if (basicIdentity == null || !_basicAuthentication.CheckCredentials(basicIdentity.Name, basicIdentity.Password))
            {
                var response = httpListenerContext.Response;
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.StatusDescription = "Invalid credentials specified";
                response.Close();
                return false;
            }

            // Authorized
            return true;
        }

        protected override void OnEndRequest(HttpListenerContext httpListenerContext)
        {
            // NOP
        }
    }

    public interface IBasicAuthentication
    {
        bool CheckCredentials(string name, string password);
    }
}
