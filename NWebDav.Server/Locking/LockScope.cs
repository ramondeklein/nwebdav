using System;
using System.ComponentModel;

namespace NWebDav.Server.Locking
{
    public enum LockScope
    {
        [Description("exclusive")]
        Exclusive,

        [Description("shared")]
        Shared
    }
}
