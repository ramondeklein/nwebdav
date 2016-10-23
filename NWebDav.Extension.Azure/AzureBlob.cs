using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.WindowsAzure.Storage.Blob;
using NWebDav.Server;

namespace NWebDav.Extension.Azure
{
    public class AzureBlob
    {
        private const string MetaIsCollection = "is-collection";
        private const string MetaCreationTimeUtc = "creation-time-utc";
        private const string MetaLastWriteTimeUtc = "last-modified-time-utc";
        private const string MetaLastAccessTimeUtc = "last-access-time-utc";
        private const string MetaWin32Attributes = "win32-attributes";

        public readonly CloudBlockBlob _cloudBlockBlob;
        private IList<AzureBlob> _items;

        public AzureBlob(CloudBlockBlob cloudBlockBlob)
        {
            _cloudBlockBlob = cloudBlockBlob;
        }

        public string Name => Path.GetFileName(_cloudBlockBlob.Name);
        public string FullName => _cloudBlockBlob.Name;
        public long ContentLength => _cloudBlockBlob.Properties.Length;
        public string ContentType => _cloudBlockBlob.Properties.ContentType;
        public string ETag => _cloudBlockBlob.Properties.ETag;

        public Task<Stream> GetReadStreamAsync() => _cloudBlockBlob.OpenReadAsync();
        public Task UploadFromStreamAsync(Stream inputStream) => _cloudBlockBlob.UploadFromStreamAsync(inputStream);

        public bool IsCollection => GetMetaBoolean(MetaIsCollection);
        public DateTime CreationTimeUtc => GetMetaDateTimeUtc(MetaCreationTimeUtc);
        public DateTime LastWriteTimeUtc => GetMetaDateTimeUtc(MetaLastWriteTimeUtc);
        public DateTime LastAccessTimeUtc => GetMetaDateTimeUtc(MetaLastAccessTimeUtc);
        public FileAttributes Attributes => (FileAttributes)GetMetaInteger(MetaWin32Attributes);

        public Task SetIsCollectionAsync(bool isCollection) => SetMetaBooleanAsync(MetaIsCollection, isCollection);
        public Task SetCreationTimeUtcAsync(DateTime dateTime) => SetMetaDateTimeAsync(MetaCreationTimeUtc, dateTime);
        public Task SetLastWriteTimeUtcAsync(DateTime dateTime) => SetMetaDateTimeAsync(MetaCreationTimeUtc, dateTime);
        public Task SetLastAccessTimeUtcAsync(DateTime dateTime) => SetMetaDateTimeAsync(MetaCreationTimeUtc, dateTime);
        public Task SetAttributesAsync(FileAttributes attributes) => SetMetaIntegerAsync(MetaCreationTimeUtc, (int)attributes);

        public Task<IEnumerable<AzureBlob>> GetChildsAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<AzureBlob>> GetItemsAsync()
        {
            var childs = await GetChildsAsync().ConfigureAwait(false);
            return childs.Where(c => !c.IsCollection);
        }

        public async Task<IEnumerable<AzureBlob>> GetCollectionsAsync()
        {
            var childs = await GetChildsAsync().ConfigureAwait(false);
            return childs.Where(c => c.IsCollection);
        }

        public Task<DavStatusCode> CopyAsync(AzureBlob destination)
        {
            throw new NotImplementedException();
        }

        private DateTime GetMetaDateTimeUtc(string metaKey)
        {
            string dateTimeText;
            if (!_cloudBlockBlob.Metadata.TryGetValue(metaKey, out dateTimeText))
                return default(DateTime);
            return XmlConvert.ToDateTime(dateTimeText, XmlDateTimeSerializationMode.Utc);
        }

        private Task SetMetaDateTimeAsync(string metaKey, DateTime value)
        {
            _cloudBlockBlob.Metadata[metaKey] = XmlConvert.ToString(value, XmlDateTimeSerializationMode.Utc);
            return _cloudBlockBlob.SetMetadataAsync();
        }

        private bool GetMetaBoolean(string metaKey)
        {
            string booleanText;
            if (!_cloudBlockBlob.Metadata.TryGetValue(metaKey, out booleanText))
                return default(bool);
            return XmlConvert.ToBoolean(booleanText);
        }

        private Task SetMetaBooleanAsync(string metaKey, bool value)
        {
            _cloudBlockBlob.Metadata[metaKey] = XmlConvert.ToString(value);
            return _cloudBlockBlob.SetMetadataAsync();
        }

        private int GetMetaInteger(string metaKey)
        {
            string intText;
            if (!_cloudBlockBlob.Metadata.TryGetValue(metaKey, out intText))
                return default(int);
            return XmlConvert.ToInt32(intText);
        }

        private Task SetMetaIntegerAsync(string metaKey, int value)
        {
            _cloudBlockBlob.Metadata[metaKey] = XmlConvert.ToString(value);
            return _cloudBlockBlob.SetMetadataAsync();
        }
    }
}