using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using System.Xml.Linq;

using NWebDav.Server.Http;
using NWebDav.Server.Stores;

namespace NWebDav.Server.Props
{
    public abstract class DavTypedProperty<TEntry, TType> : DavProperty<TEntry> where TEntry : IStoreItem
    {
        private Func<IHttpContext, TEntry, TType> _getter;
        private Func<IHttpContext, TEntry, TType, DavStatusCode> _setter;

        public new Func<IHttpContext, TEntry, TType> Getter
        {
            get { return _getter; }
            set
            {
                _getter = value;
                base.Getter = (c,s) =>
                {
                    var v = _getter(c, s);
                    return Converter != null ? Converter.ToXml(c, v) : v;
                };
            }
        }

        public new Func<IHttpContext, TEntry, TType, DavStatusCode> Setter
        {
            get { return _setter; }
            set
            {
                _setter = value;
                base.Setter = (c, s, v) =>
                {
                    var tv = Converter != null ? Converter.FromXml(c, v) : (TType)v;
                    return _setter(c, s, tv);
                };
            }
        }

        public abstract IConverter<TType> Converter { get; }
    }

    public abstract class DavRfc1123Date<TEntry> : DavTypedProperty<TEntry, DateTime> where TEntry : IStoreItem
    {
        private class Rfc1123DateValidator : IValidator
        {
            public bool Validate(object value)
            {
                var dateString = value as string;
                if (dateString == null)
                    return false;

                // TODO: Check if it matches the RFC1123
                return true;
            }
        }

        private class Rfc1123DateConverter : IConverter<DateTime>
        {
            public object ToXml(IHttpContext httpContext, DateTime value) => value.ToString("R");
            public DateTime FromXml(IHttpContext httpContext, object value) => DateTime.Parse((string)value);
        }

        private static IValidator TypeValidator { get; } = new Rfc1123DateValidator();
        private static IConverter<DateTime> TypeConverter { get; } = new Rfc1123DateConverter();

        public override IValidator Validator => TypeValidator;
        public override IConverter<DateTime> Converter => TypeConverter;
    }

    public abstract class DavIso8601Date<TEntry> : DavTypedProperty<TEntry, DateTime> where TEntry : IStoreItem
    {
        private class DavIso8601DateValidator : IValidator
        {
            public bool Validate(object value)
            {
                var dateString = value as string;
                if (dateString == null)
                    return false;

                // TODO: Check if it matches the ISO8601
                return true;
            }
        }

        private class Iso8601DateConverter : IConverter<DateTime>
        {
            public object ToXml(IHttpContext httpContext, DateTime value)
            {
                // We need to recreate the date again, because the Windows 7
                // WebDAV client cannot deal with more than 3 digits for the
                // milliseconds.
                var dt = new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, value.Millisecond, DateTimeKind.Utc);
                return XmlConvert.ToString(dt, XmlDateTimeSerializationMode.Utc);
            }
            public DateTime FromXml(IHttpContext httpContext, object value) => XmlConvert.ToDateTime((string)value, XmlDateTimeSerializationMode.Utc);
        }

        private static IValidator TypeValidator { get; } = new DavIso8601DateValidator();
        private static IConverter<DateTime> TypeConverter { get; } = new Iso8601DateConverter();

        public override IValidator Validator => TypeValidator;
        public override IConverter<DateTime> Converter => TypeConverter;
    }

    public abstract class DavBoolean<TEntry> : DavTypedProperty<TEntry, Boolean> where TEntry : IStoreItem
    {
        private class BooleanValidator : IValidator
        {
            public bool Validate(object value)
            {
                var boolString = value as string;
                if (boolString == null)
                    return false;

                // Check if it's a valid number
                return boolString == "0" || boolString == "1";
            }
        }

        private class BooleanConverter : IConverter<Boolean>
        {
            public object ToXml(IHttpContext httpContext, Boolean value) => value ? "1" : "0";
            public Boolean FromXml(IHttpContext httpContext, object value) => int.Parse(value.ToString()) != 0;
        }

        private static IValidator TypeValidator { get; } = new BooleanValidator();
        private static IConverter<Boolean> TypeConverter { get; } = new BooleanConverter();

        public override IValidator Validator => TypeValidator;
        public override IConverter<Boolean> Converter => TypeConverter;
    }

    public abstract class DavString<TEntry> : DavTypedProperty<TEntry, string> where TEntry : IStoreItem
    {
        private class StringConverter : IConverter<string>
        {
            public object ToXml(IHttpContext httpContext, string value) => value;
            public string FromXml(IHttpContext httpContext, object value) => value.ToString();
        }

        private static IConverter<string> TypeConverter { get; } = new StringConverter();

        public override IConverter<string> Converter => TypeConverter;
    }

    public abstract class DavInt32<TEntry> : DavTypedProperty<TEntry, Int32> where TEntry : IStoreItem
    {
        private class Int32Validator : IValidator
        {
            public bool Validate(object value)
            {
                var intString = value as string;
                if (intString == null)
                    return false;

                // Check if it's a valid number
                int result;
                return int.TryParse(intString, NumberStyles.None, CultureInfo.InvariantCulture, out result);
            }
        }

        private class Int32Converter : IConverter<Int32>
        {
            public object ToXml(IHttpContext httpContext, Int32 value) => value.ToString(CultureInfo.InvariantCulture);
            public Int32 FromXml(IHttpContext httpContext, object value) => int.Parse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        private static IValidator TypeValidator { get; } = new Int32Validator();
        private static IConverter<Int32> TypeConverter { get; } = new Int32Converter();

        public override IValidator Validator => TypeValidator;
        public override IConverter<Int32> Converter => TypeConverter;
    }

    public abstract class DavInt64<TEntry> : DavTypedProperty<TEntry, Int64> where TEntry : IStoreItem
    {
        private class Int64Validator : IValidator
        {
            public bool Validate(object value)
            {
                var intString = value as string;
                if (intString == null)
                    return false;

                // Check if it's a valid number
                int result;
                return int.TryParse(intString, NumberStyles.None, CultureInfo.InvariantCulture, out result);
            }
        }

        private class Int64Converter : IConverter<Int64>
        {
            public object ToXml(IHttpContext httpContext, Int64 value) => value.ToString(CultureInfo.InvariantCulture);
            public Int64 FromXml(IHttpContext httpContext, object value) => int.Parse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        private static IValidator TypeValidator { get; } = new Int64Validator();
        private static IConverter<Int64> TypeConverter { get; } = new Int64Converter();

        public override IValidator Validator => TypeValidator;
        public override IConverter<Int64> Converter => TypeConverter;
    }

    public abstract class DavXElementArray<TEntry> : DavTypedProperty<TEntry, IEnumerable<XElement>> where TEntry : IStoreItem
    {
        private class XElementArrayValidator : IValidator
        {
            public bool Validate(object value)
            {
                return value == null || value is IEnumerable<XElement>;

                // TODO: Extend this with (optional) schema validation
            }
        }

        private class XElementArrayConverter : IConverter<IEnumerable<XElement>>
        {
            public object ToXml(IHttpContext httpContext, IEnumerable<XElement> value) => value;
            public IEnumerable<XElement> FromXml(IHttpContext httpContext, object value) => (IEnumerable<XElement>)value;
        }

        private static IValidator TypeValidator { get; } = new XElementArrayValidator();
        private static IConverter<IEnumerable<XElement>> TypeConverter { get; } = new XElementArrayConverter();

        public override IValidator Validator => TypeValidator;
        public override IConverter<IEnumerable<XElement>> Converter => TypeConverter;
    }

    public abstract class DavXElement<TEntry> : DavTypedProperty<TEntry, XElement> where TEntry : IStoreItem
    {
        private class XElementValidator : IValidator
        {
            public bool Validate(object value)
            {
                return value == null || value is XElement;

                // TODO: Extend this with (optional) schema validation
            }
        }

        private class XElementConverter : IConverter<XElement>
        {
            public object ToXml(IHttpContext httpContext, XElement value) => value;
            public XElement FromXml(IHttpContext httpContext, object value) => (XElement)value;
        }

        private static IValidator TypeValidator { get; } = new XElementValidator();
        private static IConverter<XElement> TypeConverter { get; } = new XElementConverter();

        public override IValidator Validator => TypeValidator;
        public override IConverter<XElement> Converter => TypeConverter;
    }

    public abstract class DavUri<TEntry> : DavTypedProperty<TEntry, Uri> where TEntry : IStoreItem
    {
        private class UriValidator : IValidator
        {
            public bool Validate(object value)
            {
                var uriString = value as string;
                if (uriString == null)
                    return false;

                // TODO: Check if URI is valid
                return true;
            }
        }

        private class UriConverter : IConverter<Uri>
        {
            public object ToXml(IHttpContext httpContext, Uri value) => value.ToString();
            public Uri FromXml(IHttpContext httpContext, object value) => new Uri((string)value);
        }

        private static IValidator TypeValidator { get; } = new UriValidator();
        private static IConverter<Uri> TypeConverter { get; } = new UriConverter();

        public override IValidator Validator => TypeValidator;
        public override IConverter<Uri> Converter => TypeConverter;
    }

}
