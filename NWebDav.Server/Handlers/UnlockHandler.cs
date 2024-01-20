using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using NWebDav.Server.Helpers;
using NWebDav.Server.Locking;
using NWebDav.Server.Stores;

namespace NWebDav.Server.Handlers;

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
    private readonly IStore _store;
    private readonly ILockingManager _lockingManager;

    public UnlockHandler(IStore store, ILockingManager lockingManager)
    {
        _store = store;
        _lockingManager = lockingManager;
    }
    
    /// <summary>
    /// Handle a UNLOCK request.
    /// </summary>
    /// <param name="httpContext">
    /// The HTTP context of the request.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous UNLOCK operation. The task
    /// will always return <see langword="true"/> upon completion.
    /// </returns>
    public async Task<bool> HandleRequestAsync(HttpContext httpContext)
    {
        // Obtain request and response
        var request = httpContext.Request;
        var response = httpContext.Response;

        // Obtain the lock-token
        var lockToken = request.GetLockToken();
        if (lockToken == null)
        {
            // Set status to not found
            response.SetStatus(DavStatusCode.BadRequest, "Lock-Token is missing");
            return true;
        }

        // Obtain the WebDAV item
        var item = await _store.GetItemAsync(request.GetUri(), httpContext.RequestAborted).ConfigureAwait(false);
        if (item == null)
        {
            // Set status to not found
            response.SetStatus(DavStatusCode.PreconditionFailed);
            return true;
        }

        // Perform the lock
        var result = await _lockingManager.UnlockAsync(item, lockToken, httpContext.RequestAborted).ConfigureAwait(false);

        // Send response
        response.SetStatus(result);
        return true;
    }
}
