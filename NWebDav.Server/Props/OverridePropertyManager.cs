using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
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
        }

        public IEnumerable<PropertyInfo> Properties => _properties.Select(p => new PropertyInfo(p.Value.Name, p.Value.IsExpensive));

        public object GetProperty(IPrincipal principal, IStoreItem item, XName name, bool skipExpensive = false)
        {
            // Find the property
            DavProperty<TEntry> property;
            if (!_properties.TryGetValue(name, out property))
                return _basePropertyManager.GetProperty(principal, _converter((TEntry)item), name, skipExpensive);

            // Check if the property has a getter
            if (property.Getter == null)
                return _basePropertyManager.GetProperty(principal, _converter((TEntry)item), name, skipExpensive);

            // Skip expsensive properties
            if (skipExpensive && property.IsExpensive)
                return null;

            // Obtain the value
            var value = property.Getter(principal, (TEntry)item);

            // Validate the value
            property.Validator.Validate(value);
            return value;
        }

        public DavStatusCode SetProperty(IPrincipal principal, IStoreItem item, XName name, object value)
        {
            // Find the property
            DavProperty<TEntry> property;
            if (!_properties.TryGetValue(name, out property))
                return _basePropertyManager.SetProperty(principal, _converter((TEntry)item), name, value);

            // Check if the property has a setter
            if (property.Setter == null)
                return _basePropertyManager.SetProperty(principal, _converter((TEntry)item), name, value);

            // Validate the value (if not null)
            if (value != null && !property.Validator.Validate(value))
                return DavStatusCode.Conflict;

            // Set the value
            return property.Setter(principal, (TEntry)item, value);
        }
    }
}