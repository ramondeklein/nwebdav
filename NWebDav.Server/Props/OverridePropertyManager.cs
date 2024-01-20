using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using NWebDav.Server.Stores;

namespace NWebDav.Server.Props;

public class OverridePropertyManager<TEntry> : IPropertyManager
    where TEntry : IStoreItem
{
    private readonly Func<TEntry, IStoreItem> _converter;
    private readonly IDictionary<XName, DavProperty<TEntry>> _properties;
    private readonly IPropertyManager _basePropertyManager;

    public OverridePropertyManager(IEnumerable<DavProperty<TEntry>> properties, IPropertyManager basePropertyManager, Func<TEntry, IStoreItem>? converter = null)
    {
        // Convert the properties to a dictionary for fast retrieval
        _properties = properties.ToDictionary(p => p.Name) ?? throw new ArgumentNullException(nameof(properties));
        _basePropertyManager = basePropertyManager ?? throw new ArgumentNullException(nameof(basePropertyManager));
        _converter = converter ?? (si => si);

        // Create the property information immediately
        Properties = GetPropertyInfo();
    }

    public IList<PropertyInfo> Properties { get; }

    public Task<object?> GetPropertyAsync(IStoreItem item, XName propertyName, bool skipExpensive, CancellationToken cancellationToken)
    {
        // Find the property
        if (!_properties.TryGetValue(propertyName, out var property))
            return _basePropertyManager.GetPropertyAsync(_converter((TEntry)item), propertyName, skipExpensive, cancellationToken);

        // Check if the property has a getter
        if (property.GetterAsync == null)
            return _basePropertyManager.GetPropertyAsync(_converter((TEntry)item), propertyName, skipExpensive, cancellationToken);

        // Skip expensive properties
        if (skipExpensive && property.IsExpensive)
            return Task.FromResult((object?)null);

        // Obtain the value
        return property.GetterAsync((TEntry)item, cancellationToken);
    }

    public Task<DavStatusCode> SetPropertyAsync(IStoreItem item, XName propertyName, object value, CancellationToken cancellationToken)
    {
        // Find the property
        if (!_properties.TryGetValue(propertyName, out var property))
            return _basePropertyManager.SetPropertyAsync(_converter((TEntry)item), propertyName, value, cancellationToken);

        // Check if the property has a setter
        if (property.SetterAsync == null)
            return _basePropertyManager.SetPropertyAsync(_converter((TEntry)item), propertyName, value, cancellationToken);

        // Set the value
        return property.SetterAsync((TEntry)item, value, cancellationToken);
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