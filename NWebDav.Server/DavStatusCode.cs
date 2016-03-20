using System;
using System.ComponentModel;
using System.Net;

namespace NWebDav.Server
{
    public enum DavStatusCode
    {
        [Description("Processing")]
        Processing = 102,

        [Description("OK")]
        OK = (int)HttpStatusCode.OK,

        [Description("Created")]
        Created = (int)HttpStatusCode.Created,

        [Description("No Content")]
        NoContent = (int)HttpStatusCode.NoContent,

        [Description("Multi-Status")]
        MultiStatus = 207,

        [Description("Bad Request")]
        BadRequest = (int)HttpStatusCode.BadRequest,

        [Description("Forbidden")]
        Forbidden = (int)HttpStatusCode.Forbidden,

        [Description("Conflict")]
        Conflict = (int)HttpStatusCode.Conflict,

        [Description("Not Found")]
        NotFound = (int)HttpStatusCode.NotFound,

        [Description("Precondition Failed")]
        PreconditionFailed = (int)HttpStatusCode.PreconditionFailed,

        [Description("Unprocessable Entity")]
        UnprocessableEntity = 422,

        [Description("Locked")]
        Locked = 423,

        [Description("Failed Dependency")]
        FailedDependency = 424,

        [Description("Internal Server Error")]
        InternalServerError = (int)HttpStatusCode.InternalServerError,

        [Description("Bad Gateway")]
        BadGateway = (int)HttpStatusCode.BadGateway,

        [Description("Insufficient Storage")]
        InsufficientStorage = 507,
    }
}
