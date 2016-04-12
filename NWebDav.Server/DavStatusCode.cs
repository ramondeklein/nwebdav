using System;
using System.Net;

using NWebDav.Server.Helpers;

namespace NWebDav.Server
{
    public enum DavStatusCode
    {
        [DavStatusCode("Processing")]
        Processing = 102,

        [DavStatusCode("OK")]
        Ok = HttpStatusCode.OK,

        [DavStatusCode("Created")]
        Created = HttpStatusCode.Created,

        [DavStatusCode("No Content")]
        NoContent = HttpStatusCode.NoContent,

        [DavStatusCode("Partial Content")]
        PartialContent = HttpStatusCode.PartialContent,

        [DavStatusCode("Multi-Status")]
        MultiStatus = 207,

        [DavStatusCode("Bad Request")]
        BadRequest = HttpStatusCode.BadRequest,

        [DavStatusCode("Forbidden")]
        Forbidden = HttpStatusCode.Forbidden,

        [DavStatusCode("Not Found")]
        NotFound = HttpStatusCode.NotFound,
        [DavStatusCode("Conflict")]
        Conflict = HttpStatusCode.Conflict,


        [DavStatusCode("Precondition Failed")]
        PreconditionFailed = HttpStatusCode.PreconditionFailed,

        [DavStatusCode("Unprocessable Entity")]
        UnprocessableEntity = 422,

        [DavStatusCode("Locked")]
        Locked = 423,

        [DavStatusCode("Failed Dependency")]
        FailedDependency = 424,

        [DavStatusCode("Internal Server Error")]
        InternalServerError = HttpStatusCode.InternalServerError,

        [DavStatusCode("Bad Gateway")]
        BadGateway = HttpStatusCode.BadGateway,

        [DavStatusCode("Insufficient Storage")]
        InsufficientStorage = 507
    }
}
