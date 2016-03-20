using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace NWebDav.Server.Props
{
    public class PropertyManager<TEntry> : IPropertyManager where TEntry : IStoreCollectionEntry
    {
        private readonly IDictionary<XName, DavProperty<TEntry>> _properties;

        public PropertyManager(IEnumerable<DavProperty<TEntry>> properties)
        {
            // If properties are supported, then the properties should be set
            if (properties == null)
                throw new ArgumentNullException(nameof(properties));

            // Convert the properties to a dictionary for fast retrieval
            _properties = properties.ToDictionary(p => p.Name);
        }

        public IEnumerable<PropertyInfo> Properties => _properties.Select(p => new PropertyInfo(p.Value.Name, p.Value.IsExpensive));

        public object GetProperty(IStoreCollectionEntry entry, XName name, bool skipExpensive = false)
        {
            // Find the property
            DavProperty<TEntry> property;
            if (!_properties.TryGetValue(name, out property))
                return null;

            // Check if the property has a getter
            if (property.Getter == null)
                return null;

            // Skip expsensive properties
            if (skipExpensive && property.IsExpensive)
                return null;

            // Obtain the value
            var value = property.Getter((TEntry)entry);

            // Validate the value
            property.Validator.Validate(value);
            return value;
        }

        public bool SetProperty(IStoreCollectionEntry entry, XName name, object value)
        {
            // Find the property
            DavProperty<TEntry> property;
            if (!_properties.TryGetValue(name, out property))
                return false;

            // Check if the property has a setter
            if (property.Setter == null)
                return false;

            // Validate the value
            if (!property.Validator.Validate(value))
                return false;

            // Set the value
            if (!property.Setter((TEntry)entry, value))
                return false;
            return true;
        }

        public T GetTypedProperty<T>(IStoreCollectionEntry entry, XName name)
        {
            // Find the property
            DavProperty<TEntry> property;
            if (!_properties.TryGetValue(name, out property))
                return default(T);

            // Check if the property has a getter
            if (property.Getter == null)
                return default(T);

            // Obtain the value
            return (T)property.Getter((TEntry)entry);
        }
    }
}
