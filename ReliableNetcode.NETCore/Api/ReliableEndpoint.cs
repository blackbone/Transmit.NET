using System;
using ReliableNetcode.Utils;

namespace ReliableNetcode
{
    /// <summary>
    ///     Main class for routing messages through QoS channels
    /// </summary>
    public class ReliableEndpoint
    {
        private readonly ITransportChannel _transportChannel;
        private readonly ReliableOrderedMessageChannel _serviceChannel;
        private readonly MessageChannel[] _messageChannels;

        public uint Index = uint.MaxValue;

        /// <summary>
        ///     Approximate round-trip-time
        /// </summary>
        public float RTT => _serviceChannel.RTT;

        /// <summary>
        ///     Approximate packet loss
        /// </summary>
        public float PacketLoss => _serviceChannel.PacketLoss;

        /// <summary>
        ///     Approximate send bandwidth
        /// </summary>
        public float SentBandwidthKBPS => _serviceChannel.SentBandwidthKBPS;

        /// <summary>
        ///     Approximate received bandwidth
        /// </summary>
        public float ReceivedBandwidthKBPS => _serviceChannel.ReceivedBandwidthKBPS;

        // === TO THINK ABOUT IT

        private double _time;

        /// <summary>
        ///     Method which will be called to transmit raw datagrams over the network
        /// </summary>
        public Action<byte[], int> TransmitCallback;

        // Index, buffer, bufferLength
        public Action<uint, byte[], int> TransmitExtendedCallback;

        public ReliableEndpoint(ITransportChannel transportChannel, params QosType[] channelsConfig)
        {
            _transportChannel = transportChannel;
            var len = channelsConfig.Length + 1;
            var passConfig = new QosType[len];
            passConfig[0] = QosType.None;
            Array.Copy(channelsConfig, 0, passConfig, 1, channelsConfig.Length);

            _time = DateTime.Now.GetTotalSeconds();
            _messageChannels = MessageChannelFactory.CreateChannels(passConfig);
            _messageChannels[0] = _serviceChannel = new ReliableOrderedMessageChannel(0);

            for (int i = 0; i < len; ++i)
            {
                _messageChannels[i].Transport = _transportChannel;
            }
        }

        /// <summary>
        ///     Reset the endpoint
        /// </summary>
        public void Reset()
        {
            for (int i = 0, len = _messageChannels.Length; i < len; ++i)
            {
                _messageChannels[i].Reset();
            }
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
            _time += increment;
            Update(_time);
        }

        /// <summary>
        ///     Update the endpoint with a specific time value
        /// </summary>
        private void Update(double time)
        {
            this._time = time;

            for (int i = 0, len = _messageChannels.Length; i < len; ++i)
            {
                _messageChannels[i].Update(this._time);
            }
        }

        /// <summary>
        ///     Call this when a datagram has been received over the network
        /// </summary>
        public void ReceivePacket(byte[] buffer, int bufferLength)
        {
            int channel = buffer[1];
            _messageChannels[channel].ReceivePacket(buffer, bufferLength);
        }

        /// <summary>
        ///     Send a message with the given QoS level
        /// </summary>
        public void SendMessage(byte[] buffer, int bufferLength, int channelId)
        {
            _messageChannels[channelId].SendMessage(buffer, bufferLength);
        }
    }
}