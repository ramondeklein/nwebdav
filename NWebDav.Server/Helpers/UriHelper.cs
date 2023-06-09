using System;

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
            return entryUri
                .AbsoluteUri
                .Replace("#", "%23")
                .Replace("[", "%5B")
                .Replace("]", "%5D");
        }

        public static string GetDecodedPath(Uri uri)
        {
            return uri.LocalPath + Uri.UnescapeDataString(uri.Fragment);
        }

        internal static Uri RemoveRootDirectory(Uri uri, string rootDirectory)
        {
            return new($"{uri.Scheme}://{uri.Host}:{uri.Port}{new System.Text.RegularExpressions.Regex($"^\\/{rootDirectory}").Replace(uri.LocalPath, string.Empty)}");
        }
    }
}
