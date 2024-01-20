using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace NWebDav.Server.Helpers;

/// <summary>
/// Split URI consisting of a collection URI and a name string.
/// </summary>
public class SplitUri
{
    /// <summary>
    /// Collection URI that holds the collection/document.
    /// </summary>
    public required Uri CollectionUri { get; init; }

    /// <summary>
    /// Name of the collection/document within its container collection.
    /// </summary>
    public required string Name { get; init; }
}

/// <summary>
/// Range
/// </summary>
public class Range
{
    /// <summary>
    /// Optional start value.
    /// </summary>
    public long? Start { get; init; }

    /// <summary>
    /// Optional end value.
    /// </summary>
    public long? End { get; init; }

    /// <summary>
    /// Optional conditional date/time.
    /// </summary>
    public DateTime If {get; set; }
}

/// <summary>
/// Helper methods for <see cref="HttpRequest"/> objects.
/// </summary>
public static class RequestHelper
{
    private static readonly Regex s_rangeRegex = new("bytes\\=(?<start>[0-9]*)-(?<end>[0-9]*)");
    private static readonly char[] s_splitChars = {','};

    /// <summary>
    /// Split an URI into a collection and name part.
    /// </summary>
    /// <param name="uri">URI that should be split.</param>
    /// <returns>
    /// Split URI in a collection URI and a name string.
    /// </returns>
    public static SplitUri? SplitUri(Uri uri)
    {
        // Strip a trailing slash
        var trimmedUri = uri.AbsoluteUri;
        if (trimmedUri.EndsWith('/'))
            trimmedUri = trimmedUri[..^1];

        // Determine the offset of the name
        var slashOffset = trimmedUri.LastIndexOf('/');
        if (slashOffset == -1)
            return null;

        // Separate name from path
        return new SplitUri
        {
            CollectionUri = new Uri(trimmedUri[..slashOffset]),
            Name = Uri.UnescapeDataString(trimmedUri[(slashOffset + 1)..])
        };
    }

    /// <summary>
    /// Obtain the uri from the request.
    /// </summary>
    /// <param name="request">HTTP request.</param>
    /// <returns>
    /// URI for this HTTP request.
    /// </returns>
    public static Uri GetUri(this HttpRequest request)
    {
        return new Uri(request.GetEncodedUrl());
    }
    
    /// <summary>
    /// Obtain the destination uri from the request.
    /// </summary>
    /// <param name="request">HTTP request.</param>
    /// <returns>
    /// Destination for this HTTP request (or <see langword="null"/> if no
    /// destination is specified).
    /// </returns>
    public static Uri? GetDestinationUri(this HttpRequest request)
    {
        // Obtain the destination
        var destinationHeader = request.Headers["Destination"].FirstOrDefault();
        if (destinationHeader == null)
            return null;

        // Create the destination URI
        return destinationHeader.StartsWith("/") ? new Uri(new Uri(request.GetEncodedUrl()), destinationHeader) : new Uri(destinationHeader);
    }        

    /// <summary>
    /// Obtain the depth value from the request.
    /// </summary>
    /// <param name="request">HTTP request.</param>
    /// <returns>
    /// Depth of the HTTP request (<c>int.MaxValue</c> if infinity).
    /// </returns>
    /// <remarks>
    /// If the Depth header is not set, then the specification specifies
    /// that it should be interpreted as infinity.
    /// </remarks>
    public static int GetDepth(this HttpRequest request)
    {
        // Obtain the depth header (no header means infinity)
        var depthHeader = request.Headers["Depth"].FirstOrDefault();
        if (depthHeader == null || depthHeader == "infinity")
            return int.MaxValue;

        // Determined depth
        if (!int.TryParse(depthHeader, out var depth))
            return int.MaxValue;

        // Return depth
        return depth;
    }

    /// <summary>
    /// Obtain the overwrite value from the request.
    /// </summary>
    /// <param name="request">HTTP request.</param>
    /// <returns>
    /// Flag indicating whether or not to overwrite the destination
    /// if it already exists.
    /// </returns>
    /// <remarks>
    /// If the Overwrite header is not set, then the specification
    /// specifies that it should be interpreted as 
    /// <see langwordk="true"/>.
    /// </remarks>
    public static bool GetOverwrite(this HttpRequest request)
    {
        // Get the Overwrite header
        var overwriteHeader = request.Headers["Overwrite"].FirstOrDefault() ?? "T";

        // It should be set to "T" (true) or "F" (false)
        return overwriteHeader.ToUpperInvariant() == "T";
    }

    /// <summary>
    /// Obtain the list of timeout values from the request.
    /// </summary>
    /// <param name="request">HTTP request.</param>
    /// <returns>
    /// List of timeout values in seconds (<c>-1</c> if infinite).
    /// </returns>
    /// <remarks>
    /// If the Timeout header is not set, then <see langword="null"/> is
    /// returned.
    /// </remarks>
    public static IList<int>? GetTimeouts(this HttpRequest request)
    {
        // Get the value of the timeout header as a string
        var timeoutHeader = request.Headers["Timeout"].FirstOrDefault();
        if (string.IsNullOrEmpty(timeoutHeader))
            return null;

        // Return each item
        int ParseTimeout(string t)
        {
            // Check for 'infinite'
            if (t == "Infinite")
                return -1;

            // Parse the number of seconds
            if (!t.StartsWith("Second-") || !int.TryParse(t.Substring(7), out var timeout))
                return 0;
            return timeout;
        }

        // Return the timeout values
        return timeoutHeader.Split(s_splitChars, StringSplitOptions.RemoveEmptyEntries).Select(ParseTimeout).Where(t => t != 0).ToArray();
    }

    /// <summary>
    /// Obtain the lock-token URI from the request.
    /// </summary>
    /// <param name="request">HTTP request.</param>
    /// <returns>
    /// Lock token URI (<see langword="null"/> if not set).
    /// </returns>
    public static Uri? GetLockToken(this HttpRequest request)
    {
        // Get the value of the lock-token header as a string
        var lockTokenHeader = request.Headers["Lock-Token"].FirstOrDefault();
        if (string.IsNullOrEmpty(lockTokenHeader))
            return null;

        // Strip the brackets from the header
        if (!lockTokenHeader.StartsWith("<") || !lockTokenHeader.EndsWith(">"))
            return null;

        // Create an Uri of the intermediate part
        return new Uri(lockTokenHeader.Substring(1, lockTokenHeader.Length - 2), UriKind.Absolute);
    }

    /// <summary>
    /// Obtain the if-lock-token URI from the request.
    /// </summary>
    /// <param name="request">HTTP request.</param>
    /// <returns>
    /// If lock token URI (<see langword="null"/> if not set).
    /// </returns>
    public static Uri? GetIfLockToken(this HttpRequest request)
    {
        // Get the value of the lock-token header as a string
        var lockTokenHeader = request.Headers["If"].FirstOrDefault();
        if (string.IsNullOrEmpty(lockTokenHeader))
            return null;

        // Strip the brackets from the header
        if (!lockTokenHeader.StartsWith("(<") || !lockTokenHeader.EndsWith(">)"))
            return null;

        // Create an Uri of the intermediate part
        return new Uri(lockTokenHeader.Substring(2, lockTokenHeader.Length - 4), UriKind.Absolute);
    }

    /// <summary>
    /// Obtain the range value from the request.
    /// </summary>
    /// <param name="request">HTTP request.</param>
    /// <returns>
    /// Range value (start/end) with an option if condition.
    /// </returns>
    public static Range? GetRange(this HttpRequest request)
    {
        // Get the value of the range header as a string
        var rangeHeader = request.Headers["Range"].FirstOrDefault();
        if (string.IsNullOrEmpty(rangeHeader))
            return null;

        // We only support the bytes=<start>-<end> format
        var match = s_rangeRegex.Match(rangeHeader);
        if (!match.Success)
            throw new FormatException($"Illegal format for range header: {rangeHeader}");

        // Obtain the start and end
        var startText = match.Groups["start"].Value;
        var endText = match.Groups["end"].Value;
        var range = new Range
        {
            Start = !string.IsNullOrEmpty(startText) ? long.Parse(startText) : null,
            End = !string.IsNullOrEmpty(endText) ? long.Parse(endText ) : null
        };

        // Check if we also have an If-Range
        var ifRangeHeader = request.Headers.IfRange.FirstOrDefault();
        if (ifRangeHeader != null)
        {
            // Attempt to parse the date. If we don't understand the If-Range
            // then we need to return the entire file, so we will act as if no
            // range was specified at all.
            if (!DateTime.TryParse(ifRangeHeader, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                return null;

            // Use the date for the 'If'
            range.If = dt;
        }

        // Return the range
        return range;
    }
}
