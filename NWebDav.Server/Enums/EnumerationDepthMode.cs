namespace NWebDav.Server.Enums
{
    /// <summary>
    /// When the Depth is set to infinite, then this enumeration specifies
    /// how to deal with this.
    /// </summary>
    public enum EnumerationDepthMode
    {
        /// <summary>
        /// Infinite depth is allowed (this is according spec).
        /// </summary>
        Allowed,

        /// <summary>
        /// Infinite depth is not allowed (this results in HTTP 403 Forbidden).
        /// </summary>
        Rejected,

        /// <summary>
        /// Infinite depth is handled as Depth 0.
        /// </summary>
        Assume0,

        /// <summary>
        /// Infinite depth is handled as Depth 1.
        /// </summary>
        Assume1
    }
}
