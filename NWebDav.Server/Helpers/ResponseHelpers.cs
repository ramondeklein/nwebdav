using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace NWebDav.Server.Helpers;

/// <summary>
/// Helper methods for <see cref="HttpResponse"/> objects.
/// </summary>
public static class ResponseHelper
{
    /// <summary>
    /// Set status of the HTTP response.
    /// </summary>
    /// <param name="response">
    /// The HTTP response that should be changed.
    /// </param>
    /// <param name="statusCode">
    /// WebDAV status code that should be set.
    /// </param>
    /// <param name="statusDescription">
    /// The human-readable WebDAV status description. If no status
    /// description is set (or <see langword="null"/>), then the
    /// default status description is written. 
    /// </param>
    /// <remarks>
    /// Not all HTTP infrastructures allow to set the status description,
    /// so it should only be used for informational purposes.
    /// </remarks>
    public static void SetStatus(this HttpResponse response, DavStatusCode statusCode, string? statusDescription = null)
    {
        // Set the status code and description
        response.StatusCode = (int)statusCode;
        if (statusDescription != null)
        {
            var httpResponseFeature = response.HttpContext.Features.Get<IHttpResponseFeature>();
            if (httpResponseFeature != null)
                httpResponseFeature.ReasonPhrase = statusDescription;
        }
    }
}
