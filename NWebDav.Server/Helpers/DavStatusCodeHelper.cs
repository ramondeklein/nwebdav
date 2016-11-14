using System;
using System.Linq;
using System.Reflection;

namespace NWebDav.Server.Helpers
{
    public static class DavStatusCodeHelper
    {
        public static string GetStatusDescription(DavStatusCode value, string defaultValue = null)
        {
            // Obtain the member information
            var memberInfo = typeof(DavStatusCode).GetMember(value.ToString()).FirstOrDefault();
            if (memberInfo == null)
                return defaultValue;

            var davStatusCodeAttribute = memberInfo.GetCustomAttribute<DavStatusCodeAttribute>();
            return davStatusCodeAttribute?.Description;
        }
    }
}
