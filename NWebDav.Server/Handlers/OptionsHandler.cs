using Microsoft.Extensions.Logging;
using NWebDav.Server.Helpers;
using NWebDav.Server.Http;
using NWebDav.Server.Stores;
using SecureFolderFS.Sdk.Storage;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace NWebDav.Server.Handlers
{
    /// <summary>
    /// Implementation of the OPTIONS method.
    /// </summary>
    /// <remarks>
    /// This implementation reports a class 1 and 2 compliant WebDAV server
    /// that supports all the standard WebDAV methods.
    /// </remarks>
    public sealed class OptionsHandler : IRequestHandler
    {
        /// <summary>
        /// Handle a OPTIONS request.
        /// </summary>
        /// <inheritdoc/>
        public Task HandleRequestAsync(IHttpContext context, IStore store, IStorageService storageService, ILogger? logger = null, CancellationToken cancellationToken = default)
        {
            // Obtain response
            var response = context.Response;

            // We're a DAV class 1 and 2 compatible server
            response.SetHeaderValue("Dav", "1, 2");
            response.SetHeaderValue("MS-Author-Via", "DAV");

            // Set the Allow/Public headers
            response.SetHeaderValue("Allow", string.Join(", ", RequestHandlerFactory.AllowedMethods));
            response.SetHeaderValue("Public", string.Join(", ", RequestHandlerFactory.AllowedMethods));

            // Finished
            response.SetStatus(HttpStatusCode.OK);
            return Task.CompletedTask;
        }
    }
}
