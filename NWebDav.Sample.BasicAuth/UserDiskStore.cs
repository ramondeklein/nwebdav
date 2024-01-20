using System.IO;
using System.Security.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NWebDav.Server.Stores;

namespace NWebDav.Sample.Kestrel;

internal sealed class UserDiskStore : DiskStoreBase
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserDiskStore(IHttpContextAccessor httpContextAccessor, DiskStoreCollectionPropertyManager diskStoreCollectionPropertyManager, DiskStoreItemPropertyManager diskStoreItemPropertyManager, ILoggerFactory loggerFactory) : base(diskStoreCollectionPropertyManager, diskStoreItemPropertyManager, loggerFactory)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override bool IsWritable => true;

    public override string BaseDirectory
    {
        get
        {
            // Each user has a dedicated directory
            var username = User?.Identity?.Name;
            if (username == null) throw new AuthenticationException("not authenticated");
            var path = Path.Combine(Path.GetTempPath(), username);
            Directory.CreateDirectory(path);
            return path;
        }
    }

    // Even though the store is a singleton, the HttpContext will still hold
    // the current request's principal. IHttpContextAccessor uses AsyncLocal
    // internally that flows the async operation.
    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;
}