using NWebDav.Server.Helpers;
using NWebDav.Server.Http;
using NWebDav.Server.Stores;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace NWebDav.Server.Handlers
{
    /// <summary>
    /// Implementation of the PUT method.
    /// </summary>
    /// <remarks>
    /// The specification of the WebDAV PUT method can be found in the
    /// <see href="http://www.webdav.org/specs/rfc2518.html#METHOD_PUT">
    /// WebDAV specification
    /// </see>.
    /// </remarks>
    public class PutHandler : IRequestHandler
    {
        /// <summary>
        /// Handle a PUT request.
        /// </summary>
        /// <inheritdoc/>
        public async Task<bool> HandleRequestAsync(IHttpContext context, IStore store, CancellationToken cancellationToken = default)
        {
            // Obtain request and response
            var request = context.Request;
            var response = context.Response;

            // It's not a collection, so we'll try again by fetching the item in the parent collection
            var splitUri = RequestHelper.SplitUri(request.Url);

            // Obtain collection
            var collection = await store.GetCollectionAsync(splitUri.CollectionUri, context).ConfigureAwait(false);
            if (collection == null)
            {
                // Source not found
                response.SetStatus(HttpStatusCode.Conflict);
                return true;
            }

            // Obtain the item
            var result = await collection.CreateItemAsync(splitUri.Name, true, context).ConfigureAwait(false);
            var status = result.Result;
            if (status == HttpStatusCode.Created || status == HttpStatusCode.NoContent)
            {
                // Upload the information to the item
                var uploadStatus = await result.Item.UploadFromStreamAsync(context, request.InputStream).ConfigureAwait(false);
                if (uploadStatus != HttpStatusCode.OK)
                    status = uploadStatus;
            }

            // Finished writing
            response.SetStatus(status);
            return true;
        }
    }
}
