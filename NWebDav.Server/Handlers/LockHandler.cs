using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Linq;

using NWebDav.Server.Helpers;
using NWebDav.Server.Http;
using NWebDav.Server.Locking;
using NWebDav.Server.Stores;

namespace NWebDav.Server.Handlers
{
    [Verb("LOCK")]
    public class LockHandler : IRequestHandler
    {
        public async Task<bool> HandleRequestAsync(IHttpContext httpContext, IStore store)
        {
            // Obtain request and response
            var request = httpContext.Request;
            var response = httpContext.Response;
            var principal = httpContext.Session?.Principal;

            // Determine the depth and requested timeout(s)
            var depth = request.GetDepth();
            var timeouts = request.GetTimeouts();

            // TODO: Refreshing locks is not supported yet

            // Determine lockscope and owner
            LockScope lockScope;
            LockType lockType;
            XElement owner;

            // Read the property set/remove items from the request
            try
            {
                // Create an XML document from the stream
                var xDoc = XDocument.Load(request.Stream);

                // The document should contain a 'propertyupdate' element
                if (xDoc.Root?.Name != WebDavNamespaces.DavNs + "lockinfo")
                    throw new Exception("Invalid root element (expected 'lockinfo')");

                // Save the root document
                var xRoot = xDoc.Root;

                // Check all descendants
                var xLockScope = xRoot.Elements(WebDavNamespaces.DavNs + "lockscope").Single();
                var xLockScopeValue = xLockScope.Elements().Single();
                if (xLockScopeValue.Name == WebDavNamespaces.DavNs + "exclusive")
                    lockScope = LockScope.Exclusive;
                else if (xLockScopeValue.Name == WebDavNamespaces.DavNs + "shared")
                    lockScope = LockScope.Shared;
                else
                    throw new Exception("Invalid locksope (expected 'exclusive' or 'shared')");

                // Determine the lock-type
                var xLockType = xRoot.Elements(WebDavNamespaces.DavNs + "locktype").Single();
                var xLockTypeValue = xLockType.Elements().Single();
                if (xLockTypeValue.Name == WebDavNamespaces.DavNs + "write")
                    lockType = LockType.Write;
                else
                    throw new Exception("Invalid locktype (expected 'write')");

                // Determine the owner
                var xOwner = xRoot.Elements(WebDavNamespaces.DavNs + "owner").Single();
                owner = xOwner.Elements().Single();
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
                response.SendResponse(DavStatusCode.PreconditionFailed);
                return true;
            }

            // Check if we have a lock manager
            var lockingManager = item.LockingManager;
            if (lockingManager == null)
            {
                // Set status to not found
                response.SendResponse(DavStatusCode.PreconditionFailed);
                return true;
            }

            // Perform the lock
            var result = lockingManager.Lock(item, lockType, lockScope, owner, depth > 0, timeouts);
            if (result.Result != DavStatusCode.Ok)
            {
                // Set status to not found
                response.SendResponse(result.Result);
                return true;
            }

            // We should have an active lock result at this point
            Debug.Assert(result.LockInfo.HasValue, "Lock information should be supplied, when creating or refreshing a lock");

            // Return the information about the lock
            var xDocument = new XDocument(
                new XElement(WebDavNamespaces.DavNs + "prop",
                    new XElement(WebDavNamespaces.DavNs + "lockdiscovery",
                        result.LockInfo.Value.ToXml())));

            // Stream the document
            await response.SendResponseAsync(DavStatusCode.Ok, xDocument).ConfigureAwait(false);
            return true;
        }
    }
}
