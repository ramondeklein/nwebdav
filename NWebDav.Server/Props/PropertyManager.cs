using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

using NWebDav.Server.Http;
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

        /// <summary>
        /// Obtain the list of all implemented properties.
        /// </summary>
        public IList<PropertyInfo> Properties { get; }

        public Task<object> GetPropertyAsync(IHttpContext httpContext, IStoreItem item, XName name, bool skipExpensive = false)
        {
            // Find the property
            DavProperty<TEntry> property;
            if (!_properties.TryGetValue(name, out property))
                return Task.FromResult((object)null);

            // Check if the property has a getter
            if (property.GetterAsync == null)
                return Task.FromResult((object)null);

            // Skip expensive properties
            if (skipExpensive && property.IsExpensive)
                return Task.FromResult((object)null);

            // Obtain the value
            return property.GetterAsync(httpContext, (TEntry)item);
        }

        public Task<DavStatusCode> SetPropertyAsync(IHttpContext httpContext, IStoreItem item, XName name, object value)
        {
            // Find the property
            DavProperty<TEntry> property;
            if (!_properties.TryGetValue(name, out property))
                return Task.FromResult(DavStatusCode.NotFound);

            // Check if the property has a setter
            if (property.SetterAsync == null)
                return Task.FromResult(DavStatusCode.Conflict);

            // Set the value
            return property.SetterAsync(httpContext, (TEntry)item, value);
        }
    }
}
