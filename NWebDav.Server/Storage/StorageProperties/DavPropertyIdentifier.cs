﻿namespace NWebDav.Server.Storage.StorageProperties
{
    /// <summary>
    /// Represents an identifier for WebDav properties.
    /// </summary>
    /// <param name="Name">Gets the name of the property.</param>
    /// <param name="IsExpensive">Determines whether the property is expensive to retrieve.</param>
    public sealed record class DavPropertyIdentifier(string Name, bool IsExpensive);
}
