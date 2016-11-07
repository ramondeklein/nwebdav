using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

using NWebDav.Server.Http;
using NWebDav.Server.Stores;

namespace NWebDav.Server.Props
{
    public class OverridePropertyManager<TEntry> : IPropertyManager
        where TEntry : IStoreItem
    {
        private readonly Func<TEntry, IStoreItem> _converter;
        private readonly IDictionary<XName, DavProperty<TEntry>> _properties;
        private readonly IPropertyManager _basePropertyManager;

        public OverridePropertyManager(IEnumerable<DavProperty<TEntry>> properties, IPropertyManager basePropertyManager, Func<TEntry, IStoreItem> converter = null)
        {
            // If properties are supported, then the properties should be set
            if (properties == null)
                throw new ArgumentNullException(nameof(properties));
            if (basePropertyManager == null)
                throw new ArgumentNullException(nameof(basePropertyManager));

            // Convert the properties to a dictionary for fast retrieval
            _properties = properties.ToDictionary(p => p.Name);
            _basePropertyManager = basePropertyManager;
            _converter = converter ?? (si => si);

            // Create the property information immediately
            Properties = GetPropertyInfo();
        }

        public IList<PropertyInfo> Properties { get; }

        public async Task<object> GetPropertyAsync(IHttpContext httpContext, IStoreItem item, XName name, bool skipExpensive = false)
        {
            // Find the property
            DavProperty<TEntry> property;
            if (!_properties.TryGetValue(name, out property))
                return await _basePropertyManager.GetPropertyAsync(httpContext, _converter((TEntry)item), name, skipExpensive);

            // Check if the property has a getter
            if (property.GetterAsync == null)
                return await _basePropertyManager.GetPropertyAsync(httpContext, _converter((TEntry)item), name, skipExpensive);

            // Skip expensive properties
            if (skipExpensive && property.IsExpensive)
                return Task.FromResult((object)null);

            // Obtain the value
            return await property.GetterAsync(httpContext, (TEntry)item);
        }

        public Task<DavStatusCode> SetPropertyAsync(IHttpContext httpContext, IStoreItem item, XName name, object value)
        {
            // Find the property
            DavProperty<TEntry> property;
            if (!_properties.TryGetValue(name, out property))
                return _basePropertyManager.SetPropertyAsync(httpContext, _converter((TEntry)item), name, value);

            // Check if the property has a setter
            if (property.SetterAsync == null)
                return _basePropertyManager.SetPropertyAsync(httpContext, _converter((TEntry)item), name, value);

            // Set the value
            return property.SetterAsync(httpContext, (TEntry)item, value);
        }

        private IList<PropertyInfo> GetPropertyInfo()
        {
            // Obtain the base properties that do not have an override
            var basePropertyInfo = _basePropertyManager.Properties.Where(p => !_properties.ContainsKey(p.Name));
            var overridePropertyInfo = _properties.Values.Where(p => p.GetterAsync != null || p.SetterAsync != null).Select(p => new PropertyInfo(p.Name, p.IsExpensive));

            // Combine both lists
            return basePropertyInfo.Concat(overridePropertyInfo).ToList();
        }
    }
}