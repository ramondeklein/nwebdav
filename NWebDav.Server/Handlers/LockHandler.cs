using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Xml.Linq;

using NWebDav.Server.Helpers;
using NWebDav.Server.Locking;
using NWebDav.Server.Stores;

namespace NWebDav.Server.Handlers
{
    [Verb("LOCK")]
    public class LockHandler : IRequestHandler
    {
        public async Task<bool> HandleRequestAsync(HttpListenerContext httpListenerContext, IStore store)
        {
            // Obtain request and response
            var request = httpListenerContext.Request;
            var response = httpListenerContext.Response;
            var principal = httpListenerContext.User;

            // Determine the depth and requested timeout(s)
            var depth = request.GetDepth();
            var timeouts = request.GetTimeouts();

            // Determine lockscope and owner
            LockScope lockScope;
            LockType lockType;
            XElement owner;

            // Read the property set/remove items from the request
            try
            {
                // Create an XML document from the stream
                var xDoc = XDocument.Load(request.InputStream);

                // The document should contain a 'propertyupdate' element
                if (xDoc.Root?.Name != WebDavNamespaces.DavNs + "D:lockinfo")
                    throw new Exception("Invalid root element (expected 'lockinfo')");

                // Save the root document
                var xRoot = xDoc.Root;

                // Check all descendants
                var xLockscope = xRoot.Descendants(WebDavNamespaces.DavNs + "lockscope").Single();
                if (xLockscope.Name == WebDavNamespaces.DavNs + "exclusive")
                    lockScope = LockScope.Exclusive;
                else if (xLockscope.Name == WebDavNamespaces.DavNs + "shared")
                    lockScope = LockScope.Shared;
                else
                    throw new Exception("Invalid locksope (expected 'exclusive' or 'shared')");

                // Determine the lock-type
                var xLockType = xRoot.Descendants(WebDavNamespaces.DavNs + "locktype").Single();
                if (xLockType.Name == WebDavNamespaces.DavNs + "write")
                    lockType = LockType.Write;
                else
                    throw new Exception("Invalid locktype (expected 'write')");

                // Determine the owner
                var xOwner = xRoot.Descendants(WebDavNamespaces.DavNs + "owner").Single();
                owner = xOwner.Descendants().Single();
            }
            catch (Exception)
            {
                response.SendResponse(DavStatusCode.BadRequest);
                return true;
            }

            // Obtain the WebDAV item
            var item = await store.GetItemAsync(request.Url, principal).ConfigureAwait(false);
            if (item == null)
            {
                // Set status to not found
                response.SendResponse(DavStatusCode.NotFound);
                return true;
            }

            // Lock all items
            await LockAsync(item, depth, lockScope, lockType, owner, timeouts, principal);

            // Set the response
            response.SendResponse(DavStatusCode.OK);
            return true;
        }

        private async Task LockAsync(IStoreItem item, int depth, LockScope lockScope, LockType lockType, XElement owner, IList<int> timeouts, IPrincipal principal)
        {
            // Lock recursively
            var collection = item as IStoreCollection;
            if (collection != null && depth > 0)
            {
                foreach (var subItem in await collection.GetItemsAsync(principal))
                    await LockAsync(subItem, depth - 1, lockScope, lockType, owner, timeouts, principal);
            }

            // Check if we have a lock manager
            var lockingManager = item.LockingManager;
            if (lockingManager != null)
            {
                var result = lockingManager.Lock(item, lockScope, lockType, owner, timeouts);
                // TODO: Do something
            }
            else
            {
                // TODO: Do something
            }

            // TODO: Add the result to the list
        }
    }
}
