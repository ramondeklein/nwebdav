using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using NWebDav.Server.Stores;

namespace NWebDav.Server.Props;

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
        object ToXml(TType value);
            
        /// <summary>
        /// Get the typed value of the specified XML representation.
        /// </summary>
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
        TType FromXml(object value);
    }

    private Func<TEntry, TType> _getter;
    private Func<TEntry, TType, DavStatusCode> _setter;
    private Func<TEntry, CancellationToken, Task<TType>> _getterAsync;
    private Func<TEntry, TType, CancellationToken, Task<DavStatusCode>> _setterAsync;

    /// <summary>
    /// Converter to convert property values from/to XML for this type.
    /// </summary>
    /// <remarks>
    /// This property should be set from the derived typed property implementation.
    /// </remarks>
    public abstract IConverter? Converter { get; }

    /// <summary>
    /// Synchronous getter to obtain the property value.
    /// </summary>
    public Func<TEntry, TType> Getter
    {
        get => _getter;
        init
        {
            _getter = value;
            base.GetterAsync = (s, ct) =>
            {
                ct.ThrowIfCancellationRequested();
                var v = _getter(s);
                return Task.FromResult(Converter != null ? Converter.ToXml(v) : v);
            };
        }
    }

    /// <summary>
    /// Synchronous setter to set the property value.
    /// </summary>
    public Func<TEntry, TType, DavStatusCode> Setter
    {
        get => _setter;
        init
        {
            _setter = value;
            base.SetterAsync = (s, v, ct) =>
            {
                ct.ThrowIfCancellationRequested();
                var tv = Converter != null ? Converter.FromXml(v) : (TType)v;
                return Task.FromResult(_setter(s, tv));
            };
        }
    }

    /// <summary>
    /// Asynchronous getter to obtain the property value.
    /// </summary>
    public new Func<TEntry, CancellationToken, Task<TType>> GetterAsync
    {
        get => _getterAsync;
        init
        {
            _getterAsync = value;
            base.GetterAsync = async (s, ct) =>
            {
                var v = await _getterAsync(s, ct).ConfigureAwait(false);
                return Converter != null ? Converter.ToXml(v) : v;
            };
        }
    }

    /// <summary>
    /// Asynchronous setter to set the property value.
    /// </summary>
    public new Func<TEntry, TType, CancellationToken, Task<DavStatusCode>> SetterAsync
    {
        get => _setterAsync;
        init
        {
            _setterAsync = value;
            base.SetterAsync = (s, v, ct) =>
            {
                var tv = Converter != null ? Converter.FromXml(v) : (TType)v;
                return _setterAsync(s, tv, ct);
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
        public object ToXml(DateTime value) => value.ToString("R");
        public DateTime FromXml(object value) => DateTime.Parse((string)value, CultureInfo.InvariantCulture);
    }

    private static IConverter TypeConverter { get; } = new Rfc1123DateConverter();

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
    private readonly Iso8601DateConverter _converter;
        
    protected DavIso8601Date(IHttpContextAccessor httpContextAccessor)
    {
        _converter = new Iso8601DateConverter(httpContextAccessor);
    }
        
    private class Iso8601DateConverter : IConverter
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public Iso8601DateConverter(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }
            
        public object ToXml(DateTime value)
        {
            // The older built-in Windows WebDAV clients have a problem, so
            // they cannot deal with more than 3 digits for the
            // milliseconds.
            if (HasIso8601FractionBug)
            {
                // We need to recreate the date again, because the Windows 7
                // WebDAV client cannot 
                var dt = new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, value.Millisecond, DateTimeKind.Utc);
                return XmlConvert.ToString(dt, XmlDateTimeSerializationMode.Utc);
            }

            return XmlConvert.ToString(value, XmlDateTimeSerializationMode.Utc);
        }

        public DateTime FromXml(object value) => XmlConvert.ToDateTime((string)value, XmlDateTimeSerializationMode.Utc);

        private bool HasIso8601FractionBug
        {
            get
            {
                var userAgent = _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.FirstOrDefault();
                _ = userAgent;  // TODO: Determine if this bug is present based on the user-agent
                return true;
            }
        }
    }

    /// <summary>
    /// Converter to map ISO 8601 dates to/from a <see cref="DateTime"/>.
    /// </summary>
    public override IConverter Converter => _converter;
}

/// <summary>
/// Abstract base class representing a single DAV property using a
/// <see cref="bool"/> type.
/// </summary>
/// <typeparam name="TEntry">
/// Store item or collection to which this DAV property applies.
/// </typeparam>
public abstract class DavBoolean<TEntry> : DavTypedProperty<TEntry, bool> where TEntry : IStoreItem
{
    private class BooleanConverter : IConverter
    {
        public object ToXml(bool value) => value ? "1" : "0";
        public bool FromXml(object value) => int.Parse(value.ToString()) != 0;
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
        public object ToXml(string value) => value;
        public string FromXml(object value) => value.ToString();
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
public abstract class DavInt32<TEntry> : DavTypedProperty<TEntry, int> where TEntry : IStoreItem
{
    private class Int32Converter : IConverter
    {
        public object ToXml(int value) => value.ToString(CultureInfo.InvariantCulture);
        public int FromXml(object value) => int.Parse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture);
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
public abstract class DavInt64<TEntry> : DavTypedProperty<TEntry, long> where TEntry : IStoreItem
{
    private class Int64Converter : IConverter
    {
        public object ToXml(long value) => value.ToString(CultureInfo.InvariantCulture);
        public long FromXml(object value) => int.Parse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture);
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
        public object ToXml(IEnumerable<XElement> value) => value;
        public IEnumerable<XElement> FromXml(object value) => (IEnumerable<XElement>)value;
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
        public object ToXml(XElement value) => value;
        public XElement FromXml(object value) => (XElement)value;
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
        public object ToXml(Uri value) => value.ToString();
        public Uri FromXml(object value) => new Uri((string)value);
    }

    public static IConverter TypeConverter { get; } = new UriConverter();

    /// <summary>
    /// Converter to map an XML string to/from a <see cref="Uri"/>.
    /// </summary>
    public override IConverter Converter => TypeConverter;
}