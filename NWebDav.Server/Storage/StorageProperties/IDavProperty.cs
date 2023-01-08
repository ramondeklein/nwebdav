using SecureFolderFS.Sdk.Storage.StorageProperties;

namespace NWebDav.Server.Storage.StorageProperties
{
    /// <inheritdoc cref="IStorageProperty{T}"/>
    public interface IDavProperty : IModifiableProperty<object>
    {
        /// <summary>
        /// Gets the name of the property.
        /// </summary>
        /// <remarks>
        /// The name of this storage property is determined by the underlying implementation.
        /// </remarks>
        string Name { get; }
    }
}
