using System;
using System.Collections.Generic;
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
            var overwriteHeader = request.Headers["Overwrite"] ?? "T";
            return overwriteHeader.ToUpperInvariant() == "T";
        }
    }
}
