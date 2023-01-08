using Microsoft.Extensions.Logging;
using NWebDav.Server.Helpers;
using NWebDav.Server.Http;
using NWebDav.Server.Stores;
using SecureFolderFS.Sdk.Storage;
using SecureFolderFS.Sdk.Storage.Enums;
using SecureFolderFS.Sdk.Storage.Extensions;
using SecureFolderFS.Sdk.Storage.ModifiableStorage;
using SecureFolderFS.Shared.Extensions;
using System.IO;
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
    public sealed class PutHandler : IRequestHandler
    {
        /// <summary>
        /// Handle a PUT request.
        /// </summary>
        /// <inheritdoc/>
        public async Task HandleRequestAsync(IHttpContext context, IStore store, IStorageService storageService, ILogger? logger = null, CancellationToken cancellationToken = default)
        {
            if (context.Request.Url is null)
            {
                context.Response.SetStatus(HttpStatusCode.NotFound);
                return;
            }

            // It's not a collection, so we'll try again by fetching the item in the parent collection
            var splitUri = RequestHelper.SplitUri(context.Request.Url);

            // Obtain collection
            var folder = await storageService.TryGetFolderFromPathAsync(splitUri.CollectionUri.GetUriPath(), cancellationToken).ConfigureAwait(false);
            if (folder is null)
            {
                context.Response.SetStatus(HttpStatusCode.Conflict);
                return;
            }
            if (folder is not IModifiableFolder modifiableFolder)
            {
                context.Response.SetStatus(HttpStatusCode.Forbidden);
                return;
            }

            var createdFileResult = await modifiableFolder.CreateFileWithResultAsync(splitUri.Name, CreationCollisionOption.ReplaceExisting, cancellationToken).ConfigureAwait(false);
            if (createdFileResult.Successful)
            {
                var fileStreamResult = await createdFileResult.Value!.OpenStreamWithResultAsync(FileAccess.ReadWrite, cancellationToken).ConfigureAwait(false);
                if (!fileStreamResult.Successful)
                {
                    context.Response.SetStatus(fileStreamResult);
                    return;
                }

                if (context.Request.InputStream is null)
                {
                    // TODO: Is that error appropriate?
                    context.Response.SetStatus(HttpStatusCode.NoContent);
                    return;
                }

                var fileStream = fileStreamResult.Value!;
                await using (fileStream)
                {
                    // Make sure we can write to the file
                    if (!fileStream.CanWrite)
                    {
                        context.Response.SetStatus(HttpStatusCode.Forbidden);
                        return;
                    }

                    try
                    {
                        // Copy contents
                        await context.Request.InputStream.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);

                        // Set status to OK
                        context.Response.SetStatus(HttpStatusCode.OK);
                    }
                    catch (IOException ioEx) when (ioEx.IsDiskFull())
                    {
                        context.Response.SetStatus(HttpStatusCode.InsufficientStorage);
                        return;
                    }
                }
            }
            else
                context.Response.SetStatus(createdFileResult);
        }
    }
}
