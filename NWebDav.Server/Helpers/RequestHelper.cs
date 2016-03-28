using System;
using System.Collections.Generic;
using System.Linq;
using NWebDav.Server.Http;

namespace NWebDav.Server.Helpers
{
    public class SplitUri
    {
        public Uri CollectionUri { get; set; }
        public string Name { get; set; }
    }

    public static class RequestHelper
    {
        public static SplitUri SplitUri(string uri)
        {
            // Determine the offset of the name
            var slashOffset = uri.LastIndexOf('/');
            if (slashOffset == -1)
                return null;

            // Seperate name from path
            return new SplitUri
            {
                CollectionUri = new Uri(uri.Substring(0, slashOffset)),
                Name = Uri.UnescapeDataString(uri.Substring(slashOffset + 1))
            };
        }

        public static SplitUri SplitUri(Uri uri)
        {
            return SplitUri(uri.AbsoluteUri);
        }

        public static Uri GetDestinationUri(this IHttpRequest request)
        {
            // Obtain the destination
            var destinationHeader = request.GetHeaderValue("Destination");
            if (destinationHeader == null)
                return null;

            // Return the splitted Uri
            return new Uri(destinationHeader);
        }

        public static int GetDepth(this IHttpRequest request)
        {
            // Obtain the depth header (no header means infinity)
            var depthHeader = request.GetHeaderValue("Depth");
            if (depthHeader == null || depthHeader == "infinity")
                return int.MaxValue;

            // Determined depth
            int depth;
            if (!int.TryParse(depthHeader, out depth))
                return int.MaxValue;

            // Return depth
            return depth;
        }

        public static bool GetOverwrite(this IHttpRequest request)
        {
            // Get the Overwrite header
            var overwriteHeader = request.GetHeaderValue("Overwrite") ?? "T";

            // It should be set to "T" (true) or "F" (false)
            return overwriteHeader.ToUpperInvariant() == "T";
        }


        public static IList<int> GetTimeouts(this IHttpRequest request)
        {
            // Get the value of the timeout header as a string
            var timeoutHeader = request.GetHeaderValue("Timeout");
            if (string.IsNullOrEmpty(timeoutHeader))
                return null;

            // Return each item
            Func<string, int> parseTimeout = t =>
            {
                // Check for 'infinite'
                if (t == "Infinite")
                    return -1;

                // Parse the number of seconds
                int timeout;
                if (!t.StartsWith("Second-") || !int.TryParse(t, out timeout))
                    return 0;
                return timeout;
            };

            // Return the timeout values
            return timeoutHeader.Split(new [] {','}, StringSplitOptions.RemoveEmptyEntries).Select(parseTimeout).Where(t => t != 0).ToArray();
        }

        public static Uri GetLockToken(this IHttpRequest request)
        {
            // Get the value of the lock-token header as a string
            var lockTokenHeader = request.GetHeaderValue("Lock-Token");
            if (string.IsNullOrEmpty(lockTokenHeader))
                return null;

            // Strip the brackets from the header
            if (!lockTokenHeader.StartsWith("<") || !lockTokenHeader.EndsWith(">"))
                return null;

            // Create an Uri of the intermediate part
            return new Uri(lockTokenHeader.Substring(1, lockTokenHeader.Length-2), UriKind.Absolute);
        }
    }
}
