using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Xml.Linq;
using NWebDav.Server.Stores;

namespace NWebDav.Server.Props
{
    public class PropertyManager<TEntry> : IPropertyManager where TEntry : IStoreItem
    {
        private readonly IDictionary<XName, DavProperty<TEntry>> _properties;

        public PropertyManager(IEnumerable<DavProperty<TEntry>> properties)
        {
            // If properties are supported, then the properties should be set
            if (properties == null)
                throw new ArgumentNullException(nameof(properties));

            // Convert the properties to a dictionary for fast retrieval
            _properties = properties.ToDictionary(p => p.Name);

            // Create the property information immediately
            Properties = _properties.Select(p => new PropertyInfo(p.Value.Name, p.Value.IsExpensive)).ToList();
        }

        public IEnumerable<PropertyInfo> Properties { get; }

        public object GetProperty(IPrincipal principal, IStoreItem item, XName name, bool skipExpensive = false)
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
                return DavStatusCode.NotFound;

            // Check if the property has a setter
            if (property.Setter == null)
                return DavStatusCode.Conflict;

            // Validate the value (if not null)
            if (value != null && !property.Validator.Validate(value))
                return DavStatusCode.Conflict;

            // Set the value
            return property.Setter(principal, (TEntry)item, value);
        }
    }
}
