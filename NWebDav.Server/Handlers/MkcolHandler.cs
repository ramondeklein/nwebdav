using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using NWebDav.Server.Helpers;
using NWebDav.Server.Stores;

namespace NWebDav.Server.Handlers;

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
    private readonly IStore _store;

    public MkcolHandler(IStore store)
    {
        _store = store;
    }
    
    /// <summary>
    /// Handle a MKCOL request.
    /// </summary>
    /// <param name="httpContext">
    /// The HTTP context of the request.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous MKCOL operation. The task
    /// will always return <see langword="true"/> upon completion.
    /// </returns>
    public async Task<bool> HandleRequestAsync(HttpContext httpContext)
    {
        // Obtain request and response
        var request = httpContext.Request;
        var response = httpContext.Response;

        // The collection must always be created inside another collection
        var splitUri = RequestHelper.SplitUri(request.GetUri());

        // Obtain the parent entry
        var collection = await _store.GetCollectionAsync(splitUri.CollectionUri, httpContext.RequestAborted).ConfigureAwait(false);
        if (collection == null)
        {
            // Source not found
            response.SetStatus(DavStatusCode.Conflict);
            return true;
        }

        // Create the collection
        var result = await collection.CreateCollectionAsync(splitUri.Name, false, httpContext.RequestAborted).ConfigureAwait(false);

        // Finished
        response.SetStatus(result.Result);
        return true;
    }
}
