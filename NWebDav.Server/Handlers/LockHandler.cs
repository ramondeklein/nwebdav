using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using NWebDav.Server.Helpers;
using NWebDav.Server.Locking;
using NWebDav.Server.Stores;

namespace NWebDav.Server.Handlers;

/// <summary>
/// Implementation of the LOCK method.
/// </summary>
/// <remarks>
/// The specification of the WebDAV LOCK method can be found in the
/// <see href="http://www.webdav.org/specs/rfc2518.html#METHOD_LOCK">
/// WebDAV specification
/// </see>.
/// </remarks>
public class LockHandler : IRequestHandler
{
    private readonly IXmlReaderWriter _xmlReaderWriter;
    private readonly IStore _store;
    private readonly ILockingManager _lockingManager;

    public LockHandler(IXmlReaderWriter xmlReaderWriter, IStore store, ILockingManager lockingManager)
    {
        _xmlReaderWriter = xmlReaderWriter;
        _store = store;
        _lockingManager = lockingManager;
    }
    
    /// <summary>
    /// Handle a LOCK request.
    /// </summary>
    /// <param name="httpContext">
    /// The HTTP context of the request.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous LOCK operation. The task
    /// will always return <see langword="true"/> upon completion.
    /// </returns>
    public async Task<bool> HandleRequestAsync(HttpContext httpContext)
    {
        // Obtain request and response
        var request = httpContext.Request;
        var response = httpContext.Response;
        
        // Determine the depth and requested timeout(s)
        var depth = request.GetDepth();
        var timeouts = request.GetTimeouts();

        // Obtain the WebDAV item
        var item = await _store.GetItemAsync(request.GetUri(), httpContext.RequestAborted).ConfigureAwait(false);
        if (item == null)
        {
            // Set status to not found
            response.SetStatus(DavStatusCode.PreconditionFailed);
            return true;
        }

        LockResult lockResult;

        // Check if an IF header is present (this would refresh the lock)
        var refreshLockToken = request.GetIfLockToken();
        if (refreshLockToken != null)
        {
            // Obtain the token
            lockResult = await _lockingManager.RefreshLockAsync(item, depth > 0, timeouts, refreshLockToken, httpContext.RequestAborted).ConfigureAwait(false);
        }
        else
        {
            // Determine lock-scope and owner
            LockScope lockScope;
            LockType lockType;
            XElement owner;

            // Read the property set/remove items from the request
            try
            {
                // Create an XML document from the stream
                var xDoc = await _xmlReaderWriter.LoadXmlDocumentAsync(request, httpContext.RequestAborted).ConfigureAwait(false);
                if (xDoc == null)
                    throw new Exception("Request-content couldn't be read");

                // Save the root document
                var xRoot = xDoc.Root;
                if (xRoot == null)
                    throw new Exception("No root element (expected 'lockinfo')");

                // The document should contain a 'lockinfo' element
                if (xRoot.Name != WebDavNamespaces.DavNs + "lockinfo")
                    throw new Exception("Invalid root element (expected 'lockinfo')");

                // Check all descendants
                var xLockScope = xRoot.Elements(WebDavNamespaces.DavNs + "lockscope").Single();
                var xLockScopeValue = xLockScope.Elements().Single();
                if (xLockScopeValue.Name == WebDavNamespaces.DavNs + "exclusive")
                    lockScope = LockScope.Exclusive;
                else if (xLockScopeValue.Name == WebDavNamespaces.DavNs + "shared")
                    lockScope = LockScope.Shared;
                else
                    throw new Exception("Invalid lockscope (expected 'exclusive' or 'shared')");

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
                response.SetStatus(DavStatusCode.BadRequest);
                return true;
            }

            // Perform the lock
            lockResult = await _lockingManager.LockAsync(item, lockType, lockScope, owner, request.GetUri(), depth > 0, timeouts, httpContext.RequestAborted).ConfigureAwait(false);
        }

        // Check if result is fine
        if (lockResult.Result != DavStatusCode.Ok)
        {
            // Set status to not found
            response.SetStatus(lockResult.Result);
            return true;
        }

        // We should have an active lock result at this point
        Debug.Assert(lockResult.Lock.HasValue, "Lock information should be supplied, when creating or refreshing a lock");

        // Return the information about the lock
        var xDocument = new XDocument(
            new XElement(WebDavNamespaces.DavNs + "prop",
                new XElement(WebDavNamespaces.DavNs + "lockdiscovery",
                    lockResult.Lock.Value.ToXml())));

        // Add the Lock-Token in the response
        // (only when creating a new lock)
        if (refreshLockToken == null)
            response.Headers["Lock-Token"] = $"<{lockResult.Lock.Value.LockToken.AbsoluteUri}>";

        // Stream the document
        await _xmlReaderWriter.SendResponseAsync(response, DavStatusCode.Ok, xDocument).ConfigureAwait(false);
        return true;
    }
}
