using System;
using System.IO;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NWebDav.Server.Helpers;

namespace NWebDav.Server.Stores;

public abstract class DiskStoreBase : IStore
{
    private readonly DiskStoreCollectionPropertyManager _diskStoreCollectionPropertyManager;
    private readonly DiskStoreItemPropertyManager _diskStoreItemPropertyManager;
    private readonly ILoggerFactory _loggerFactory;

    protected DiskStoreBase(DiskStoreCollectionPropertyManager diskStoreCollectionPropertyManager, DiskStoreItemPropertyManager diskStoreItemPropertyManager, ILoggerFactory loggerFactory)
    {
        _diskStoreCollectionPropertyManager = diskStoreCollectionPropertyManager;
        _diskStoreItemPropertyManager = diskStoreItemPropertyManager;
        _loggerFactory = loggerFactory;
    }

    public abstract bool IsWritable { get; }
    public abstract string BaseDirectory { get; }

    public Task<IStoreItem?> GetItemAsync(Uri uri, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
            
        var path = GetPathFromUri(uri);
        var item = CreateFromPath(path);
        return Task.FromResult(item);
    }

    public Task<IStoreCollection?> GetCollectionAsync(Uri uri, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
            
        // Determine the path from the uri
        var path = GetPathFromUri(uri);
        if (!Directory.Exists(path))
            return Task.FromResult<IStoreCollection?>(null);

        // Return the item
        return Task.FromResult<IStoreCollection?>(CreateCollection(new DirectoryInfo(path)));
    }

    private string GetPathFromUri(Uri uri)
    {
        // Determine the path
        var requestedPath = UriHelper.GetDecodedPath(uri)[1..].Replace('/', Path.DirectorySeparatorChar);

        // Determine the full path
        var fullPath = Path.GetFullPath(Path.Combine(BaseDirectory, requestedPath));

        // Make sure we're still inside the specified directory
        if (fullPath != BaseDirectory && !fullPath.StartsWith(BaseDirectory + Path.DirectorySeparatorChar))
            throw new SecurityException($"Uri '{uri}' is outside the '{BaseDirectory}' directory.");

        // Return the combined path
        return fullPath;
    }

    internal IStoreItem? CreateFromPath(string path)
    {
        // Check if it's a directory
        if (Directory.Exists(path))
            return CreateCollection(new DirectoryInfo(path));

        // Check if it's a file
        if (File.Exists(path))
            return CreateItem(new FileInfo(path));

        // The item doesn't exist
        return null;
    }

    internal DiskStoreCollection CreateCollection(DirectoryInfo directoryInfo) =>
        new(this, _diskStoreCollectionPropertyManager, directoryInfo, _loggerFactory.CreateLogger<DiskStoreCollection>());

    internal DiskStoreItem CreateItem(FileInfo file) =>
        new(this, _diskStoreItemPropertyManager, file, _loggerFactory.CreateLogger<DiskStoreItem>());
}