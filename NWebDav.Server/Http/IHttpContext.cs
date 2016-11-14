using System.Threading.Tasks;

namespace NWebDav.Server.Http
{
    /// <summary>
    /// HTTP context interface.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The HTTP context specifies the context for the current WebDAV request.
    /// It's an abstraction of the underlying HTTP context implementation and
    /// it should contain the request, response and the session information.
    /// </para>
    /// <para>
    /// The HTTP context is passed to all handlers and should contain the all
    /// information. Although the internal NWebDAV code will serialize access
    /// to th IHttpContext (and its underlying request, response and session),
    /// the context should be accessible from an arbitrary thread and not rely
    /// internally on the synchronization context (i.e. a call to a static
    /// property that returns the thread's current HTTP context might result
    /// in a <c>null</c> or invalid HTTP context.
    /// </para>
    /// </remarks>
    public interface IHttpContext
    {
        /// <summary>
        /// Gets the current HTTP request message.
        /// </summary>
        /// <value>HTTP request.</value>
        IHttpRequest Request { get; }

        /// <summary>
        /// Gets the current HTTP response message.
        /// </summary>
        /// <value>HTTP response.</value>
        IHttpResponse Response { get; }

        /// <summary>
        /// Gets the session belonging to the current request.
        /// </summary>
        /// <value>Session.</value>
        IHttpSession Session { get; }

        /// <summary>
        /// Close the context.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Each request will have its own HTTP context and the
        /// <seealso cref="WebDavDispatcher"/> dispatching the request will
        /// make sure the context is closed at the end of the request. When
        /// this method completes the response should have been sent or it
        /// should be ready, so the underlying HTTP infrastructure can send
        /// it.
        /// </para>
        /// </remarks>
        Task CloseAsync();
    }
}