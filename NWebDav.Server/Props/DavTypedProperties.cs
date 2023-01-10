﻿using NWebDav.Server.Http;
using NWebDav.Server.Stores;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace NWebDav.Server.Props
{
    /// <summary>
    /// Abstract base class representing a single DAV property with a specific
    /// CLR type.
    /// </summary>
    /// <remarks>
    /// A dedicated converter should be implemented to convert the property 
    /// value to/from an XML value. This class supports both synchronous and
    /// asynchronous accessor methods. To improve scalability, it is
    /// recommended to use the asynchronous methods for properties that require
    /// some time to get/set.
    /// </remarks>
    /// <typeparam name="TEntry">
    /// Store item or collection to which this DAV property applies.
    /// </typeparam>
    /// <typeparam name="TType">
    /// CLR type of the property.
    /// </typeparam>
    public abstract class DavTypedProperty<TEntry, TType> : DavProperty<TEntry> where TEntry : IStoreItem
    {
        /// <summary>
        /// Converter defining methods to convert property values from/to XML.
        /// </summary>
        public interface IConverter
        {
            /// <summary>
            /// Get the XML representation of the specified value.
            /// </summary>
            /// <param name="context">
            /// Current HTTP context.
            /// </param>
            /// <param name="value">
            /// Value that needs to be converted to XML output.
            /// </param>
            /// <returns>
            /// The XML representation of the <paramref name="value"/>. The
            /// XML output should either be a <see cref="System.String"/> or
            /// an <see cref="System.Xml.Linq.XElement"/>.
            /// </returns>
            /// <remarks>
            /// The current HTTP context can be used to generate XML that is
            /// compatible with the requesting WebDAV client.
            /// </remarks>
            object ToXml(IHttpContext context, TType value);
            
            /// <summary>
            /// Get the typed value of the specified XML representation.
            /// </summary>
            /// <param name="context">
            /// Current HTTP context.
            /// </param>
            /// <param name="value">
            /// The XML value that needs to be converted to the target
            /// type. This value is always a <see cref="System.String"/>
            /// or an <see cref="System.Xml.Linq.XElement"/>.
            /// </param>
            /// <returns>
            /// The typed value of the XML representation.
            /// </returns>
            /// <remarks>
            /// The current HTTP context can be used to generate XML that is
            /// compatible with the requesting WebDAV client.
            /// </remarks>
            TType FromXml(IHttpContext context, object value);
        }

        private Func<IHttpContext, TEntry, TType> _getter;
        private Func<IHttpContext, TEntry, TType, HttpStatusCode> _setter;
        private Func<IHttpContext, TEntry, Task<TType>> _getterAsync;
        private Func<IHttpContext, TEntry, TType, Task<HttpStatusCode>> _setterAsync;

        /// <summary>
        /// Converter to convert property values from/to XML for this type.
        /// </summary>
        /// <remarks>
        /// This property should be set from the derived typed property implementation.
        /// </remarks>
        public abstract IConverter Converter { get; }

        /// <summary>
        /// Synchronous getter to obtain the property value.
        /// </summary>
        public Func<IHttpContext, TEntry, TType> Getter
        {
            get => _getter;
            set
            {
                _getter = value;
                base.GetterAsync = (c, s) =>
                {
                    var v = _getter(c, s);
                    return Task.FromResult(Converter != null ? Converter.ToXml(c, v) : v);
                };
            }
        }

        /// <summary>
        /// Synchronous setter to set the property value.
        /// </summary>
        public Func<IHttpContext, TEntry, TType, HttpStatusCode> Setter
        {
            get => _setter;
            set
            {
                _setter = value;
                base.SetterAsync = (c, s, v) =>
                {
                    var tv = Converter != null ? Converter.FromXml(c, v) : (TType)v;
                    return Task.FromResult(_setter(c, s, tv));
                };
            }
        }

        /// <summary>
        /// Asynchronous getter to obtain the property value.
        /// </summary>
        public new Func<IHttpContext, TEntry, Task<TType>> GetterAsync
        {
            get => _getterAsync;
            set
            {
                _getterAsync = value;
                base.GetterAsync = async (c, s) =>
                {
                    var v = await _getterAsync(c, s).ConfigureAwait(false);
                    return Converter != null ? Converter.ToXml(c, v) : v;
                };
            }
        }

        /// <summary>
        /// Asynchronous setter to set the property value.
        /// </summary>
        public new Func<IHttpContext, TEntry, TType, Task<HttpStatusCode>> SetterAsync
        {
            get => _setterAsync;
            set
            {
                _setterAsync = value;
                base.SetterAsync = (c, s, v) =>
                {
                    var tv = Converter != null ? Converter.FromXml(c, v) : (TType)v;
                    return _setterAsync(c, s, tv);
                };
            }
        }
    }

    /// <summary>
    /// Abstract base class representing a single DAV property using an
    /// RFC1123 date type (mapped to <see cref="DateTime"/>).
    /// </summary>
    /// <typeparam name="TEntry">
    /// Store item or collection to which this DAV property applies.
    /// </typeparam>
    public abstract class DavRfc1123Date<TEntry> : DavTypedProperty<TEntry, DateTime> where TEntry : IStoreItem
    {
        private class Rfc1123DateConverter : IConverter
        {
            public object ToXml(IHttpContext context, DateTime value) => value.ToString("R");
            public DateTime FromXml(IHttpContext context, object value) => DateTime.Parse((string)value, CultureInfo.InvariantCulture);
        }

        public static IConverter TypeConverter { get; } = new Rfc1123DateConverter();

        /// <summary>
        /// Converter to map RFC1123 dates to/from a <see cref="DateTime"/>.
        /// </summary>
        public override IConverter Converter => TypeConverter;
    }

    /// <summary>
    /// Abstract base class representing a single DAV property using an
    /// ISO 8601 date type (mapped to <see cref="DateTime"/>).
    /// </summary>
    /// <typeparam name="TEntry">
    /// Store item or collection to which this DAV property applies.
    /// </typeparam>
    public abstract class DavIso8601Date<TEntry> : DavTypedProperty<TEntry, DateTime> where TEntry : IStoreItem
    {
        private class Iso8601DateConverter : IConverter
        {
            public object ToXml(IHttpContext context, DateTime value)
            {
                // The older built-in Windows WebDAV clients have a problem, so
                // they cannot deal with more than 3 digits for the
                // milliseconds.
                if (HasIso8601FractionBug(context))
                {
                    // We need to recreate the date again, because the Windows 7
                    // WebDAV client cannot 
                    var dt = new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, value.Millisecond, DateTimeKind.Utc);
                    return XmlConvert.ToString(dt, XmlDateTimeSerializationMode.Utc);
                }

                return XmlConvert.ToString(value, XmlDateTimeSerializationMode.Utc);
            }

            public DateTime FromXml(IHttpContext context, object value) => XmlConvert.ToDateTime((string)value, XmlDateTimeSerializationMode.Utc);

            private bool HasIso8601FractionBug(IHttpContext context)
            {
                // TODO: Determine which WebDAV clients have this bug
                return true;
            }
        }

        public static IConverter TypeConverter { get; } = new Iso8601DateConverter();

        /// <summary>
        /// Converter to map ISO 8601 dates to/from a <see cref="DateTime"/>.
        /// </summary>
        public override IConverter Converter => TypeConverter;
    }

    /// <summary>
    /// Abstract base class representing a single DAV property using a
    /// <see cref="bool"/> type.
    /// </summary>
    /// <typeparam name="TEntry">
    /// Store item or collection to which this DAV property applies.
    /// </typeparam>
    public abstract class DavBoolean<TEntry> : DavTypedProperty<TEntry, Boolean> where TEntry : IStoreItem
    {
        private class BooleanConverter : IConverter
        {
            public object ToXml(IHttpContext context, Boolean value) => value ? "1" : "0";
            public Boolean FromXml(IHttpContext context, object value) => int.Parse(value.ToString()) != 0;
        }

        public static IConverter TypeConverter { get; } = new BooleanConverter();

        /// <summary>
        /// Converter to map an XML boolean to/from a <see cref="bool"/>.
        /// </summary>
        public override IConverter Converter => TypeConverter;
    }

    /// <summary>
    /// Abstract base class representing a single DAV property using a
    /// <see cref="string"/> type.
    /// </summary>
    /// <typeparam name="TEntry">
    /// Store item or collection to which this DAV property applies.
    /// </typeparam>
    public abstract class DavString<TEntry> : DavTypedProperty<TEntry, string> where TEntry : IStoreItem
    {
        private class StringConverter : IConverter
        {
            public object ToXml(IHttpContext context, string value) => value;
            public string FromXml(IHttpContext context, object value) => value.ToString();
        }

        public static IConverter TypeConverter { get; } = new StringConverter();

        /// <summary>
        /// Converter to map an XML string to/from a <see cref="string"/>.
        /// </summary>
        public override IConverter Converter => TypeConverter;
    }

    /// <summary>
    /// Abstract base class representing a single DAV property using an
    /// <see cref="int"/> type.
    /// </summary>
    /// <typeparam name="TEntry">
    /// Store item or collection to which this DAV property applies.
    /// </typeparam>
    public abstract class DavInt32<TEntry> : DavTypedProperty<TEntry, Int32> where TEntry : IStoreItem
    {
        private class Int32Converter : IConverter
        {
            public object ToXml(IHttpContext context, Int32 value) => value.ToString(CultureInfo.InvariantCulture);
            public Int32 FromXml(IHttpContext context, object value) => int.Parse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        public static IConverter TypeConverter { get; } = new Int32Converter();

        /// <summary>
        /// Converter to map an XML number to/from a <see cref="int"/>.
        /// </summary>
        public override IConverter Converter => TypeConverter;
    }

    /// <summary>
    /// Abstract base class representing a single DAV property using a
    /// <see cref="long"/> type.
    /// </summary>
    /// <typeparam name="TEntry">
    /// Store item or collection to which this DAV property applies.
    /// </typeparam>
    public abstract class DavInt64<TEntry> : DavTypedProperty<TEntry, Int64> where TEntry : IStoreItem
    {
        private class Int64Converter : IConverter
        {
            public object ToXml(IHttpContext context, Int64 value) => value.ToString(CultureInfo.InvariantCulture);
            public Int64 FromXml(IHttpContext context, object value) => int.Parse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        public static IConverter TypeConverter { get; } = new Int64Converter();

        /// <summary>
        /// Converter to map an XML number to/from a <see cref="long"/>.
        /// </summary>
        public override IConverter Converter => TypeConverter;
    }

    /// <summary>
    /// Abstract base class representing a single DAV property using an
    /// <see cref="XElement"/> array.
    /// </summary>
    /// <typeparam name="TEntry">
    /// Store item or collection to which this DAV property applies.
    /// </typeparam>
    public abstract class DavXElementArray<TEntry> : DavTypedProperty<TEntry, IEnumerable<XElement>> where TEntry : IStoreItem
    {
        private class XElementArrayConverter : IConverter
        {
            public object ToXml(IHttpContext context, IEnumerable<XElement> value) => value;
            public IEnumerable<XElement> FromXml(IHttpContext context, object value) => (IEnumerable<XElement>)value;
        }

        public static IConverter TypeConverter { get; } = new XElementArrayConverter();

        /// <summary>
        /// Converter to map an XML number to/from an <see cref="XElement"/> array.
        /// </summary>
        public override IConverter Converter => TypeConverter;
    }

    /// <summary>
    /// Abstract base class representing a single DAV property using an
    /// <see cref="XElement"/> type.
    /// </summary>
    /// <typeparam name="TEntry">
    /// Store item or collection to which this DAV property applies.
    /// </typeparam>
    public abstract class DavXElement<TEntry> : DavTypedProperty<TEntry, XElement> where TEntry : IStoreItem
    {
        private class XElementConverter : IConverter
        {
            public object ToXml(IHttpContext context, XElement value) => value;
            public XElement FromXml(IHttpContext context, object value) => (XElement)value;
        }

        public static IConverter TypeConverter { get; } = new XElementConverter();

        /// <summary>
        /// Converter to map an XML number to/from a <see cref="XElement"/>.
        /// </summary>
        public override IConverter Converter => TypeConverter;
    }

    /// <summary>
    /// Abstract base class representing a single DAV property using an
    /// <see cref="Uri"/> type.
    /// </summary>
    /// <typeparam name="TEntry">
    /// Store item or collection to which this DAV property applies.
    /// </typeparam>
    public abstract class DavUri<TEntry> : DavTypedProperty<TEntry, Uri> where TEntry : IStoreItem
    {
        private class UriConverter : IConverter
        {
            public object ToXml(IHttpContext context, Uri value) => value.ToString();
            public Uri FromXml(IHttpContext context, object value) => new Uri((string)value);
        }

        public static IConverter TypeConverter { get; } = new UriConverter();

        /// <summary>
        /// Converter to map an XML string to/from a <see cref="Uri"/>.
        /// </summary>
        public override IConverter Converter => TypeConverter;
    }
}
