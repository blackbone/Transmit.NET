namespace Transmit
{
    /// <summary>
    ///     Quality-of-service type for a message
    /// </summary>
    public enum QosType : byte
    {
        /// <summary>
        ///     Message is not created or not recognized
        /// </summary>
        None = 0,

        /// <summary>
        ///     Message is not guaranteed delivery nor order
        /// </summary>
        Unreliable = 1,

        /// <summary>
        ///     Message is guaranteed to arrive but order is not guaranteed
        /// </summary>
        Reliable = 2,

        /// <summary>
        ///     Message is not guaranteed delivery, but will be in order
        /// </summary>
        UnreliableOrdered = 3,

        /// <summary>
        ///     Message is guaranteed to arrive adn in order
        /// </summary>
        ReliableOrdered = 4
    }
}