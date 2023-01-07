using NWebDav.Server.Handlers;
using System.Collections.Generic;

namespace NWebDav.Server
{
    /// <summary>
    /// Default implementation of the <see cref="IRequestHandlerFactory"/>
    /// interface to create WebDAV request handlers. 
    /// </summary>
    /// <seealso cref="IRequestHandler"/>
    /// <seealso cref="IRequestHandlerFactory"/>
    public sealed class RequestHandlerFactory : IRequestHandlerFactory
    {
        private static readonly IDictionary<string, IRequestHandler> s_requestHandlers = new Dictionary<string, IRequestHandler>
        {
            { "COPY", new CopyHandler() },
            { "DELETE", new DeleteHandler() },
            { "GET", new GetAndHeadHandler() },
            { "HEAD", new GetAndHeadHandler() },
            { "LOCK", new LockHandler() },
            { "MKCOL", new MkcolHandler() },
            { "MOVE", new MoveHandler() },
            { "OPTIONS", new OptionsHandler() },
            { "PROPFIND", new PropFindHandler() },
            { "PROPPATCH", new PropPatchHandler() },
            { "PUT", new PutHandler() },
            { "UNLOCK", new UnlockHandler() }
        };

        /// <inheritdoc/>
        public IRequestHandler? GetRequestHandler(string request)
        {
            // Obtain the dispatcher
            if (!s_requestHandlers.TryGetValue(request, out var requestHandler))
                return null;

            // Create an instance of the request handler
            return requestHandler;
        }

        /// <summary>
        /// Gets a list of supported HTTP methods.
        /// </summary>
        public static IEnumerable<string> AllowedMethods => s_requestHandlers.Keys;
    }
}
