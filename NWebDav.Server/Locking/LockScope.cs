using System;
using System.ComponentModel.DataAnnotations;

namespace NWebDav.Server.Locking
{
    public enum LockScope
    {
        [Display(Description = "exclusive")]
        Exclusive,

        [Display(Description = "shared")]
        Shared
    }
}
