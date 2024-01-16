using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NWebDav.Server.Helpers;

namespace NWebDav.Server.Handlers;

/// <summary>
/// Implementation of the OPTIONS method.
/// </summary>
/// <remarks>
/// This implementation reports a class 1 and 2 compliant WebDAV server
/// that supports all the standard WebDAV methods.
/// </remarks>
public class OptionsHandler : IRequestHandler
{
    private readonly string _allowedMethods;

    public OptionsHandler(IOptions<NWebDavOptions> options)
    {
        _allowedMethods = string.Join(", ", options.Value.Handlers.Keys);
    }
    
    /// <summary>
    /// Handle a OPTIONS request.
    /// </summary>
    /// <param name="httpContext">
    /// The HTTP context of the request.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous OPTIONS operation. The task
    /// will always return <see langword="true"/> upon completion.
    /// </returns>
    public Task<bool> HandleRequestAsync(HttpContext httpContext)
    {
        // Obtain response
        var response = httpContext.Response;

        // We're a DAV class 1 and 2 compatible server
        response.Headers["Dav"] = "1, 2";
        response.Headers["MS-Author-Via"]= "DAV";

        // Set the Allow/Public headers
        response.Headers["Allow"] = _allowedMethods;
        response.Headers["Public"] = _allowedMethods;

        // Finished
        response.SetStatus(DavStatusCode.Ok);
        return Task.FromResult(true);
    }
}
