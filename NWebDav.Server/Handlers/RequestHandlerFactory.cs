using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using NWebDav.Server.Http;

namespace NWebDav.Server.Handlers
{
    public class RequestHandlerFactory : IRequestHandlerFactory
    {
        private static readonly IDictionary<string, Type> RequestHandlers = new Dictionary<string, Type>();

        static RequestHandlerFactory()
        {
            foreach (var requestHandlerType in Assembly.GetExecutingAssembly().GetTypes().Where(t => typeof(IRequestHandler).IsAssignableFrom(t)))
            {
                // Obtain the verbs of the request handler
                foreach (var verbAttribute in requestHandlerType.GetCustomAttributes<VerbAttribute>())
                    RequestHandlers.Add(verbAttribute.Verb, requestHandlerType);
            }
        }

        public IRequestHandler GetRequestHandler(IHttpContext httpContext)
        {
            // Obtain the dispatcher
            Type requestHandlerType;
            if (!RequestHandlers.TryGetValue(httpContext.Request.HttpMethod, out requestHandlerType))
                return null;

            // Create an instance of the request handler
            return (IRequestHandler)Activator.CreateInstance(requestHandlerType);
        }
    }
}
