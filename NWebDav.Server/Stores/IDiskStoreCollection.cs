using System;

namespace NWebDav.Server.Stores
{
    public interface IDiskStoreCollection : IStoreCollection
    {
        string FullPath { get; }
    }
}
