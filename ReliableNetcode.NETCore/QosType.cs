namespace ReliableNetcode
{
    /// <summary>
    ///     Quality-of-service type for a message
    /// </summary>
    public enum QosType : byte
    {
        /// <summary>
        ///     Message is guaranteed to arrive and in order
        /// </summary>
        Reliable = 0,

        /// <summary>
        ///     Message is not guaranteed delivery nor order
        /// </summary>
        Unreliable = 1,

        /// <summary>
        ///     Message is not guaranteed delivery, but will be in order
        /// </summary>
        UnreliableOrdered = 2
    }
}