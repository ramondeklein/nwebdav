using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NWebDav.Server.Http;
using NWebDav.Server.Stores;

namespace NWebDav.Server.Dispatching
{
    /// <inheritdoc cref="IRequestDispatcher"/>
    public sealed class WebDavDispatcher : BaseDispatcher
    {
        private readonly IStore _store;

        public WebDavDispatcher(IStore store, IRequestHandlerFactory requestHandlerFactory, ILogger? logger)
            : base(requestHandlerFactory, logger)
        {
            _store = store;
        }

        /// <inheritdoc/>
        protected override Task<bool> InvokeRequestAsync(IRequestHandler requestHandler, IHttpContext context, CancellationToken cancellationToken)
        {
            return requestHandler.HandleRequestAsync(context, _store, cancellationToken);
        }
    }
}
