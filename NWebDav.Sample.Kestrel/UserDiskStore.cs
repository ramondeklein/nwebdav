using System.IO;
using System.Security.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NWebDav.Server.Stores;

namespace NWebDav.Sample.Kestrel;

public sealed class UserDiskStore : DiskStoreBase
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
            var username = _httpContextAccessor.HttpContext?.User.Identity?.Name;
            if (username == null) throw new AuthenticationException("not authenticated");
            var path = Path.Combine(Path.GetTempPath(), username);
            Directory.CreateDirectory(path);
            return path;
        }
    }
}