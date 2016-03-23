using System;
using System.Globalization;
using System.IO;
using System.Xml.Linq;
using NWebDav.Server.Stores;

namespace NWebDav.Server.Props
{
    public class Win32CreationTime<TEntry> : DavRfc1123Date<TEntry> where TEntry : IStoreItem
    {
        public override XName Name => WebDavNamespaces.Win32Ns + "Win32CreationTime";
    }

    public class Win32LastAccessTime<TEntry> : DavRfc1123Date<TEntry> where TEntry : IStoreItem
    {
        public override XName Name => WebDavNamespaces.Win32Ns + "Win32LastAccessTime";
    }

    public class Win32LastModifiedTime<TEntry> : DavRfc1123Date<TEntry> where TEntry : IStoreItem
    {
        public override XName Name => WebDavNamespaces.Win32Ns + "Win32LastModifiedTime";
    }

    public class Win32FileAttributes<TEntry> : DavTypedProperty<TEntry, FileAttributes> where TEntry : IStoreItem
    {
        private class FileAttributesValidator : IValidator
        {
            public bool Validate(object value)
            {
                var intString = value as string;
                if (intString == null)
                    return false;

                // Check if it's a valid HEX number
                int result;
                return int.TryParse(intString, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out result);
            }
        }

        private class FileAttributesConverter : IConverter<FileAttributes>
        {
            public object ToXml(FileAttributes value) => ((int)value).ToString("X8");
            public FileAttributes FromXml(object value) => (FileAttributes)Convert.ToInt32((string)value, 16);
        }

        private static IValidator TypeValidator { get; } = new FileAttributesValidator();
        private static IConverter<FileAttributes> TypeConverter { get; } = new FileAttributesConverter();

        public override XName Name => WebDavNamespaces.Win32Ns + "Win32FileAttributes";
        public override IValidator Validator => TypeValidator;
        public override IConverter<FileAttributes> Converter => TypeConverter;
    }
}
