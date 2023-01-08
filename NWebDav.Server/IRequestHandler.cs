using NWebDav.Server.Http;
using NWebDav.Server.Stores;
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
    /// implementing the <see cref="IRequestHandlerFactory"/> interface.
    /// </para>
    /// </remarks>
    /// <seealso cref="IRequestHandlerFactory"/>
    public interface IRequestHandler
    {
        /// <summary>
        /// Handles and processes an incoming request.
        /// </summary>
        /// <param name="context">The HTTP context of the request.</param>
        /// <param name="store">Store that is used to access the collections and items.</param>
        /// <returns>
        /// A <see cref="Task"/> that represents the asynchronous operation.
        /// The task will return a boolean upon completion of the task that is <see langword="true"/> if the request was handled or
        /// <see langword="false"/> if the request wasn't handled. If a request is not handled, then the status code
        /// <see cref="HttpStatusCode.NotImplemented"/> is returned to the requester.
        /// </returns>
        // TODO(wd): Use IStorageService
        Task<bool> HandleRequestAsync(IHttpContext context, IStore store, CancellationToken cancellationToken = default); 
    }
}
