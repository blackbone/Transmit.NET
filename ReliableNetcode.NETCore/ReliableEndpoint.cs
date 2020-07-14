using System;
using ReliableNetcode.Utils;

namespace ReliableNetcode
{
    /// <summary>
    ///     Main class for routing messages through QoS channels
    /// </summary>
    public class ReliableEndpoint
    {
        // the reliable channel
        private readonly ReliableMessageChannel _reliableChannel;

        private readonly MessageChannel[] messageChannels;
        public uint Index = uint.MaxValue;

        /// <summary>
        ///     Method which will be called when messages are received
        /// </summary>
        public Action<byte[], int> ReceiveCallback;

        public Action<uint, byte[], int> ReceiveExtendedCallback;
        private double time;

        /// <summary>
        ///     Method which will be called to transmit raw datagrams over the network
        /// </summary>
        public Action<byte[], int> TransmitCallback;

        // Index, buffer, bufferLength
        public Action<uint, byte[], int> TransmitExtendedCallback;

        public ReliableEndpoint(params QosType[] channelsConfig)
        {
            time = DateTime.Now.GetTotalSeconds();

            messageChannels = new MessageChannel[channelsConfig.Length];
            for (int i = 0, len = channelsConfig.Length; i < len; ++i)
            {
                messageChannels[i] = CreateChannel(channelsConfig[i]);
            }

            _reliableChannel = new ReliableMessageChannel
                {TransmitCallback = transmitMessage, ReceiveCallback = receiveMessage};

            messageChannels = new MessageChannel[]
            {
                _reliableChannel,
                new UnreliableMessageChannel {TransmitCallback = transmitMessage, ReceiveCallback = receiveMessage},
                new UnreliableOrderedMessageChannel {TransmitCallback = transmitMessage, ReceiveCallback = receiveMessage}
            };
        }

        public ReliableEndpoint(uint index) : this()
        {
            Index = index;
        }

        /// <summary>
        ///     Approximate round-trip-time
        /// </summary>
        public float RTT => _reliableChannel.RTT;

        /// <summary>
        ///     Approximate packet loss
        /// </summary>
        public float PacketLoss => _reliableChannel.PacketLoss;

        /// <summary>
        ///     Approximate send bandwidth
        /// </summary>
        public float SentBandwidthKBPS => _reliableChannel.SentBandwidthKBPS;

        /// <summary>
        ///     Approximate received bandwidth
        /// </summary>
        public float ReceivedBandwidthKBPS => _reliableChannel.ReceivedBandwidthKBPS;

        /// <summary>
        ///     Reset the endpoint
        /// </summary>
        public void Reset()
        {
            for (var i = 0; i < messageChannels.Length; i++)
                messageChannels[i].Reset();
        }

        /// <summary>
        ///     Update the endpoint with the current time
        /// </summary>
        public void Update()
        {
            Update(DateTime.Now.GetTotalSeconds());
        }

        /// <summary>
        ///     Manually step the endpoint forward by increment in seconds
        /// </summary>
        public void UpdateFastForward(double increment)
        {
            time += increment;
            Update(time);
        }

        /// <summary>
        ///     Update the endpoint with a specific time value
        /// </summary>
        public void Update(double time)
        {
            this.time = time;

            for (var i = 0; i < messageChannels.Length; i++)
                messageChannels[i].Update(this.time);
        }

        /// <summary>
        ///     Call this when a datagram has been received over the network
        /// </summary>
        public void ReceivePacket(byte[] buffer, int bufferLength)
        {
            int channel = buffer[1];
            messageChannels[channel].ReceivePacket(buffer, bufferLength);
        }

        /// <summary>
        ///     Send a message with the given QoS level
        /// </summary>
        public void SendMessage(byte[] buffer, int bufferLength, QosType qos)
        {
            messageChannels[(int) qos].SendMessage(buffer, bufferLength);
        }

        protected void receiveMessage(byte[] buffer, int length)
        {
            if (ReceiveCallback != null)
                ReceiveCallback(buffer, length);

            if (ReceiveExtendedCallback != null)
                ReceiveExtendedCallback(Index, buffer, length);
        }

        protected void transmitMessage(byte[] buffer, int length)
        {
            if (TransmitCallback != null)
                TransmitCallback(buffer, length);

            if (TransmitExtendedCallback != null)
                TransmitExtendedCallback(Index, buffer, length);
        }
    }
}