using SecureFolderFS.Sdk.Storage.ExtendableStorage;
using SecureFolderFS.Sdk.Storage.LocatableStorage;

namespace NWebDav.Server.Storage
{
    /// <summary>
    /// Represents a WebDAV file.
    /// </summary>
    public interface IDavFile : IDavStorable, ILocatableFile, IFileExtended // TODO: Maybe split addressability into a separate IDav.. interface?
    {
    }
}
