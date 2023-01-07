using Microsoft.Extensions.Logging;
using NWebDav.Server.Http;
using System.Threading;
using System.Threading.Tasks;

namespace NWebDav.Server.Dispatching
{
    /// <summary>
    /// Interface responsible for dispatching <see cref="IRequestHandler"/> requests.
    /// </summary>
    /// <remarks>
    /// The WebDAV dispatcher handles the processing of a WebDAV request.
    /// The library provides <see cref="IRequestDispatcher"/> that can be used to dispatch
    /// requests based on the <see cref="IRequestHandler"/> interface and <see cref="IRequestHandlerFactory"/> interfaces.
    /// </remarks>
    public interface IRequestDispatcher
    {
        /// <summary>
        /// Gets the request handler factory associated with this dispatcher.
        /// </summary>
        IRequestHandlerFactory RequestHandlerFactory { get; }

        /// <summary>
        /// Gets the logger associated with this dispatcher.
        /// </summary>
        ILogger? Logger { get; }

        /// <summary>
        /// Dispatch the WebDAV request based on the given HTTP context.
        /// </summary>
        /// <param name="context">HTTP context for this request.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels this action.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
        Task DispatchRequestAsync(IHttpContext context, CancellationToken cancellationToken = default);
    }
}