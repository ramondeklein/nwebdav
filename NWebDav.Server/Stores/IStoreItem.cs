using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NWebDav.Server.Props;

namespace NWebDav.Server.Stores;

public interface IStoreItem
{
    // Item properties
    string Name { get; }
    string UniqueKey { get; }

    // Read/Write access to the data
    Task<Stream> GetReadableStreamAsync(CancellationToken cancellationToken);
    Task<DavStatusCode> UploadFromStreamAsync(Stream source, CancellationToken cancellationToken);

    // Copy support
    Task<StoreItemResult> CopyAsync(IStoreCollection destination, string name, bool overwrite, CancellationToken cancellationToken);

    // Property support
    IPropertyManager? PropertyManager { get; }
}