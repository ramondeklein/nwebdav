using Microsoft.Extensions.Logging;
using NWebDav.Server.Helpers;
using NWebDav.Server.Http;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace NWebDav.Server.Dispatching
{
    /// <inheritdoc cref="IRequestDispatcher"/>
    public abstract class BaseDispatcher : IRequestDispatcher
    {
        /// <inheritdoc/>
        public virtual IRequestHandlerProvider RequestHandlerFactory { get; }

        /// <inheritdoc/>
        public virtual ILogger? Logger { get; }

        protected BaseDispatcher(IRequestHandlerProvider requestHandlerFactory, ILogger? logger)
        {
            RequestHandlerFactory = requestHandlerFactory;
            Logger = logger;
        }

        /// <inheritdoc/>
        public async Task DispatchRequestAsync(IHttpContext context, CancellationToken cancellationToken = default)
        {
            // Make sure a HTTP context is specified
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // Make sure the HTTP context has a request
            var request = context.Request;
            if (request == null)
                throw new ArgumentException("The HTTP context doesn't have a request.", nameof(context));

            // Make sure the HTTP context has a response
            var response = context.Response;
            if (response == null)
                throw new ArgumentException("The HTTP context doesn't have a response.", nameof(context));

            // Determine the request log-string
            var logRequest = $"{request.HttpMethod}:{request.Url}:{request.RemoteEndPoint}";

            // Log the request
            Logger?.LogInformation($"{logRequest} - Start processing");

            try
            {
                // Set the Server header of the response message. This has no
                // functional use, but it can be used to diagnose problems by
                // determining the actual WebDAV server and version.
                // response.SetHeaderValue("Server", SERVER_NAME);

                // Start the stopwatch
                var sw = Stopwatch.StartNew();

                IRequestHandler? requestHandler;
                try
                {
                    // Obtain the request handler for this message
                    requestHandler = RequestHandlerFactory.GetRequestHandler(context.Request.HttpMethod);

                    // Make sure we got a request handler
                    if (requestHandler is null)
                    {
                        // Log warning
                        Logger?.LogWarning($"{logRequest} - Not implemented.");

                        // This request is not implemented
                        context.Response.SetStatus(HttpStatusCode.NotImplemented);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Logger?.LogError(ex, $"Unexpected exception while trying to obtain the request handler (method={request.HttpMethod}, url={request.Url}, source={request.RemoteEndPoint}");
                    return;
                }

                try
                {
                    // Handle the request
                    if (await InvokeRequestAsync(requestHandler, context, cancellationToken).ConfigureAwait(false))
                    {
                        // Log processing duration
                        Logger?.LogInformation($"{logRequest} - Finished processing ({sw.ElapsedMilliseconds}ms, HTTP result: {context.Response.StatusCode})");
                    }
                    else
                    {
                        // Log warning
                        Logger?.LogWarning($"{logRequest} - Not processed.");

                        // Set status code to bad request
                        context.Response.SetStatus(HttpStatusCode.NotImplemented);
                    }
                }
                catch (Exception ex)
                {
                    // Log what's going wrong
                    Logger?.LogError(ex, $"Unexpected exception while handling request (method={request.HttpMethod}, url={request.Url}, source={request.RemoteEndPoint}");

                    try
                    {
                        // Attempt to return 'InternalServerError' (if still possible)
                        context.Response.SetStatus(HttpStatusCode.InternalServerError);
                    }
                    catch (Exception)
                    {
                        // We might not be able to send the response, because a response was already initiated by the the request handler.
                    }
                }
                finally
                {
                    // Check if we need to dispose the request handler
                    (requestHandler as IDisposable)?.Dispose();
                }
            }
            finally
            {
                try
                {
                    // Always close the context
                    await context.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // Closing the context can sometimes fail, for example when Microsoft-WebDAV-MiniRedir cancels
                    // a PUT request due to the file size being too large.
                }
            }
        }

        /// <summary>
        /// Invokes and completes request for a given <paramref name="requestHandler"/>.
        /// </summary>
        /// <param name="requestHandler">The <see cref="IRequestHandler"/> to use for handling <paramref name="context"/>.</param>
        /// <param name="context">The <see cref="IHttpContext"/> of the operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels this action.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous operation. Value is true if the operation is implemented and ran to completion, otherwise false.</returns>
        /// <remarks>
        /// The return value does not represent the processed result - it does not guarantee that the operation completed as expected, only that the operation is implemented.
        /// </remarks>
        protected abstract Task<bool> InvokeRequestAsync(IRequestHandler requestHandler, IHttpContext context, CancellationToken cancellationToken);
    }
}
