using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using NWebDav.Server.Helpers;

namespace NWebDav.Server.Stores
{
    public class DiskStore : IStoreResolver
    {
        private readonly string _directory;

        public DiskStore(string directory)
        {
            _directory = directory;
        }

        private class StoreItem : IStoreItem
        {
            private static readonly XName[] CheapFileProperties =
            {
                // RFC-2518 properties
                WebDavConstants.DavNs + "creationdate",
                WebDavConstants.DavNs + "displayname",
                WebDavConstants.DavNs + "getcontentlength",
                WebDavConstants.DavNs + "getcontenttype",
                //WebDavConstants.DavNs + "getetag",            // We don't have an efficient method to calculate the ETag
                WebDavConstants.DavNs + "getlastmodified",
                //WebDavConstants.DavNs + "lockdiscovery",      // Locks are not supported (yet)
                //WebDavConstants.DavNs + "resourcetype",
                //WebDavConstants.DavNs + "supportedlock",

                // Hopmann/Lippert collection properties
                // (useless for items, but some WebDAV clients do ask for it)
                WebDavConstants.DavNs + "childcount",
                WebDavConstants.DavNs + "iscollection",
                WebDavConstants.DavNs + "isfolder",
                WebDavConstants.DavNs + "ishidden",
                WebDavConstants.DavNs + "hassubs",
                WebDavConstants.DavNs + "nosubs",
                WebDavConstants.DavNs + "objectcount",
                WebDavConstants.DavNs + "visiblecount",

                // Win32 extensions
                WebDavConstants.Win32Ns + "Win32CreationTime",
                WebDavConstants.Win32Ns + "Win32LastAccessTime",
                WebDavConstants.Win32Ns + "Win32LastModifiedTime",
                WebDavConstants.Win32Ns + "Win32FileAttributes",
            };

            private static readonly XName[] ExpensiveFileProperties =
            {
                // RFC-2518 properties
                WebDavConstants.DavNs + "getetag"
            };

            private readonly FileInfo _fileInfo;

            public StoreItem(FileInfo fileInfo)
            {
                _fileInfo = fileInfo;
            }

            public string Name => _fileInfo.Name;
            public string ContentLanguage => null;
            public long? ContentLength => _fileInfo.Length;
            public string ContentType => "application/octet-stream";
            public DateTime? CreationUtc => _fileInfo.CreationTimeUtc;
            public DateTime? LastModifiedUtc => _fileInfo.LastWriteTimeUtc;
            public string Etag => CalcEtag(_fileInfo.FullName);
            public Stream GetReadableStream(IPrincipal principal) => _fileInfo.OpenRead();
            public Stream GetWritableStream(IPrincipal principal) => _fileInfo.OpenWrite();

            public async Task<DavStatusCode> CopyToAsync(IStoreCollection destination, string name, bool overwrite, IPrincipal principal)
            {
                try
                {
                    // If the destination is also a disk-store, then we can use the FileCopy API
                    // (it's probably a bit more efficient than copying in C#)
                    var diskCollection = destination as StoreCollection;
                    if (diskCollection != null)
                    {
                        var destinationPath = Path.Combine(diskCollection.FullPath, name);

                        // Check if the file already exists
                        var fileExists = File.Exists(destinationPath);
                        if (fileExists && !overwrite)
                            return DavStatusCode.PreconditionFailed;

                        // Copy the file
                        File.Copy(_fileInfo.FullName, destinationPath, true);

                        // Return the appropriate status
                        return fileExists ? DavStatusCode.NoContent : DavStatusCode.Created;
                    }
                    else
                    {
                        // Create the item in the destination collection
                        var result = await destination.CreateItemAsync(name, overwrite, principal);

                        // Check if the item could be created
                        if (result.Item != null)
                        {
                            using (var destinationStream = result.Item.GetWritableStream(principal))
                            using (var sourceStream = GetReadableStream(principal))
                            {
                                await sourceStream.CopyToAsync(destinationStream);
                            }
                        }

                        // Return result
                        return result.Result;
                    }
                }
                catch (IOException ioException) when (ioException.IsDiskFull())
                {
                    // TODO: Log exception
                    return DavStatusCode.InsufficientStorage;
                }
                catch (Exception exc)
                {
                    // TODO: Log exception
                    return DavStatusCode.InternalServerError;
                }
            }

            public IList<XName> CheapProperties => CheapFileProperties;
            public IList<XName> ExpensiveProperties => ExpensiveFileProperties;

            public string GetProperty(XName propertyName)
            {
                if (propertyName.Namespace == WebDavConstants.DavNs)
                {
                    switch (propertyName.LocalName)
                    {
                        case "creationdate": return CreationUtc.Value.ToString("s") + "Z";
                        case "displayname": return Name;
                        case "getcontentlength": return ContentLength.Value.ToString(CultureInfo.InvariantCulture);
                        case "getcontenttype": return ContentType;
                        case "getetag": return Etag;
                        case "getlastmodified": return LastModifiedUtc.Value.ToString("s") + "Z";
                        case "childcount": return "0";
                        case "iscollection":
                        case "isfolder": return "0";
                        case "ishidden": return ((_fileInfo .Attributes & FileAttributes.Hidden) != 0) ? "1" : "0";
                        case "hassubs": return "0";
                        case "nosubs": return "1";
                        case "objectcount": return "0";
                        case "visiblecount": return "0";
                    }
                }
                else if (propertyName.Namespace == WebDavConstants.Win32Ns)
                {
                    switch (propertyName.LocalName)
                    {
                        case "Win32CreationTime": return _fileInfo.CreationTimeUtc.ToString("s") + "Z";
                        case "Win32LastAccessTime": return _fileInfo.LastAccessTimeUtc.ToString("s") + "Z";
                        case "Win32LastModifiedTime": return _fileInfo.LastWriteTimeUtc.ToString("s") + "Z";
                        case "Win32FileAttributes": return ((int)_fileInfo.Attributes).ToString("X8");
                    }
                }

                // Unknown
                return null;
            }

            public bool SetProperty(XName propertyName, string value)
            {
                if (propertyName.Namespace == WebDavConstants.Win32Ns)
                {
                    switch (propertyName.LocalName)
                    {
                        case "Win32CreationTime":
                            _fileInfo.CreationTimeUtc = Convert.ToDateTime(value);
                            return true;
                        case "Win32LastAccessTime":
                            _fileInfo.LastAccessTimeUtc = Convert.ToDateTime(value);
                            return true;
                        case "Win32LastModifiedTime":
                            _fileInfo.LastWriteTimeUtc = Convert.ToDateTime(value);
                            return true;
                        case "Win32FileAttributes":
                            _fileInfo.Attributes = (FileAttributes)Convert.ToInt32(value, 16);
                            return true;
                    }
                }

                return false;
            }

            private string CalcEtag(string path)
            {
                using (var stream = File.OpenRead(path))
                {
                    var hash = new SHA256Managed().ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", string.Empty);
                }
            }
        }

        private class StoreCollection : IStoreCollection
        {
            private static readonly XName[] CheapCollectionProperties =
            {
                // RFC-2518 properties
                WebDavConstants.DavNs + "creationdate",
                WebDavConstants.DavNs + "displayname",
                //WebDavConstants.DavNs + "getcontentlength",
                //WebDavConstants.DavNs + "getcontenttype",
                //WebDavConstants.DavNs + "getetag",
                WebDavConstants.DavNs + "getlastmodified",
                //WebDavConstants.DavNs + "lockdiscovery",
                //WebDavConstants.DavNs + "resoucetype",
                //WebDavConstants.DavNs + "supportedlock",

                // Hopmann/Lippert collection properties
                WebDavConstants.DavNs + "childcount",
                WebDavConstants.DavNs + "iscollection",
                WebDavConstants.DavNs + "isfolder",
                WebDavConstants.DavNs + "ishidden",
                WebDavConstants.DavNs + "hassubs",
                WebDavConstants.DavNs + "nosubs",
                WebDavConstants.DavNs + "objectcount",
                WebDavConstants.DavNs + "visiblecount",

                // Win32 extensions
                WebDavConstants.Win32Ns + "Win32CreationTime",
                WebDavConstants.Win32Ns + "Win32LastAccessTime",
                WebDavConstants.Win32Ns + "Win32LastModifiedTime",
                WebDavConstants.Win32Ns + "Win32FileAttributes",
            };

            private static readonly XName[] ExpensiveCollectionProperties =
            {
                // No expensive properties known
            };

            private readonly DiskStore _diskStore;
            private readonly DirectoryInfo _directoryInfo;
            private IList<IStoreCollectionEntry> _entries;

            public StoreCollection(DiskStore diskStore, DirectoryInfo directoryInfo)
            {
                _diskStore = diskStore;
                _directoryInfo = directoryInfo;
            }

            public string Name => _directoryInfo.Name;
            public string ContentLanguage => null;
            public long? ContentLength => null;
            public string ContentType => null;
            public DateTime? CreationUtc => _directoryInfo.CreationTimeUtc;
            public DateTime? LastModifiedUtc => _directoryInfo.LastWriteTimeUtc;
            public DateTime? LastAccessUtc => _directoryInfo.LastAccessTimeUtc;
            public string Etag => null;
            public string FullPath => _directoryInfo.FullName;

            public Task<IList<IStoreCollectionEntry>> GetEntriesAsync(IPrincipal principal)
            {
                // Check if we have already fetched the items
                if (_entries == null)
                {
                    var entries = new List<IStoreCollectionEntry>();

                    // Add all directories
                    foreach (var subDirectory in _directoryInfo.GetDirectories())
                        entries.Add(new StoreCollection(_diskStore, subDirectory));

                    // Add all files
                    foreach (var file in _directoryInfo.GetFiles())
                        entries.Add(new StoreItem(file));

                    // Save the entries
                    _entries = new ReadOnlyCollection<IStoreCollectionEntry>(entries);
                }

                // Return the entries
                return Task.FromResult(_entries);
            }

            public Task<StoreItemResult> CreateItemAsync(string name, bool overwrite, IPrincipal principal)
            {
                // Determine the destination path
                var destinationPath = Path.Combine(FullPath, name);

                // Determine result
                DavStatusCode result;

                // Check if the file can be overwritten
                if (File.Exists(name))
                {
                    if (!overwrite)
                        return Task.FromResult(new StoreItemResult(DavStatusCode.PreconditionFailed));

                    result = DavStatusCode.NoContent;
                }
                else
                {
                    result = DavStatusCode.Created;
                }

                try
                {
                    // Create a new file
                    File.Create(destinationPath).Dispose();                    
                }
                catch (Exception exc)
                {
                    // TODO: Log exception
                    return Task.FromResult(new StoreItemResult(result));
                }

                // Return result
                return Task.FromResult(new StoreItemResult(result, new StoreItem(new FileInfo(destinationPath))));
            }

            public Task<StoreCollectionResult> CreateCollectionAsync(string name, bool overwrite, IPrincipal principal)
            {
                // Determine the destination path
                var destinationPath = Path.Combine(FullPath, name);

                // Check if the directory can be overwritten
                DavStatusCode result;
                if (Directory.Exists(destinationPath))
                {
                    // Check if overwrite is allowed
                    if (!overwrite)
                        return Task.FromResult(new StoreCollectionResult(DavStatusCode.PreconditionFailed));

                    // Overwrite existing
                    result = DavStatusCode.NoContent;
                }
                else
                {
                    // Created new directory
                    result = DavStatusCode.Created;
                }

                try
                {
                    // Attempt to create the directory
                    Directory.CreateDirectory(destinationPath);
                }
                catch (Exception exc)
                {
                    // TODO: Log exception
                    return null;
                }

                // Return the collection
                return Task.FromResult(new StoreCollectionResult(result, new StoreCollection(_diskStore, new DirectoryInfo(destinationPath))));
            }

            public Task<StoreCollectionResult> CopyToAsync(IStoreCollection destinationCollection, string name, bool overwrite, IPrincipal principal)
            {
                // Just create the folder itself
                return destinationCollection.CreateCollectionAsync(name, overwrite, principal);
            }

            public Task<DavStatusCode> DeleteAsync(IPrincipal principal)
            {
                // Delete the directory
                try
                {
                    Directory.Delete(_directoryInfo.FullName, false);
                    return Task.FromResult(DavStatusCode.OK);
                }
                catch (Exception exc)
                {
                    // TODO: Log exception
                    return Task.FromResult(DavStatusCode.InternalServerError);
                }
            }

            public Task<DavStatusCode> DeleteItemAsync(string name, IPrincipal principal)
            {
                try
                {
                    // Check if the file exists
                    if (!File.Exists(name))
                        return Task.FromResult(DavStatusCode.NotFound);

                    File.Delete(Path.Combine(_directoryInfo.FullName, name));
                    return Task.FromResult(DavStatusCode.OK);
                }
                catch (Exception exc)
                {
                    // TODO: Log exception
                    return Task.FromResult(DavStatusCode.InternalServerError);
                }
            }

            public bool AllowInfiniteDepthProperties => false;

            public IList<XName> CheapProperties => CheapCollectionProperties;
            public IList<XName> ExpensiveProperties => ExpensiveCollectionProperties;

            public string GetProperty(XName propertyName)
            {
                if (propertyName.Namespace == WebDavConstants.DavNs)
                {
                    switch (propertyName.LocalName)
                    {
                        case "creationdate": return CreationUtc.Value.ToString("s") + "Z";
                        case "displayname": return Name;
                        case "getlastmodified": return LastModifiedUtc.Value.ToString("s") + "Z";
                        case "childcount": return (_directoryInfo.EnumerateFiles().Count() + _directoryInfo.EnumerateDirectories().Count()).ToString(CultureInfo.InvariantCulture);
                        case "iscollection":
                        case "isfolder": return "1";
                        case "ishidden": return ((_directoryInfo.Attributes & FileAttributes.Hidden) != 0) ? "1" : "0";
                        case "hassubs": return _directoryInfo.EnumerateDirectories().Any() ? "1" : "0";
                        case "nosubs": return "0";
                        case "objectcount": return _directoryInfo.EnumerateFiles().Count().ToString(CultureInfo.InvariantCulture);
                        case "visiblecount": return _directoryInfo.EnumerateFiles().Count(fi => (fi.Attributes & FileAttributes.Hidden) == 0).ToString(CultureInfo.InvariantCulture);
                    }
                }
                else if (propertyName.Namespace == WebDavConstants.Win32Ns)
                {
                    switch (propertyName.LocalName)
                    {
                        case "Win32CreationTime": return _directoryInfo.CreationTimeUtc.ToString("s") + "Z";
                        case "Win32LastAccessTime": return _directoryInfo.LastAccessTimeUtc.ToString("s") + "Z";
                        case "Win32LastModifiedTime": return _directoryInfo.LastWriteTimeUtc.ToString("s") + "Z";
                        case "Win32FileAttributes": return ((int)_directoryInfo.Attributes).ToString("X8");
                    }
                }

                // Unknown
                return null;
            }

            public bool SetProperty(XName propertyName, string value)
            {
                if (propertyName.Namespace == WebDavConstants.Win32Ns)
                {
                    switch (propertyName.LocalName)
                    {
                        case "Win32CreationTime":
                            _directoryInfo.CreationTimeUtc = Convert.ToDateTime(value);
                            return true;
                        case "Win32LastAccessTime":
                            _directoryInfo.LastAccessTimeUtc = Convert.ToDateTime(value);
                            return true;
                        case "Win32LastModifiedTime":
                            _directoryInfo.LastWriteTimeUtc = Convert.ToDateTime(value);
                            return true;
                        case "Win32FileAttributes":
                            _directoryInfo.Attributes = (FileAttributes)Convert.ToInt32(value, 16);
                            return true;
                    }
                }

                return false;
            }
        }

        public Task<IStoreItem> GetItemAsync(Uri uri, IPrincipal principal)
        {
            // Determine the path from the uri
            var path = GetPathFromUri(uri);
            if (!File.Exists(path))
                return Task.FromResult<IStoreItem>(null);

            // Return the item
            return Task.FromResult<IStoreItem>(new StoreItem(new FileInfo(path)));
        }

        public Task<IStoreCollection> GetCollectionAsync(Uri uri, IPrincipal principal)
        {
            // Determine the path from the uri
            var path = GetPathFromUri(uri);
            if (!Directory.Exists(path))
                return Task.FromResult<IStoreCollection>(null);

            // Return the item
            return Task.FromResult<IStoreCollection>(new StoreCollection(this, new DirectoryInfo(path)));
        }

        private string GetPathFromUri(Uri uri)
        {
            // Determine the path
            var requestedPath = uri.AbsolutePath.Substring(1).Replace('/', '\\');

            // Determine the full path
            var fullPath = Path.GetFullPath(Path.Combine(_directory, requestedPath));

            // Make sure we're still inside the specified directory
            if (fullPath != _directory && !fullPath.StartsWith(_directory + "\\"))
                throw new SecurityException($"Uri '{uri}' is outside the '{_directory}' directory.");

            // Return the combined path
            return fullPath;
        }
    }
}
