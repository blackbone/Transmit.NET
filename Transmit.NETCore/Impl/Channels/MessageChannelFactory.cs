using System;

namespace Transmit
{
    internal static class MessageChannelFactory
    {
        internal static MessageChannel CreateChannel(QosType qosType, int channelId)
        {
            switch (qosType)
            {
                case QosType.None: return null;
                case QosType.Unreliable: return new UnreliableMessageChannel(channelId);
                case QosType.UnreliableOrdered: return new UnreliableOrderedMessageChannel(channelId);
                case QosType.Reliable: return new ReliableMessageChannel(channelId);
                case QosType.ReliableOrdered: return new ReliableOrderedMessageChannel(channelId);
                default: throw new NotSupportedException($"Qos Type {qosType.ToString()} is not supported.");
            }
        }

        internal static MessageChannel[] CreateChannels(params QosType[] qosTypes)
        {
            if (qosTypes == null || qosTypes.Length == 0)
            {
                throw new InvalidOperationException("Channels configuration is null or has no elements");
            }

            var len = qosTypes.Length;
            var result = new MessageChannel[len];
            for (int i = 0; i < len; ++i)
            {
                result[i] = CreateChannel(qosTypes[i], i);
            }

            return result;
        }
    }
}