using System;
using System.Globalization;
using System.Xml.Linq;

namespace NWebDav.Server.Props
{
    public abstract class DavTypedProperty<TEntry, TType> : DavProperty<TEntry> where TEntry : IStoreItem
    {
        private Func<TEntry, TType> _getter;
        private Func<TEntry, TType, DavStatusCode> _setter;

        public new Func<TEntry, TType> Getter
        {
            get { return _getter; }
            set
            {
                _getter = value;
                base.Getter = s =>
                {
                    var v = _getter(s);
                    return Converter != null ? Converter.ToXml(v) : v;
                };
            }
        }

        public new Func<TEntry, TType, DavStatusCode> Setter
        {
            get { return _setter; }
            set
            {
                _setter = value;
                base.Setter = (s, v) =>
                {
                    var tv = Converter != null ? Converter.FromXml(v) : (TType)v;
                    return _setter(s, tv);
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
            public object ToXml(DateTime value) => value.ToString("R");
            public DateTime FromXml(object value) => DateTime.Parse((string)value);
        }

        private static IValidator TypeValidator { get; } = new Rfc1123DateValidator();
        private static IConverter<DateTime> TypeConverter { get; } = new Rfc1123DateConverter();

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
            public object ToXml(Boolean value) => value ? "1" : "0";
            public Boolean FromXml(object value) => int.Parse(value.ToString()) != 0;
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
            public object ToXml(string value) => value;
            public string FromXml(object value) => value.ToString();
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
            public object ToXml(Int32 value) => value.ToString(CultureInfo.InvariantCulture);
            public Int32 FromXml(object value) => int.Parse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture);
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
            public object ToXml(Int64 value) => value.ToString(CultureInfo.InvariantCulture);
            public Int64 FromXml(object value) => int.Parse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        private static IValidator TypeValidator { get; } = new Int64Validator();
        private static IConverter<Int64> TypeConverter { get; } = new Int64Converter();

        public override IValidator Validator => TypeValidator;
        public override IConverter<Int64> Converter => TypeConverter;
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
            public object ToXml(XElement value) => value;
            public XElement FromXml(object value) => (XElement)value;
        }

        private static IValidator TypeValidator { get; } = new XElementValidator();
        private static IConverter<XElement> TypeConverter { get; } = new XElementConverter();

        public override IValidator Validator => TypeValidator;
        public override IConverter<XElement> Converter => TypeConverter;
    }
}
