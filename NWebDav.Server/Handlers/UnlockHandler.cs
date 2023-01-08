using NWebDav.Server.Helpers;
using NWebDav.Server.Http;
using NWebDav.Server.Stores;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace NWebDav.Server.Handlers
{
    /// <summary>
    /// Implementation of the UNLOCK method.
    /// </summary>
    /// <remarks>
    /// The specification of the WebDAV UNLOCK method can be found in the
    /// <see href="http://www.webdav.org/specs/rfc2518.html#METHOD_UNLOCK">
    /// WebDAV specification
    /// </see>.
    /// </remarks>
    public class UnlockHandler : IRequestHandler
    {
        /// <summary>
        /// Handle a UNLOCK request.
        /// </summary>
        /// <inheritdoc/>
        public async Task<bool> HandleRequestAsync(IHttpContext context, IStore store, CancellationToken cancellationToken = default)
        {
            // Obtain request and response
            var request = context.Request;
            var response = context.Response;

            // Obtain the lock-token
            var lockToken = request.GetLockToken();

            // Obtain the WebDAV item
            var item = await store.GetItemAsync(request.Url, context).ConfigureAwait(false);
            if (item == null)
            {
                // Set status to not found
                response.SetStatus(HttpStatusCode.PreconditionFailed);
                return true;
            }

            // Check if we have a lock manager
            var lockingManager = item.LockingManager;
            if (lockingManager == null)
            {
                // Set status to not found
                response.SetStatus(HttpStatusCode.PreconditionFailed);
                return true;
            }

            // Perform the lock
            var result = lockingManager.Unlock(item, lockToken);

            // Send response
            response.SetStatus(result);
            return true;
        }
    }
}
