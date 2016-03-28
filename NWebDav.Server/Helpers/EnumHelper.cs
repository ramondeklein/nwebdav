using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace NWebDav.Server.Helpers
{
    public static class EnumHelper
    {
        public static string GetEnumValue<TEnum>(TEnum value, string defaultValue = null) where TEnum : struct
        {
            // Obtain the member information
            var memberInfo = typeof(TEnum).GetMember(value.ToString()).FirstOrDefault();
            if (memberInfo == null)
                return defaultValue;

            var displayAttribute = memberInfo.GetCustomAttributes<DisplayAttribute>().FirstOrDefault();
            return displayAttribute?.Description;
        }
    }
}
