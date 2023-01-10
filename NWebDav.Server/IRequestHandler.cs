using Microsoft.Extensions.Logging;
using NWebDav.Server.Http;
using NWebDav.Server.Stores;
using SecureFolderFS.Sdk.Storage;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace NWebDav.Server
{
    /// <summary>
    /// Interface for all request handlers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each HTTP request will be handled by a single object implementing this
    /// interface. A request handler is generally handling only one HTTP method
    /// (i.e. PROPPATCH), but it can also choose to implement multiple HTTP
    /// methods, because there is a lot of overlap between the two methods
    /// (i.e. GET and HEAD).
    /// </para>
    /// <para>
    /// It is possible to re-use request handlers, but care must be taken that
    /// the handler is re-entrant, because it can be called multiple times in
    /// parallel.
    /// </para>
    /// <para>
    /// Request handlers are typically created via request handler factories
    /// implementing the <see cref="IRequestHandlerProvider"/> interface.
    /// </para>
    /// </remarks>
    /// <seealso cref="IRequestHandlerProvider"/>
    public interface IRequestHandler
    {
        /// <summary>
        /// Handles and processes an incoming request.
        /// </summary>
        /// <param name="context">The HTTP context of the request.</param>
        /// <param name="store">Store that is used to access the collections and items.</param>
        /// <param name="storageService">The <see cref="IStorageService"/> instance that will be used to access the file system.</param>
        /// <param name="logger">The logger to used to trace warnings and debug information.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels this action.</param>
        /// <returns>
        /// A <see cref="Task"/> that represents the asynchronous operation. If the request was not handled - i.e. <see cref="NotImplementedException"/> was thrown,
        /// then the status code <see cref="HttpStatusCode.NotImplemented"/> is returned to the requester.
        /// </returns>
        Task HandleRequestAsync(IHttpContext context, IStore store, IStorageService storageService, ILogger? logger = null, CancellationToken cancellationToken = default); 
    }
}
