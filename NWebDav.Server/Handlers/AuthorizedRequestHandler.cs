using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NWebDav.Server.Handlers
{
    public abstract class AuthenticatedRequestHandlerFactory : IRequestHandlerFactory
    {
        private readonly IRequestHandlerFactory _baseRequestHandlerFactory;

        private class AuthenticatedRequestHandler : IRequestHandler, IDisposable
        {
            private readonly AuthenticatedRequestHandlerFactory _factory;
            private readonly IRequestHandler _baseRequestHandler;

            public AuthenticatedRequestHandler(AuthenticatedRequestHandlerFactory factory, IRequestHandler baseRequestHandler)
            {
                _factory = factory;
                _baseRequestHandler = baseRequestHandler;
            }

            public Task<bool> HandleRequestAsync(HttpListenerContext httpListenerContext, IStoreResolver storeResolver)
            {
                // Invoke the OnBeginRequest method
                if (!_factory.OnBeginRequest(httpListenerContext))
                    return Task.FromResult(true);

                try
                {
                    // Invoke the actual request handler
                    return _baseRequestHandler.HandleRequestAsync(httpListenerContext, storeResolver);
                }
                finally
                {
                    // Make sure the OnEndRequest method is called
                    _factory.OnEndRequest(httpListenerContext);
                }
            }

            public void Dispose()
            {
                // Call the base dispose (if any)
                (_baseRequestHandler as IDisposable)?.Dispose();
            }
        }

        public AuthenticatedRequestHandlerFactory(IRequestHandlerFactory baseRequestHandlerFactory = null)
        {
            // Use the default request handler if none has been specified
            if (baseRequestHandlerFactory == null)
                baseRequestHandlerFactory = new RequestHandlerFactory();

            // Save the factory
            _baseRequestHandlerFactory = baseRequestHandlerFactory;
        }

        public IRequestHandler GetRequestHandler(HttpListenerContext httpListenerContext)
        {
            // Obtain the base request handler
            var baseRequestHandler = _baseRequestHandlerFactory.GetRequestHandler(httpListenerContext);

            // Wrap it in the authorized request handler
            return new AuthenticatedRequestHandler(this, baseRequestHandler);
        }

        protected abstract bool OnBeginRequest(HttpListenerContext httpListenerContext);
        protected abstract void OnEndRequest(HttpListenerContext httpListenerContext);
    }
}
