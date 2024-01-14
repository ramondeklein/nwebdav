using System;
using System.IO;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NWebDav.Server.Helpers;
using NWebDav.Server.Locking;

namespace NWebDav.Server.Stores
{
    public class DiskStoreOptions
    {
        public required string BaseDirectory { get; set; }
        public bool IsWritable { get; set; } = true;
    }
    
    public sealed class DiskStore : IStore
    {
        private readonly IOptions<DiskStoreOptions> _options;
        private readonly ILoggerFactory _loggerFactory;

        public DiskStore(IOptions<DiskStoreOptions> options, ILockingManager lockingManager, ILoggerFactory loggerFactory)
        {
            _options = options;
            LockingManager = lockingManager;
            _loggerFactory = loggerFactory;
        }

        internal ILockingManager LockingManager { get; }
        internal bool IsWritable => _options.Value.IsWritable;
        private string BaseDirectory => _options.Value.BaseDirectory;

        public Task<IStoreItem?> GetItemAsync(Uri uri, CancellationToken cancellationToken)
        {
            // Determine the path from the uri
            var path = GetPathFromUri(uri);

            // Check if it's a directory
            if (Directory.Exists(path))
                return Task.FromResult<IStoreItem?>(CreateCollection(new DirectoryInfo(path)));

            // Check if it's a file
            if (File.Exists(path))
                return Task.FromResult<IStoreItem?>(CreateItem(new FileInfo(path)));

            // The item doesn't exist
            return Task.FromResult<IStoreItem?>(null);
        }

        public Task<IStoreCollection?> GetCollectionAsync(Uri uri, CancellationToken cancellationToken)
        {
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
            var requestedPath = UriHelper.GetDecodedPath(uri).Substring(1).Replace('/', Path.DirectorySeparatorChar);

            // Determine the full path
            var fullPath = Path.GetFullPath(Path.Combine(BaseDirectory, requestedPath));

            // Make sure we're still inside the specified directory
            if (fullPath != BaseDirectory && !fullPath.StartsWith(BaseDirectory + Path.DirectorySeparatorChar))
                throw new SecurityException($"Uri '{uri}' is outside the '{BaseDirectory}' directory.");

            // Return the combined path
            return fullPath;
        }

        internal DiskStoreCollection CreateCollection(DirectoryInfo directoryInfo) =>
            new(this, directoryInfo, _loggerFactory.CreateLogger<DiskStoreCollection>());

        internal DiskStoreItem CreateItem(FileInfo file) =>
            new(this, file, _loggerFactory.CreateLogger<DiskStoreItem>());
    }
}
