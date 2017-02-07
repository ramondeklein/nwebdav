using System;
using System.Linq;

namespace NWebDav.Server.Helpers
{
    public static class UriHelper
    {
        public static Uri Combine(Uri baseUri, string path)
        {
            var uriText = baseUri.OriginalString;
            if (uriText.EndsWith("/"))
                uriText = uriText.Substring(0, uriText.Length - 1);
            return new Uri($"{uriText}/{path}", UriKind.Absolute);
        }

        public static string ToEncodedString(Uri entryUri)
        {
            var path = entryUri.LocalPath + entryUri.Fragment;
            var encodedPath = string.Join("/", path.Split('/').Select(Uri.EscapeDataString));
            return $"{entryUri.Scheme}://{entryUri.Authority}{encodedPath}";
        }
    }
}
