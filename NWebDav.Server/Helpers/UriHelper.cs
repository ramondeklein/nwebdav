using System;

namespace NWebDav.Server.Helpers
{
    public static class UriHelper
    {
        public static Uri Combine(Uri baseUri, string path)
        {
            var escapedPath = Uri.EscapeDataString(path);
            var uriText = baseUri.OriginalString;
            if (uriText.EndsWith("/"))
                return new Uri(baseUri, escapedPath);
            return new Uri($"{uriText}/{escapedPath}", UriKind.Absolute);
        }
    }
}
