using System;

namespace NWebDav.Server.Stores
{
    public interface IDiskStoreItem : IStoreItem
    {
        string FullPath { get; }
    }
}
