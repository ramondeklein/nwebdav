using System;
using System.ComponentModel.DataAnnotations;

namespace NWebDav.Server.Locking
{
    public enum LockType
    {
        [Display(Description = "write")]
        Write
    }
}
