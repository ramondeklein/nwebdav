using System.Web;

namespace NWebDav.Server.AspNet
{
    public class WebDavHandlerFactory : IHttpHandlerFactory
    {
        private readonly WebDavDispatcher _webDavDispatcher;

        public WebDavHandlerFactory(WebDavDispatcher webDavDispatcher)
        {
            _webDavDispatcher = webDavDispatcher;

        }

        public IHttpHandler GetHandler(HttpContext context, string requestType, string url, string pathTranslated)
        {
            return new WebDavHandler(_webDavDispatcher);
        }

        public void ReleaseHandler(IHttpHandler handler)
        {
        }
    }
}