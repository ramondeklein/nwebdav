using System;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace NWebDav.Server.AspNet
{
    public class WebDavHandler : IHttpAsyncHandler
    {
        private readonly IWebDavDispatcher _webDavDispatcher;

        public WebDavHandler(IWebDavDispatcher webDavDispatcher)
        {
            _webDavDispatcher = webDavDispatcher;
        }

        public void ProcessRequest(HttpContext context)
        {
            using (var cts = GetCancellationTokenSource(context))
            {
                ExecuteAsync(context, cts.Token).Wait(cts.Token);
            }
        }

        public bool IsReusable => true;

        public IAsyncResult BeginProcessRequest(HttpContext context, AsyncCallback cb, object extraData)
        {
            // Dispatch the request. Note we don't use the ConfigureAwait(false), 
            // because we want to retain the HTTP context inside the controller.
            var cts = GetCancellationTokenSource(context);
            try
            {
                var task = ExecuteAsync(context, cts.Token);

                var tcs = new TaskCompletionSource<bool>(extraData);
                task.ContinueWith(t =>
                {
                    cts.Dispose();
                    if (t.IsFaulted)
                        tcs.TrySetException(t.Exception.InnerExceptions);
                    else if (t.IsCanceled)
                        tcs.TrySetCanceled();
                    else
                        tcs.TrySetResult(true);

                    cb?.Invoke(tcs.Task);
                }, TaskScheduler.Default);
                return tcs.Task;
            }
            catch (Exception)
            {
                cts.Dispose();
                throw;
            }
        }

        public void EndProcessRequest(IAsyncResult result)
        {
            var task = (Task) result;
            if (task.IsFaulted)
                throw task.Exception;
            if (task.IsCanceled)
                throw new OperationCanceledException();
        }

        private static CancellationTokenSource GetCancellationTokenSource(HttpContext context)
        {
            return CancellationTokenSource.CreateLinkedTokenSource(context.Response.ClientDisconnectedToken, context.Request.TimedOutToken);
        }

        private Task ExecuteAsync(HttpContext context, CancellationToken cancellationToken)
        {
            // Create the NWebDAV compatible context based on the ASP.NET context
            var aspNetContext = new AspNetContext(context);

            // Dispatch the request. Note we don't use the ConfigureAwait(false), 
            // because we want to retain the HTTP context inside the controller.
            return _webDavDispatcher.DispatchRequestAsync(aspNetContext, cancellationToken);
        }
    }
}