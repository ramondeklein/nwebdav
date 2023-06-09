using System;
using System.Threading.Tasks;
using NWebDav.Server.Helpers;
using NWebDav.Server.Http;

namespace NWebDav.Server.Stores
{
    /// <summary>
    /// Wraps an <see cref="IStore"/>. Can be used to specify the root directory of the server.
    /// </summary>
    public sealed class RootDiskStore : IStore
    {
        private readonly string _remoteRootDirectory;
        private readonly IStore _root;

        public RootDiskStore(string remoteRootDirectory, IStore root)
        {
            _remoteRootDirectory = remoteRootDirectory;
            _root = root;
        }

        public Task<IStoreItem> GetItemAsync(Uri uri, IHttpContext context)
        {
            if (!uri.LocalPath.StartsWith($"/{_remoteRootDirectory}"))
                return Task.FromResult<IStoreItem>(null);
            
            return _root.GetItemAsync(UriHelper.RemoveRootDirectory(uri, _remoteRootDirectory), context);
        }

        public Task<IStoreCollection> GetCollectionAsync(Uri uri, IHttpContext context)
        {
            if (!uri.LocalPath.StartsWith($"/{_remoteRootDirectory}"))
                return Task.FromResult<IStoreCollection>(null);
            
            return _root.GetCollectionAsync(UriHelper.RemoveRootDirectory(uri, _remoteRootDirectory), context);
        }
    }
}