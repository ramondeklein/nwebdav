using System;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace NWebDav.Server
{
    public enum DavStatusCode
    {
        [Display(Description = "Processing")]
        Processing = 102,

        [Display(Description = "OK")]
        OK = (int)HttpStatusCode.OK,

        [Display(Description = "Created")]
        Created = (int)HttpStatusCode.Created,

        [Display(Description = "No Content")]
        NoContent = (int)HttpStatusCode.NoContent,

        [Display(Description = "Multi-Status")]
        MultiStatus = 207,

        [Display(Description = "Bad Request")]
        BadRequest = (int)HttpStatusCode.BadRequest,

        [Display(Description = "Forbidden")]
        Forbidden = (int)HttpStatusCode.Forbidden,

        [Display(Description = "Conflict")]
        Conflict = (int)HttpStatusCode.Conflict,

        [Display(Description = "Not Found")]
        NotFound = (int)HttpStatusCode.NotFound,

        [Display(Description = "Precondition Failed")]
        PreconditionFailed = (int)HttpStatusCode.PreconditionFailed,

        [Display(Description = "Unprocessable Entity")]
        UnprocessableEntity = 422,

        [Display(Description = "Locked")]
        Locked = 423,

        [Display(Description = "Failed Dependency")]
        FailedDependency = 424,

        [Display(Description = "Internal Server Error")]
        InternalServerError = (int)HttpStatusCode.InternalServerError,

        [Display(Description = "Bad Gateway")]
        BadGateway = (int)HttpStatusCode.BadGateway,

        [Display(Description = "Insufficient Storage")]
        InsufficientStorage = 507,
    }
}
