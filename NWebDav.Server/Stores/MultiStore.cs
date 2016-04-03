using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading.Tasks;

namespace NWebDav.Server.Stores
{
    public class MultiStore : IStore
    {
        private readonly IDictionary<string, IStore> _storeResolvers = new Dictionary<string, IStore>();

        public void AddStore(string prefix, IStore store)
        {
            // Convert the prefix to lower-case
            prefix = prefix.ToLowerInvariant();

            // Add the prefix to the store
            _storeResolvers.Add(prefix, store);
        }

        public void RemoveStore(string prefix)
        {
            // Convert the prefix to lower-case
            prefix = prefix.ToLowerInvariant();

            // Add the prefix to the store
            _storeResolvers.Remove(prefix);
        }

        public Task<IStoreItem> GetItemAsync(Uri uri, IPrincipal principal)
        {
            return Resolve(uri, (storeResolver, subUri) => storeResolver.GetItemAsync(subUri, principal));
        }

        public Task<IStoreCollection> GetCollectionAsync(Uri uri, IPrincipal principal)
        {
            return Resolve(uri, (storeResolver, subUri) => storeResolver.GetCollectionAsync(subUri, principal));
        }

        private T Resolve<T>(Uri uri, Func<IStore, Uri, T> action)
        {
            // Determine the path
            var requestedPath = uri.LocalPath;
            var endOfPrefix = requestedPath.IndexOf('/');
            var prefix = (endOfPrefix >= 0 ? requestedPath.Substring(0, endOfPrefix) : requestedPath).ToLowerInvariant();
            var subUri = new Uri(uri, endOfPrefix >= 0 ? requestedPath.Substring(endOfPrefix + 1) : string.Empty);

            // Try to find the store
            IStore store;
            if (!_storeResolvers.TryGetValue(prefix, out store))
                return default(T);

            // Resolve via the action
            return action(store, subUri);
        }
    }
}
