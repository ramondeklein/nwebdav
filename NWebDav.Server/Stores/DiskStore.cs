using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NWebDav.Server.Stores;

public class DiskStoreOptions
{
    public required string BaseDirectory { get; set; }
    public bool IsWritable { get; set; } = true;
}

public sealed class DiskStore : DiskStoreBase
{
    private readonly IOptions<DiskStoreOptions> _options;

    public DiskStore(IOptions<DiskStoreOptions> options, DiskStoreCollectionPropertyManager diskStoreCollectionPropertyManager, DiskStoreItemPropertyManager diskStoreItemPropertyManager, ILoggerFactory loggerFactory)
        : base(diskStoreCollectionPropertyManager, diskStoreItemPropertyManager, loggerFactory)
    {
        _options = options;
    }

    public override bool IsWritable => _options.Value.IsWritable;
    public override string BaseDirectory => _options.Value.BaseDirectory;
}