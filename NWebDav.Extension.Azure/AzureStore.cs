using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using NWebDav.Server.Http;
using NWebDav.Server.Locking;
using NWebDav.Server.Stores;

namespace NWebDav.Extension.Azure
{
    public class AzureStore : IStore
    {
        private readonly string _connectionString, _containerName;
        private readonly object _sync = new object();
        private CloudBlobContainer _container;

        public AzureStore(string connectionString, string container, bool isWritable = true, ILockingManager lockingManager = null)
        {
            if (connectionString == null)
                throw new ArgumentNullException(nameof(connectionString));

            _connectionString = connectionString;
            _containerName = container;
            IsWritable = isWritable;
            LockingManager = lockingManager ?? new InMemoryLockingManager();
        }

        public ILockingManager LockingManager { get; }
        public bool IsWritable { get; }

        private async Task InitializeAsync()
        {
            lock (_sync)
            {
                if (_container != null)
                    return;

                // Retrieve storage account from connection string.
                var storageAccount = CloudStorageAccount.Parse(_connectionString);
                var blobClient = storageAccount.CreateCloudBlobClient();

                // Obtain container
                _container = blobClient.GetContainerReference(_containerName);
            }

            // Make sure the container exists
            await _container.CreateIfNotExistsAsync().ConfigureAwait(false);
        }

        public async Task<IStoreItem> GetItemAsync(Uri uri, IHttpContext httpContext)
        {
            // Obtain the BLOB
            var azureBlob = await GetAzureBlob(uri).ConfigureAwait(false);
            if (azureBlob == null)
                return null;

            // Return the collection or item
            return azureBlob.IsCollection ? (IStoreItem) new AzureStoreCollection(LockingManager, azureBlob, IsWritable) : new AzureStoreItem(LockingManager, azureBlob, IsWritable);
        }

        public async Task<IStoreCollection> GetCollectionAsync(Uri uri, IHttpContext httpContext)
        {
            // Obtain the BLOB
            var azureBlob = await GetAzureBlob(uri).ConfigureAwait(false);
            if (azureBlob == null)
                return null;

            // Return the collection or item
            return azureBlob.IsCollection ? new AzureStoreCollection(LockingManager, azureBlob, IsWritable) : null;
        }

        private async Task<AzureBlob> GetAzureBlob(Uri uri)
        {
            // Make sure the store is initialized
            await InitializeAsync().ConfigureAwait(false);

            // Determine the local path
            var blobName = "root" + uri.LocalPath;

            // Obtain the BLOB
            var cloudBlob = _container.GetBlockBlobReference(blobName);
            if (await cloudBlob.ExistsAsync().ConfigureAwait(false))
                return new AzureBlob(cloudBlob);

            // Non-roots are not created automatically
            if (blobName != "root/")
                return null;

            // Auto-create the root container
            await cloudBlob.UploadFromByteArrayAsync(new byte[0], 0, 0).ConfigureAwait(false);

            // Set all root properties
            var utcNow = DateTime.UtcNow;
            var azureBlob = new AzureBlob(cloudBlob);
            await azureBlob.SetIsCollectionAsync(true).ConfigureAwait(false);
            await azureBlob.SetCreationTimeUtcAsync(utcNow).ConfigureAwait(false);
            await azureBlob.SetLastWriteTimeUtcAsync(utcNow).ConfigureAwait(false);
            await azureBlob.SetLastAccessTimeUtcAsync(utcNow).ConfigureAwait(false);
            await azureBlob.SetAttributesAsync(FileAttributes.Directory).ConfigureAwait(false);

            // Return the blob
            return azureBlob;
        }
    }
}
