using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Xml.Linq;
using NWebDav.Server.Helpers;
using NWebDav.Server.Props;

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
            private static readonly PropertyManager<StoreItem> ItemPropertyManager = new PropertyManager<StoreItem>(new DavProperty<StoreItem>[]
            {
                // RFC-2518 properties
                new DavCreationDate<StoreItem>
                {
                    Getter = item => item._fileInfo.CreationTimeUtc,
                    Setter = (item, value) => { item._fileInfo.CreationTimeUtc = value; return true; }
                },
                new DavDisplayName<StoreItem>
                {
                    Getter = item => item._fileInfo.Name
                },
                new DavGetContentLength<StoreItem>
                {
                    Getter = item => item._fileInfo.Length
                },
                new DavGetContentType<StoreItem>
                {
                    Getter = item => item.DetermineContentType()
                },
                new DavGetEtag<StoreItem>
                {
                    // Calculating the Etag is an expensive operation,
                    // because we need to scan the entire file.
                    IsExpensive = true,             
                    Getter = item => item.CalculateEtag()
                },
                new DavGetLastModified<StoreItem>
                {
                    Getter = item => item._fileInfo.LastWriteTimeUtc,
                    Setter = (item, value) => { item._fileInfo.LastWriteTimeUtc = value; return true; }
                },
                new DavGetResourceType<StoreItem>
                {
                    Getter= item => null
                },

                // Hopmann/Lippert collection properties
                // (although not a collection, the IsHidden property might be valuable)
                new DavIsHidden<StoreItem>
                {
                    Getter = item => (item._fileInfo.Attributes & FileAttributes.Hidden) != 0
                },

                // Win32 extensions
                new Win32CreationTime<StoreItem>
                {
                    Getter = item => item._fileInfo.CreationTimeUtc,
                    Setter = (item, value) => { item._fileInfo.CreationTimeUtc = value; return true; }
                },
                new Win32LastAccessTime<StoreItem>
                {
                    Getter = item => item._fileInfo.LastAccessTimeUtc,
                    Setter = (item, value) => { item._fileInfo.LastAccessTimeUtc = value; return true; }
                },
                new Win32LastModifiedTime<StoreItem>
                {
                    Getter = item => item._fileInfo.LastWriteTimeUtc,
                    Setter = (item, value) => { item._fileInfo.LastWriteTimeUtc = value; return true; }
                },
                new Win32FileAttributes<StoreItem>
                {
                    Getter = item => item._fileInfo.Attributes,
                    Setter = (item, value) => { item._fileInfo.Attributes = value; return true; }
                }
            });

            private readonly FileInfo _fileInfo;

            public StoreItem(FileInfo fileInfo)
            {
                _fileInfo = fileInfo;
            }

            public string Name => _fileInfo.Name;
            public Stream GetReadableStream(IPrincipal principal) => _fileInfo.OpenRead();
            public Stream GetWritableStream(IPrincipal principal) => _fileInfo.OpenWrite();
            public IPropertyManager PropertyManager => ItemPropertyManager;

            public async Task<StoreItemResult> CopyToAsync(IStoreCollection destination, string name, bool overwrite, IPrincipal principal)
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
                            return new StoreItemResult(DavStatusCode.PreconditionFailed);

                        // Copy the file
                        File.Copy(_fileInfo.FullName, destinationPath, true);

                        // Return the appropriate status
                        return new StoreItemResult(fileExists ? DavStatusCode.NoContent : DavStatusCode.Created);
                    }
                    else
                    {
                        // Create the item in the destination collection
                        var result = await destination.CreateItemAsync(name, overwrite, principal).ConfigureAwait(false);

                        // Check if the item could be created
                        if (result.Item != null)
                        {
                            using (var destinationStream = result.Item.GetWritableStream(principal))
                            using (var sourceStream = GetReadableStream(principal))
                            {
                                await sourceStream.CopyToAsync(destinationStream).ConfigureAwait(false);
                            }
                        }

                        // Return result
                        return new StoreItemResult(result.Result, result.Item);
                    }
                }
                catch (IOException ioException) when (ioException.IsDiskFull())
                {
                    // TODO: Log exception
                    return new StoreItemResult(DavStatusCode.InsufficientStorage);
                }
                catch (Exception exc)
                {
                    // TODO: Log exception
                    return new StoreItemResult(DavStatusCode.InternalServerError);
                }
            }

            private string DetermineContentType()
            {
                // TODO: Determine content type based on extension
                return "application/octet-stream";
            }

            private string CalculateEtag()
            {
                using (var stream = File.OpenRead(_fileInfo.FullName))
                {
                    var hash = new SHA256Managed().ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", string.Empty);
                }
            }
        }

        private class StoreCollection : IStoreCollection
        {
            private static readonly PropertyManager<StoreCollection> CollectionPropertyManager = new PropertyManager<StoreCollection>(new DavProperty<StoreCollection>[]
            {
                // RFC-2518 properties
                new DavCreationDate<StoreCollection>
                {
                    Getter = collection => collection._directoryInfo.CreationTimeUtc,
                    Setter = (collection, value) => { collection._directoryInfo.CreationTimeUtc = value; return true; }
                },
                new DavDisplayName<StoreCollection>
                {
                    Getter = collection => collection._directoryInfo.Name
                },
                new DavGetLastModified<StoreCollection>
                {
                    Getter = collection => collection._directoryInfo.LastWriteTimeUtc,
                    Setter = (collection, value) => { collection._directoryInfo.LastWriteTimeUtc = value; return true; }
                },
                new DavGetResourceType<StoreCollection>
                {
                    Getter = collection => new XElement(WebDavNamespaces.DavNs + "collection")
                },

                // Hopmann/Lippert collection properties
                new DavChildCount<StoreCollection>
                {
                    Getter = collection => collection._directoryInfo.EnumerateFiles().Count() + collection._directoryInfo.EnumerateDirectories().Count()
                },
                new DavIsCollection<StoreCollection>
                {
                    Getter = collection => true
                },
                new DavIsFolder<StoreCollection>
                {
                    Getter = collection => true
                },
                new DavIsHidden<StoreCollection>
                {
                    Getter = collection => (collection._directoryInfo.Attributes & FileAttributes.Hidden) != 0
                },
                new DavHasSubs<StoreCollection>
                {
                    Getter = collection => collection._directoryInfo.EnumerateDirectories().Any()
                },
                new DavNoSubs<StoreCollection>
                {
                    Getter = collection => false
                },
                new DavObjectCount<StoreCollection>
                {
                    Getter = collection => collection._directoryInfo.EnumerateFiles().Count()
                },
                new DavVisibleCount<StoreCollection>
                {
                    Getter = collection => collection._directoryInfo.EnumerateFiles().Count(fi => (fi.Attributes & FileAttributes.Hidden) == 0)
                },
                
                // Win32 extensions
                new Win32CreationTime<StoreCollection>
                {
                    Getter = collection => collection._directoryInfo.CreationTimeUtc,
                    Setter = (collection, value) => { collection._directoryInfo.CreationTimeUtc = value; return true; }
                },
                new Win32LastAccessTime<StoreCollection>
                {
                    Getter = collection => collection._directoryInfo.LastAccessTimeUtc,
                    Setter = (collection, value) => { collection._directoryInfo.LastAccessTimeUtc = value; return true; }
                },
                new Win32LastModifiedTime<StoreCollection>
                {
                    Getter = collection => collection._directoryInfo.LastWriteTimeUtc,
                    Setter = (collection, value) => { collection._directoryInfo.LastWriteTimeUtc = value; return true; }
                },
                new Win32FileAttributes<StoreCollection>
                {
                    Getter = collection => collection._directoryInfo.Attributes,
                    Setter = (collection, value) => { collection._directoryInfo.Attributes = value; return true; }
                }
            });

            private readonly DirectoryInfo _directoryInfo;

            public StoreCollection(DirectoryInfo directoryInfo)
            {
                _directoryInfo = directoryInfo;
            }

            public string Name => _directoryInfo.Name;
            public string FullPath => _directoryInfo.FullName;

            // Disk collections (a.k.a. directories don't have their own data)
            public Stream GetReadableStream(IPrincipal principal) => null;
            public Stream GetWritableStream(IPrincipal principal) => null;

            public IPropertyManager PropertyManager => CollectionPropertyManager;

            public Task<IList<IStoreItem>> GetItemsAsync(IPrincipal principal)
            {
                var items = new List<IStoreItem>();

                // Add all directories
                foreach (var subDirectory in _directoryInfo.GetDirectories())
                    items.Add(new StoreCollection(subDirectory));

                // Add all files
                foreach (var file in _directoryInfo.GetFiles())
                    items.Add(new StoreItem(file));

                // Return the items
                return Task.FromResult<IList<IStoreItem>>(items);
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

            public Task<StoreItemResult> CreateCollectionAsync(string name, bool overwrite, IPrincipal principal)
            {
                // Determine the destination path
                var destinationPath = Path.Combine(FullPath, name);

                // Check if the directory can be overwritten
                DavStatusCode result;
                if (Directory.Exists(destinationPath))
                {
                    // Check if overwrite is allowed
                    if (!overwrite)
                        return Task.FromResult(new StoreItemResult(DavStatusCode.PreconditionFailed));

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
                return Task.FromResult(new StoreItemResult(result, new StoreCollection(new DirectoryInfo(destinationPath))));
            }

            public Task<StoreItemResult> CopyToAsync(IStoreCollection destinationCollection, string name, bool overwrite, IPrincipal principal)
            {
                // Just create the folder itself
                return destinationCollection.CreateCollectionAsync(name, overwrite, principal);
            }

            public Task<DavStatusCode> DeleteCollectionAsync(IPrincipal principal)
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
        }

        public Task<IStoreItem> GetItemAsync(Uri uri, IPrincipal principal)
        {
            // Determine the path from the uri
            var path = GetPathFromUri(uri);

            // Check if it's a directory
            if (Directory.Exists(path))
                return Task.FromResult<IStoreItem>(new StoreCollection(new DirectoryInfo(path)));

            // Check if it's a file
            if (File.Exists(path))
                return Task.FromResult<IStoreItem>(new StoreItem(new FileInfo(path)));

            // The item doesn't exist
            return Task.FromResult<IStoreItem>(null);
        }

        public Task<IStoreCollection> GetCollectionAsync(Uri uri, IPrincipal principal)
        {
            // Determine the path from the uri
            var path = GetPathFromUri(uri);
            if (!Directory.Exists(path))
                return Task.FromResult<IStoreCollection>(null);

            // Return the item
            return Task.FromResult<IStoreCollection>(new StoreCollection(new DirectoryInfo(path)));
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
