using System.Threading;
using System.Threading.Tasks;
using System.Web;
using NWebDav.Server.Http;

namespace NWebDav.Server.AspNet
{
    public partial class AspNetContext : IHttpContext
    {
        private readonly AspNetRequest _request;
        private readonly AspNetSession _session;
        private readonly AspNetResponse _response;
        private readonly CancellationToken _requestAborted;

        public AspNetContext(HttpContext httpContext)
        {
            // Assign properties
            _request = new AspNetRequest(httpContext.Request);
            _session = new AspNetSession(httpContext);
            _response = new AspNetResponse(httpContext.Response);
            _requestAborted = httpContext.Response.ClientDisconnectedToken;
        }

        public IHttpRequest Request => _request;
        public IHttpResponse Response => _response;
        public IHttpSession Session => _session;
        public CancellationToken RequestAborted => _requestAborted;

        public Task CloseAsync()
        {
            // NOP, so can be a synchronous call
            return Task.FromResult(true);
        }
    }
}