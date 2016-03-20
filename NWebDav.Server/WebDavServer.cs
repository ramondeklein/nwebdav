using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using log4net;
using NWebDav.Server.Handlers;
using NWebDav.Server.Helpers;

namespace NWebDav.Server
{
    public class WebDavServer : IDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static string ServerName;

        private readonly HttpListener _httpListener;
        private readonly IStoreResolver _storeResolver;
        private readonly IRequestHandlerFactory _requestHandlerFactory;
        private bool _isDisposed;

        static WebDavServer()
        {
            var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;
            ServerName = $"NWebDav/{assemblyVersion}";
        }

        public WebDavServer(IStoreResolver storeResolver, HttpListener httpListener, IRequestHandlerFactory requestHandlerFactory = null)
        {
            // Make sure a store resolver is specified
            if (storeResolver == null)
                throw new ArgumentNullException(nameof(storeResolver));

            // Make sure a HTTP listener is specified
            if (httpListener == null)
                throw new ArgumentNullException(nameof(httpListener));

            // Use the default request handler if no request handler is specified
            if (requestHandlerFactory == null)
            {
                // Make sure a requst handler factory is specified if authentication is used
                if (httpListener.AuthenticationSchemes != AuthenticationSchemes.Anonymous)
                    throw new ArgumentNullException(nameof(requestHandlerFactory), $"A '{nameof(requestHandlerFactory)}' must be specified if authentication is used.");

                // Use the default request handler
                requestHandlerFactory = new RequestHandlerFactory();
            }
            else if (httpListener.AuthenticationSchemes != AuthenticationSchemes.Anonymous)
            {
                // Make sure an authenticated request handler factory is used
                if (!(requestHandlerFactory is AuthenticatedRequestHandlerFactory))
                    throw new ArgumentException($"The '{nameof(requestHandlerFactory)}' should derive from '{nameof(AuthenticatedRequestHandlerFactory)}'", nameof(requestHandlerFactory));
            }

            // Save store resolver and request handler factory
            _storeResolver = storeResolver;
            _requestHandlerFactory = requestHandlerFactory;

            // Save the HTTP listener
            _httpListener = httpListener;
        }

        public WebDavServer(IStoreResolver storeResolver, AuthenticationSchemes authenticationSchemes = AuthenticationSchemes.Anonymous, IRequestHandlerFactory requestHandlerFactory = null)
            : this(storeResolver, CreateHttpListener(authenticationSchemes), requestHandlerFactory)
        {
        }

        private static HttpListener CreateHttpListener(AuthenticationSchemes authenticationSchemes)
        {
            return new HttpListener
            {
                AuthenticationSchemes = authenticationSchemes,
            };
        }

        public void Start(params string[] httpPrefixes)
        {
            // Forward call
            Start((IEnumerable<string>)httpPrefixes);
        }

        public void Start(IEnumerable<string> httpPrefixes)
        {
            // Make sure we're not disposed
            CheckDisposed();

            // Make sure at HTTP prefixes are specified
            if (httpPrefixes == null)
                throw new ArgumentNullException(nameof(httpPrefixes));

            // Add all HTTP prefixes
            _httpListener.Prefixes.Clear();
            foreach (var httpPrefix in httpPrefixes)
                _httpListener.Prefixes.Add(httpPrefix);

            // Start listening
            _httpListener.Start();

            // Listen for connections
            Listen();
        }

        public void Stop()
        {
            // Make sure we're not disposed
            CheckDisposed();

            // Shutdown the HTTP listener
            _httpListener.Close();
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                // Stop the listener
                Stop();
                _isDisposed = true;
            }
        }

        private void CheckDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException($"{nameof(WebDavServer)} is already disposed.");
        }

        private async void Listen()
        {
            HttpListenerContext httpListenerContext;
            while ((httpListenerContext = await _httpListener.GetContextAsync().ConfigureAwait(false)) != null)
            {
                DispatchRequest(httpListenerContext);
            }
        }

        private async void DispatchRequest(HttpListenerContext httpListenerContext)
        {
            // Determine the request log-string
            var request = httpListenerContext.Request;
            var response = httpListenerContext.Response;
            var logRequest = $"{request.HttpMethod}:{request.Url}:{request.RemoteEndPoint?.Address}";

            // Log the request
            if (Log.IsInfoEnabled)
                Log.Info($"{logRequest} - Start processing");

            // Set the Server header of the response
            response.Headers["Server"] = ServerName;

            // Start the stopwatch
            var sw = Stopwatch.StartNew();

            IRequestHandler requestHandler;
            try
            {
                // Obtain the request handler for this message
                requestHandler = _requestHandlerFactory.GetRequestHandler(httpListenerContext);

                // Make sure we got a request handler
                if (requestHandler == null)
                {
                    // Log warning
                    if (Log.IsWarnEnabled)
                        Log.Warn($"{logRequest} - Not supported.");

                    // Send BadRequest response
                    httpListenerContext.Response.SendResponse(DavStatusCode.BadRequest, "Unsupported request");
                    return;
                }
            }
            catch (Exception exc)
            {
                // Log error
                if (Log.IsErrorEnabled)
                    Log.Error($"Unexpected exception while trying to obtain the request handler (method={request.HttpMethod}, url={request.Url}, source={request.RemoteEndPoint}", exc);

                // Abort
                return;
            }

            try
            {
                // Handle the request
                if (await requestHandler.HandleRequestAsync(httpListenerContext, _storeResolver).ConfigureAwait(false))
                {
                    // Always make sure that the response is sent
                    httpListenerContext.Response.Close();

                    // Log processing duration
                    if (Log.IsInfoEnabled)
                        Log.Info($"{logRequest} - Finished processing ({sw.ElapsedMilliseconds}ms, HTTP result: {httpListenerContext.Response.StatusCode})");
                }
                else
                {
                    // Set status code to bad request
                    httpListenerContext.Response.SendResponse(DavStatusCode.BadRequest, "Request not processed");

                    // Log warning
                    if (Log.IsWarnEnabled)
                        Log.Warn($"{logRequest} - Not processed.");
                }
            }
            catch (Exception exc)
            {
                // Set status code to bad request
                httpListenerContext.Response.SendResponse(DavStatusCode.InternalServerError);

                // Log what's going wrong
                if (Log.IsErrorEnabled)
                    Log.Error($"Unexpected exception while handling request (method={request.HttpMethod}, url={request.Url}, source={request.RemoteEndPoint}", exc);
            }
            finally
            {
                // Check if we need to dispose the request handler
                (requestHandler as IDisposable)?.Dispose();
            }
        }
    }
}
