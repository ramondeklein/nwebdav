using NWebDav.Server.Enums;
using SecureFolderFS.Sdk.Storage.ExtendableStorage;
using SecureFolderFS.Sdk.Storage.LocatableStorage;
using SecureFolderFS.Sdk.Storage.ModifiableStorage;
using SecureFolderFS.Sdk.Storage.NestedStorage;

namespace NWebDav.Server.Storage
{
    /// <summary>
    /// Represents a WebDAV folder.
    /// </summary>
    public interface IDavFolder : IDavStorable, ILocatableFolder, IModifiableFolder, IFolderExtended, INestedFolder
    {
        /// <summary>
        /// Gets the depth mode for enumerating directory contents.
        /// </summary>
        EnumerationDepthMode DepthMode { get; }
    }
}
