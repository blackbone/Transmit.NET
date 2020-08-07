using System;
using System.Collections.Generic;
using ReliableNetcode.Utils;

namespace ReliableNetcode
{
    internal sealed class ReliableMessageChannel : MessageChannel
    {
        private readonly SequenceBuffer<OutgoingPacketSet> ackBuffer;

        private readonly ReliableConfig config;

        private readonly ByteBuffer messagePacker = new ByteBuffer();

        private readonly Queue<ByteBuffer> messageQueue = new Queue<ByteBuffer>();
        private readonly ReliablePacketController packetController;
        private readonly SequenceBuffer<BufferedPacket> receiveBuffer;
        private readonly SequenceBuffer<BufferedPacket> sendBuffer;
        private bool congestionControl;
        private double congestionDisableInterval;
        private double congestionDisableTimer;

        private double lastBufferFlush;
        private double lastCongestionSwitchTime;
        private double lastMessageSend;
        private ushort nextReceive;

        private ushort oldestUnacked;
        private ushort sequence;

        protected List<ushort> tempList = new List<ushort>();
        private double time;

        public ReliableMessageChannel(int channelId) : base(channelId)
        {
            config = ReliableConfig.DefaultConfig();
            config.TransmitPacketCallback = (buffer, size) => { TransmitCallback(buffer, size); };
            config.ProcessPacketCallback = processPacket;
            config.AckPacketCallback = ackPacket;

            sendBuffer = new SequenceBuffer<BufferedPacket>(256);
            receiveBuffer = new SequenceBuffer<BufferedPacket>(256);
            ackBuffer = new SequenceBuffer<OutgoingPacketSet>(256);

            time = DateTime.Now.GetTotalSeconds();
            lastBufferFlush = -1.0;
            lastMessageSend = 0.0;
            packetController = new ReliablePacketController(config, time);

            congestionDisableInterval = 5.0;

            sequence = 0;
            nextReceive = 0;
            oldestUnacked = 0;
        }

        public override void Reset()
        {
            packetController.Reset();
            sendBuffer.Reset();
            ackBuffer.Reset();

            lastBufferFlush = -1.0;
            lastMessageSend = 0.0;

            congestionControl = false;
            lastCongestionSwitchTime = 0.0;
            congestionDisableTimer = 0.0;
            congestionDisableInterval = 5.0;

            sequence = 0;
            nextReceive = 0;
            oldestUnacked = 0;
        }

        public override void Update(double newTime)
        {
            var dt = newTime - time;
            time = newTime;
            packetController.Update(time);

            // see if we can pop messages off of the message queue and put them on the send queue
            if (messageQueue.Count > 0)
            {
                var sendBufferSize = 0;
                for (var seq = oldestUnacked; PacketIo.SequenceLessThan(seq, sequence); seq++)
                    if (sendBuffer.Exists(seq))
                        sendBufferSize++;

                if (sendBufferSize < sendBuffer.Size)
                {
                    var message = messageQueue.Dequeue();
                    var messageInternalBuffer = message.InternalBuffer;
                    SendMessage(ref messageInternalBuffer, message.Length);
                    ObjPool<ByteBuffer>.Return(message);
                }
            }

            // update congestion mode
            {
                // conditions are bad if round-trip-time exceeds 250ms
                var conditionsBad = packetController.RTT >= 250f;

                // if conditions are bad, immediately enable congestion control and reset the congestion timer
                if (conditionsBad)
                {
                    if (congestionControl == false)
                    {
                        // if we're within 10 seconds of the last time we switched, double the threshold interval
                        if (time - lastCongestionSwitchTime < 10.0)
                            congestionDisableInterval = Math.Min(congestionDisableInterval * 2, 60.0);

                        lastCongestionSwitchTime = time;
                    }

                    congestionControl = true;
                    congestionDisableTimer = 0.0;
                }

                // if we're in bad mode, and conditions are good, update the timer and see if we can disable congestion control
                if (congestionControl && !conditionsBad)
                {
                    congestionDisableTimer += dt;
                    if (congestionDisableTimer >= congestionDisableInterval)
                    {
                        congestionControl = false;
                        lastCongestionSwitchTime = time;
                        congestionDisableTimer = 0.0;
                    }
                }

                // as long as conditions are good, halve the threshold interval every 10 seconds
                if (congestionControl == false)
                {
                    congestionDisableTimer += dt;
                    if (congestionDisableTimer >= 10.0)
                        congestionDisableInterval = Math.Max(congestionDisableInterval * 0.5, 5.0);
                }
            }

            // if we're in congestion control mode, only send packets 10 times per second.
            // otherwise, send 30 times per second
            var flushInterval = congestionControl ? 0.1 : 0.033;

            if (time - lastBufferFlush >= flushInterval)
            {
                lastBufferFlush = time;
                processSendBuffer();
            }
        }

        public override void ReceivePacket(ref byte[] buffer, int bufferLength)
        {
            packetController.ReceivePacket(buffer, bufferLength);
        }

        public override void SendMessage(ref byte[] buffer, int bufferLength)
        {
            var sendBufferSize = 0;
            for (var seq = oldestUnacked; PacketIo.SequenceLessThan(seq, this.sequence); seq++)
                if (sendBuffer.Exists(seq))
                    sendBufferSize++;

            if (sendBufferSize == sendBuffer.Size)
            {
                var tempBuff = ObjPool<ByteBuffer>.Get();
                tempBuff.SetSize(bufferLength);
                tempBuff.BufferCopy(buffer, 0, 0, bufferLength);
                messageQueue.Enqueue(tempBuff);

                return;
            }

            var sequence = this.sequence++;
            var packet = sendBuffer.Insert(sequence);

            packet.time = -1.0;

            // ensure size for header
            var varLength = getVariableLengthBytes((ushort) bufferLength);
            packet.buffer.SetSize(bufferLength + 2 + varLength);

            using (var writer = ByteArrayReaderWriter.Get(packet.buffer.InternalBuffer))
            {
                writer.Write(sequence);

                writeVariableLengthUShort((ushort) bufferLength, writer);
                writer.WriteBuffer(buffer, bufferLength);
            }

            // signal that packet is ready to be sent
            packet.writeLock = false;
        }

        private void sendAckPacket()
        {
            packetController.SendAck((byte) ChannelID);
        }

        private int getVariableLengthBytes(ushort val)
        {
            if (val > 0x7fff) throw new ArgumentOutOfRangeException();

            var b2 = (byte) (val >> 7);
            return b2 != 0 ? 2 : 1;
        }

        private void writeVariableLengthUShort(ushort val, ByteArrayReaderWriter writer)
        {
            if (val > 0x7fff) throw new ArgumentOutOfRangeException();

            var b1 = (byte) (val & 0x007F); // write the lowest 7 bits
            var b2 = (byte) (val >> 7); // write remaining 8 bits

            // if there's a second byte to write, set the continue flag
            if (b2 != 0) b1 |= 0x80;

            // write bytes
            writer.Write(b1);
            if (b2 != 0)
                writer.Write(b2);
        }

        private ushort readVariableLengthUShort(ByteArrayReaderWriter reader)
        {
            ushort val = 0;

            var b1 = reader.ReadByte();
            val |= (ushort) (b1 & 0x7F);

            if ((b1 & 0x80) != 0) val |= (ushort) (reader.ReadByte() << 7);

            return val;
        }

        protected void processSendBuffer()
        {
            var numUnacked = 0;
            for (var seq = oldestUnacked; PacketIo.SequenceLessThan(seq, sequence); seq++)
                numUnacked++;

            for (var seq = oldestUnacked; PacketIo.SequenceLessThan(seq, sequence); seq++)
            {
                // never send message ID >= ( oldestUnacked + bufferSize )
                if (seq >= oldestUnacked + 256)
                    break;

                // for any message that hasn't been sent in the last 0.1 seconds and fits in the available space of our message packer, add it
                var packet = sendBuffer.Find(seq);
                if (packet != null && !packet.writeLock)
                {
                    if (time - packet.time < 0.1)
                        continue;

                    var packetFits = false;

                    if (packet.buffer.Length < config.FragmentThreshold)
                        packetFits = messagePacker.Length + packet.buffer.Length <=
                                     config.FragmentThreshold - Defines.MAX_PACKET_HEADER_BYTES;
                    else
                        packetFits = messagePacker.Length + packet.buffer.Length <= config.MaxPacketSize -
                            Defines.FRAGMENT_HEADER_BYTES - Defines.MAX_PACKET_HEADER_BYTES;

                    // if the packet won't fit, flush the message packer
                    if (!packetFits) flushMessagePacker();

                    packet.time = time;

                    var ptr = messagePacker.Length;
                    messagePacker.SetSize(messagePacker.Length + packet.buffer.Length);
                    messagePacker.BufferCopy(packet.buffer, 0, ptr, packet.buffer.Length);

                    tempList.Add(seq);

                    lastMessageSend = time;
                }
            }

            // if it has been 0.1 seconds since the last time we sent a message, send an empty message
            if (time - lastMessageSend >= 0.1)
            {
                sendAckPacket();
                lastMessageSend = time;
            }

            // flush any remaining messages in message packer
            flushMessagePacker();
        }

        protected void flushMessagePacker(bool bufferAck = true)
        {
            if (messagePacker.Length > 0)
            {
                var outgoingSeq = packetController.SendPacket(messagePacker.InternalBuffer, messagePacker.Length,
                    (byte) ChannelID);
                var outgoingPacket = ackBuffer.Insert(outgoingSeq);

                // store message IDs so we can map packet-level acks to message ID acks
                outgoingPacket.MessageIds.Clear();
                outgoingPacket.MessageIds.AddRange(tempList);

                messagePacker.SetSize(0);
                tempList.Clear();
            }
        }

        protected void ackPacket(ushort seq)
        {
            // first, map seq to message IDs and ack them
            var outgoingPacket = ackBuffer.Find(seq);
            if (outgoingPacket == null)
                return;

            // process messages
            for (var i = 0; i < outgoingPacket.MessageIds.Count; i++)
            {
                // remove acked message from send buffer
                var messageID = outgoingPacket.MessageIds[i];

                if (sendBuffer.Exists(messageID))
                {
                    sendBuffer.Find(messageID).writeLock = true;
                    sendBuffer.Remove(messageID);
                }
            }

            // update oldest unacked message
            var allAcked = true;
            for (var sequence = oldestUnacked;
                sequence == this.sequence || PacketIo.SequenceLessThan(sequence, this.sequence);
                sequence++) // if it's still in the send buffer, it hasn't been acked
                if (sendBuffer.Exists(sequence))
                {
                    oldestUnacked = sequence;
                    allAcked = false;
                    break;
                }

            if (allAcked)
                oldestUnacked = this.sequence;
        }

        // process incoming packets and turn them into messages
        protected void processPacket(ushort seq, byte[] packetData, int packetLen)
        {
            using (var reader = ByteArrayReaderWriter.Get(packetData))
            {
                while (reader.ReadPosition < packetLen)
                {
                    // get message bytes and send to receive callback
                    var messageID = reader.ReadUInt16();
                    var messageLength = readVariableLengthUShort(reader);

                    if (messageLength == 0)
                        continue;

                    if (!receiveBuffer.Exists(messageID))
                    {
                        var receivedMessage = receiveBuffer.Insert(messageID);

                        receivedMessage.buffer.SetSize(messageLength);
                        reader.ReadBytesIntoBuffer(receivedMessage.buffer.InternalBuffer, messageLength);
                    }
                    else
                    {
                        reader.SeekRead(reader.ReadPosition + messageLength);
                    }

                    // keep returning the next message we're expecting as long as it's available
                    while (receiveBuffer.Exists(nextReceive))
                    {
                        var msg = receiveBuffer.Find(nextReceive);

                        ReceiveCallback(msg.buffer.InternalBuffer, msg.buffer.Length);

                        receiveBuffer.Remove(nextReceive);
                        nextReceive++;
                    }
                }
            }
        }

        internal class BufferedPacket
        {
            public ByteBuffer buffer = new ByteBuffer();
            public double time;
            public bool writeLock = true;
        }

        internal class OutgoingPacketSet
        {
            public List<ushort> MessageIds = new List<ushort>();
        }
    }
}