using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

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

        public static Uri GetDestinationUri(this HttpListenerRequest request)
        {
            // Obtain the destination
            var destinationHeader = request.Headers["Destination"];
            if (destinationHeader == null)
                return null;

            // Return the splitted Uri
            return new Uri(destinationHeader);
        }

        public static int GetDepth(this HttpListenerRequest request)
        {
            // Obtain the depth header (no header means infinity)
            var depthHeader = request.Headers["Depth"];
            if (depthHeader == null || depthHeader.Equals("infinity", StringComparison.InvariantCulture))
                return int.MaxValue;

            // Determined depth
            int depth;
            if (!int.TryParse(depthHeader, out depth))
                return int.MaxValue;

            // Return depth
            return depth;
        }

        public static bool GetOverwrite(this HttpListenerRequest request)
        {
            // Get the Overwrite header
            var overwriteHeader = request.Headers["Overwrite"] ?? "T";

            // It should be set to "T" (true) or "F" (false)
            return overwriteHeader.ToUpperInvariant() == "T";
        }


        public static IList<int> GetTimeouts(this HttpListenerRequest request)
        {
            // Get the value of the timeout header as a string
            var timeoutHeader = request.Headers["Timeout"];
            if (string.IsNullOrEmpty(timeoutHeader))
                return null;

            // Return each item
            Func<string, int> parseTimeout = t =>
            {
                // Check for 'infinite'
                if (t.Equals("Infinite", StringComparison.InvariantCulture))
                    return -1;

                // Parse the number of seconds
                int timeout;
                if (!t.StartsWith("Second-", StringComparison.InvariantCulture) || !int.TryParse(t, out timeout))
                    return 0;
                return timeout;
            };

            // Return the timeout values
            return timeoutHeader.Split(new [] {','}, StringSplitOptions.RemoveEmptyEntries).Select(parseTimeout).Where(t => t != 0).ToArray();
        }

    }
}
