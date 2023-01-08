using Microsoft.Extensions.Logging;
using NWebDav.Server.Http;
using NWebDav.Server.Stores;
using SecureFolderFS.Sdk.Storage;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NWebDav.Server.Dispatching
{
    /// <inheritdoc cref="IRequestDispatcher"/>
    public sealed class WebDavDispatcher : BaseDispatcher
    {
        private readonly IStore _store;
        private readonly IStorageService _davStorageService;

        public WebDavDispatcher(IStore store, IStorageService davStorageService, IRequestHandlerFactory requestHandlerFactory, ILogger? logger)
            : base(requestHandlerFactory, logger)
        {
            _store = store;
            _davStorageService = davStorageService;
        }

        /// <inheritdoc/>
        protected override async Task<bool> InvokeRequestAsync(IRequestHandler requestHandler, IHttpContext context, CancellationToken cancellationToken)
        {
            try
            {
                await requestHandler.HandleRequestAsync(context, _store, _davStorageService, Logger, cancellationToken).ConfigureAwait(false);
                return true;
            }
            catch (NotImplementedException)
            {
                return false;
            }
        }
    }
}
