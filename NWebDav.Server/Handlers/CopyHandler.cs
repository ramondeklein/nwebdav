using Microsoft.Extensions.Logging;
using NWebDav.Server.Helpers;
using NWebDav.Server.Http;
using NWebDav.Server.Stores;
using SecureFolderFS.Sdk.Storage;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NWebDav.Server.Handlers
{
    /// <summary>
    /// Implementation of the COPY method.
    /// </summary>
    /// <remarks>
    /// The specification of the WebDAV COPY method can be found in the
    /// <see href="http://www.webdav.org/specs/rfc2518.html#METHOD_COPY">
    /// WebDAV specification
    /// </see>.
    /// </remarks>
    public sealed class CopyHandler : IRequestHandler
    {
        /// <summary>
        /// Handle a COPY request.
        /// </summary>
        /// <inheritdoc/>
        public async Task HandleRequestAsync(IHttpContext context, IStore store, IStorageService storageService, ILogger? logger = null, CancellationToken cancellationToken = default)
        {
            // Obtain request and response
            var request = context.Request;
            var response = context.Response;

            // Obtain the destination
            var destinationUri = request.GetDestinationUri();
            if (destinationUri == null)
            {
                // Bad request
                response.SetStatus(HttpStatusCode.BadRequest, "Destination header is missing.");
                return;
            }

            // Make sure the source and destination are different
            if (request.Url.AbsoluteUri.Equals(destinationUri.AbsoluteUri, StringComparison.CurrentCultureIgnoreCase))
            {
                // Forbidden
                response.SetStatus(HttpStatusCode.Forbidden, "Source and destination cannot be the same.");
                return;
            }

            // Check if the Overwrite header is set
            var overwrite = request.GetOverwrite();

            // Split the destination Uri
            var destination = RequestHelper.SplitUri(destinationUri);

            // Obtain the destination collection
            var destinationCollection = await store.GetCollectionAsync(destination.CollectionUri, context).ConfigureAwait(false);
            if (destinationCollection == null)
            {
                // Source not found
                response.SetStatus(HttpStatusCode.Conflict, "Destination cannot be found or is not a collection.");
                return;
            }

            // Obtain the source item
            var sourceItem = await store.GetItemAsync(request.Url, context).ConfigureAwait(false);
            if (sourceItem == null)
            {
                // Source not found
                response.SetStatus(HttpStatusCode.NotFound, "Source cannot be found.");
                return;
            }

            // Determine depth
            var depth = request.GetDepth();

            // Keep track of all errors
            var errors = new UriResultCollection();

            // Copy collection
            await CopyAsync(sourceItem, destinationCollection, destination.Name, overwrite, depth, context, destination.CollectionUri, errors).ConfigureAwait(false);

            // Check if there are any errors
            if (errors.HasItems)
            {
                // Obtain the status document
                var xDocument = new XDocument(errors.GetXmlMultiStatus());

                // Stream the document
                await response.SendResponseAsync(HttpStatusCode.MultiStatus, xDocument, logger, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // Set the response
                response.SetStatus(HttpStatusCode.Created);
            }
        }

        private async Task CopyAsync(IStoreItem source, IStoreCollection destinationCollection, string name, bool overwrite, int depth, IHttpContext context, Uri baseUri, UriResultCollection errors)
        {
            // Determine the new base Uri
            var newBaseUri = UriHelper.Combine(baseUri, name);

            // Copy the item
            var copyResult = await source.CopyAsync(destinationCollection, name, overwrite, context).ConfigureAwait(false);
            if (copyResult.Result != HttpStatusCode.Created && copyResult.Result != HttpStatusCode.NoContent)
            {
                errors.AddResult(newBaseUri, copyResult.Result);
                return;
            }

            // Check if the source is a collection and we are requested to copy recursively
            var sourceCollection = source as IStoreCollection;
            if (sourceCollection != null && depth > 0)
            {
                // The result should also contain a collection
                var newCollection = (IStoreCollection)copyResult.Item;

                // Copy all childs of the source collection
                foreach (var entry in await sourceCollection.GetItemsAsync(context).ConfigureAwait(false))
                    await CopyAsync(entry, newCollection, entry.Name, overwrite, depth - 1, context, newBaseUri, errors).ConfigureAwait(false);
            }
        }
    }
}
