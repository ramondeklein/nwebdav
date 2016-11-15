using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;

using NWebDav.Server.Http;
using NWebDav.Server.Stores;

namespace NWebDav.Server.Props
{
    public interface IPropertyManager
    {
        /// <summary>
        /// Obtain the list of all implemented properties.
        /// </summary>
        IList<PropertyInfo> Properties { get; }
        Task<object> GetPropertyAsync(IHttpContext httpContext, IStoreItem item, XName name, bool skipExpensive = false);
        Task<DavStatusCode> SetPropertyAsync(IHttpContext httpContext, IStoreItem item, XName name, object value);
    }
}
