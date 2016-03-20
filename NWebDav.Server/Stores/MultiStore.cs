using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading.Tasks;

namespace NWebDav.Server.Stores
{
    public class MultiStore : IStoreResolver
    {
        private readonly IDictionary<string, IStoreResolver> _storeResolvers = new Dictionary<string, IStoreResolver>();

        public void AddStore(string prefix, IStoreResolver storeResolver)
        {
            // Convert the prefix to lower-case
            prefix = prefix.ToLowerInvariant();

            // Add the prefix to the store
            _storeResolvers.Add(prefix, storeResolver);
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

        private T Resolve<T>(Uri uri, Func<IStoreResolver, Uri, T> action)
        {
            // Determine the path
            var requestedPath = uri.AbsolutePath;
            var endOfPrefix = requestedPath.IndexOf('/');
            var prefix = (endOfPrefix >= 0 ? requestedPath.Substring(0, endOfPrefix) : requestedPath).ToLowerInvariant();
            var subUri = new Uri(uri, endOfPrefix >= 0 ? requestedPath.Substring(endOfPrefix+1) : string.Empty);

            // Try to find the store
            IStoreResolver storeResolver;
            if (!_storeResolvers.TryGetValue(prefix, out storeResolver))
                return default(T);

            // Resolve via the action
            return action(storeResolver, subUri);
        }
    }
}
