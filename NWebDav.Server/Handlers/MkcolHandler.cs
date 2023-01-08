using NWebDav.Server.Helpers;
using NWebDav.Server.Http;
using NWebDav.Server.Stores;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace NWebDav.Server.Handlers
{
    /// <summary>
    /// Implementation of the MKCOL method.
    /// </summary>
    /// <remarks>
    /// The specification of the WebDAV MKCOL method can be found in the
    /// <see href="http://www.webdav.org/specs/rfc2518.html#METHOD_MKCOL">
    /// WebDAV specification
    /// </see>.
    /// </remarks>
    public class MkcolHandler : IRequestHandler
    {
        /// <summary>
        /// Handle a MKCOL request.
        /// </summary>
        /// <inheritdoc/>
        public async Task<bool> HandleRequestAsync(IHttpContext context, IStore store, CancellationToken cancellationToken = default)
        {
            // Obtain request and response
            var request = context.Request;
            var response = context.Response;

            // The collection must always be created inside another collection
            var splitUri = RequestHelper.SplitUri(request.Url);

            // Obtain the parent entry
            var collection = await store.GetCollectionAsync(splitUri.CollectionUri, context).ConfigureAwait(false);
            if (collection == null)
            {
                // Source not found
                response.SetStatus(HttpStatusCode.Conflict);
                return true;
            }

            // Create the collection
            var result = await collection.CreateCollectionAsync(splitUri.Name, false, context).ConfigureAwait(false);

            // Finished
            response.SetStatus(result.Result);
            return true;
        }
    }
}
