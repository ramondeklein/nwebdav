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
        OK = (int)HttpStatusCode.OK,

        [DavStatusCode("Created")]
        Created = (int)HttpStatusCode.Created,

        [DavStatusCode("No Content")]
        NoContent = (int)HttpStatusCode.NoContent,

        [DavStatusCode("Partial Content")]
        PartialContent = (int)HttpStatusCode.PartialContent,

        [DavStatusCode("Multi-Status")]
        MultiStatus = 207,

        [DavStatusCode("Bad Request")]
        BadRequest = (int)HttpStatusCode.BadRequest,

        [DavStatusCode("Forbidden")]
        Forbidden = (int)HttpStatusCode.Forbidden,

        [DavStatusCode("Conflict")]
        Conflict = (int)HttpStatusCode.Conflict,

        [DavStatusCode("Not Found")]
        NotFound = (int)HttpStatusCode.NotFound,

        [DavStatusCode("Precondition Failed")]
        PreconditionFailed = (int)HttpStatusCode.PreconditionFailed,

        [DavStatusCode("Unprocessable Entity")]
        UnprocessableEntity = 422,

        [DavStatusCode("Locked")]
        Locked = 423,

        [DavStatusCode("Failed Dependency")]
        FailedDependency = 424,

        [DavStatusCode("Internal Server Error")]
        InternalServerError = (int)HttpStatusCode.InternalServerError,

        [DavStatusCode("Bad Gateway")]
        BadGateway = (int)HttpStatusCode.BadGateway,

        [DavStatusCode("Insufficient Storage")]
        InsufficientStorage = 507,
    }
}
