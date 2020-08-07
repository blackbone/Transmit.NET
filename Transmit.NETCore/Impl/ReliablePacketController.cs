using System;
using Transmit.Utils;

namespace Transmit
{
    internal class ReliableConfig
    {
        public Action<ushort> AckPacketCallback;
        public float BandwidthSmoothingFactor;
        public int FragmentReassemblyBufferSize;
        public int FragmentSize;
        public int FragmentThreshold;
        public int MaxFragments;
        public int MaxPacketSize;
        public string Name;
        public int PacketHeaderSize;
        public float PacketLossSmoothingFactor;
        public Action<ushort, byte[], int> ProcessPacketCallback;
        public int ReceivedPacketBufferSize;
        public float RTTSmoothFactor;
        public int SentPacketBufferSize;

        public Action<byte[], int> TransmitPacketCallback;

        public static ReliableConfig DefaultConfig()
        {
            var config = new ReliableConfig();
            config.Name = "endpoint";
            config.MaxPacketSize = 16 * 1024;
            config.FragmentThreshold = 1024;
            config.MaxFragments = 16;
            config.FragmentSize = 1024;
            config.SentPacketBufferSize = 256;
            config.ReceivedPacketBufferSize = 256;
            config.FragmentReassemblyBufferSize = 64;
            config.RTTSmoothFactor = 0.25f;
            config.PacketLossSmoothingFactor = 0.1f;
            config.BandwidthSmoothingFactor = 0.1f;
            config.PacketHeaderSize = 28;

            return config;
        }
    }

    internal class ReliablePacketController
    {
        private readonly SequenceBuffer<FragmentReassemblyData> fragmentReassembly;
        private readonly SequenceBuffer<ReceivedPacketData> receivedPackets;
        private readonly SequenceBuffer<SentPacketData> sentPackets;
        public ReliableConfig config;
        private ushort sequence;

        private double time;

        public ReliablePacketController(ReliableConfig config, double time)
        {
            this.config = config;
            this.time = time;

            sentPackets = new SequenceBuffer<SentPacketData>(config.SentPacketBufferSize);
            receivedPackets = new SequenceBuffer<ReceivedPacketData>(config.ReceivedPacketBufferSize);
            fragmentReassembly = new SequenceBuffer<FragmentReassemblyData>(config.FragmentReassemblyBufferSize);
        }

        public float RTT { get; private set; }

        public float PacketLoss { get; private set; }

        public float SentBandwidthKBPS { get; private set; }

        public float ReceivedBandwidthKBPS { get; private set; }

        public float AckedBandwidthKBPS { get; private set; }

        public ushort NextPacketSequence()
        {
            return sequence;
        }

        public void Reset()
        {
            sequence = 0;

            for (var i = 0; i < config.FragmentReassemblyBufferSize; i++)
            {
                var reassemblyData = fragmentReassembly.AtIndex(i);
                if (reassemblyData != null) reassemblyData.PacketDataBuffer.SetSize(0);
            }

            sentPackets.Reset();
            receivedPackets.Reset();
            fragmentReassembly.Reset();
        }

        public void Update(double newTime)
        {
            time = newTime;

            // calculate packet loss
            {
                var baseSequence = (uint) (sentPackets.Sequence - config.SentPacketBufferSize + 1 + 0xFFFF);

                var numDropped = 0;
                var numSamples = config.SentPacketBufferSize / 2;
                for (var i = 0; i < numSamples; i++)
                {
                    var sequence = (ushort) (baseSequence + i);
                    var sentPacketData = sentPackets.Find(sequence);
                    if (sentPacketData != null && !sentPacketData.acked)
                        numDropped++;
                }

                var packetLoss = numDropped / (float) numSamples;
                if (Math.Abs(PacketLoss - packetLoss) > 0.00001f)
                    PacketLoss += (packetLoss - PacketLoss) * config.PacketLossSmoothingFactor;
                else
                    PacketLoss = packetLoss;
            }

            // calculate sent bandwidth
            {
                var baseSequence = (uint) (sentPackets.Sequence - config.SentPacketBufferSize + 1 + 0xFFFF);

                var bytesSent = 0;
                var startTime = double.MaxValue;
                var finishTime = 0.0;
                var numSamples = config.SentPacketBufferSize / 2;
                for (var i = 0; i < numSamples; i++)
                {
                    var sequence = (ushort) (baseSequence + i);
                    var sentPacketData = sentPackets.Find(sequence);
                    if (sentPacketData == null) continue;

                    bytesSent += (int) sentPacketData.packetBytes;
                    startTime = Math.Min(startTime, sentPacketData.time);
                    finishTime = Math.Max(finishTime, sentPacketData.time);
                }

                if (startTime != double.MaxValue && finishTime != 0.0)
                {
                    var sentBandwidth = bytesSent / (float) (finishTime - startTime) * 8f / 1000f;
                    if (Math.Abs(SentBandwidthKBPS - sentBandwidth) > 0.00001f)
                        SentBandwidthKBPS += (sentBandwidth - SentBandwidthKBPS) * config.BandwidthSmoothingFactor;
                    else
                        SentBandwidthKBPS = sentBandwidth;
                }
            }

            // calculate received bandwidth
            lock (receivedPackets)
            {
                var baseSequence = (uint) (receivedPackets.Sequence - config.ReceivedPacketBufferSize + 1 + 0xFFFF);

                var bytesReceived = 0;
                var startTime = double.MaxValue;
                var finishTime = 0.0;
                var numSamples = config.ReceivedPacketBufferSize / 2;
                for (var i = 0; i < numSamples; i++)
                {
                    var sequence = (ushort) (baseSequence + i);
                    var receivedPacketData = receivedPackets.Find(sequence);
                    if (receivedPacketData == null) continue;

                    bytesReceived += (int) receivedPacketData.packetBytes;
                    startTime = Math.Min(startTime, receivedPacketData.time);
                    finishTime = Math.Max(finishTime, receivedPacketData.time);
                }

                if (startTime != double.MaxValue && finishTime != 0.0)
                {
                    var receivedBandwidth = bytesReceived / (float) (finishTime - startTime) * 8f / 1000f;
                    if (Math.Abs(ReceivedBandwidthKBPS - receivedBandwidth) > 0.00001f)
                        ReceivedBandwidthKBPS +=
                            (receivedBandwidth - ReceivedBandwidthKBPS) * config.BandwidthSmoothingFactor;
                    else
                        ReceivedBandwidthKBPS = receivedBandwidth;
                }
            }

            // calculate acked bandwidth
            {
                var baseSequence = (uint) (sentPackets.Sequence - config.SentPacketBufferSize + 1 + 0xFFFF);

                var bytesSent = 0;
                var startTime = double.MaxValue;
                var finishTime = 0.0;
                var numSamples = config.SentPacketBufferSize / 2;
                for (var i = 0; i < numSamples; i++)
                {
                    var sequence = (ushort) (baseSequence + i);
                    var sentPacketData = sentPackets.Find(sequence);
                    if (sentPacketData == null || sentPacketData.acked == false) continue;

                    bytesSent += (int) sentPacketData.packetBytes;
                    startTime = Math.Min(startTime, sentPacketData.time);
                    finishTime = Math.Max(finishTime, sentPacketData.time);
                }

                if (startTime != double.MaxValue && finishTime != 0.0)
                {
                    var ackedBandwidth = bytesSent / (float) (finishTime - startTime) * 8f / 1000f;
                    if (Math.Abs(AckedBandwidthKBPS - ackedBandwidth) > 0.00001f)
                        AckedBandwidthKBPS += (ackedBandwidth - AckedBandwidthKBPS) * config.BandwidthSmoothingFactor;
                    else
                        AckedBandwidthKBPS = ackedBandwidth;
                }
            }
        }

        public void SendAck(byte channelID)
        {
            ushort ack;
            uint ackBits;

            lock (receivedPackets)
            {
                receivedPackets.GenerateAckBits(out ack, out ackBits);
            }

            var transmitData = BufferPool.GetBuffer(16);
            var headerBytes = PacketIo.WriteAckPacket(transmitData, channelID, ack, ackBits);

            config.TransmitPacketCallback(transmitData, headerBytes);

            BufferPool.ReturnBuffer(transmitData);
        }

        public ushort SendPacket(byte[] packetData, int length, byte channelID)
        {
            if (length > config.MaxPacketSize)
                throw new ArgumentOutOfRangeException($"Packet is too large to send, max packet size is {config.MaxPacketSize} bytes");

            var sequence = this.sequence++;
            ushort ack;
            uint ackBits;

            lock (receivedPackets)
            {
                receivedPackets.GenerateAckBits(out ack, out ackBits);
            }

            var sentPacketData = sentPackets.Insert(sequence);
            sentPacketData.time = time;
            sentPacketData.packetBytes = (uint) (config.PacketHeaderSize + length);
            sentPacketData.acked = false;

            if (length <= config.FragmentThreshold)
            {
                // regular packet

                var transmitData = BufferPool.GetBuffer(2048);
                var headerBytes = PacketIo.WritePacketHeader(transmitData, channelID, sequence, ack, ackBits);
                var transmitBufferLength = length + headerBytes;

                Buffer.BlockCopy(packetData, 0, transmitData, headerBytes, length);

                config.TransmitPacketCallback(transmitData, transmitBufferLength);

                BufferPool.ReturnBuffer(transmitData);
            }
            else
            {
                // fragmented packet

                var packetHeader = BufferPool.GetBuffer(Defines.MAX_PACKET_HEADER_BYTES);

                var packetHeaderBytes = 0;

                packetHeaderBytes = PacketIo.WritePacketHeader(packetHeader, channelID, sequence, ack, ackBits);

                var numFragments = length / config.FragmentSize + (length % config.FragmentSize != 0 ? 1 : 0);
                //int fragmentBufferSize = Defines.FRAGMENT_HEADER_BYTES + Defines.MAX_PACKET_HEADER_BYTES + config.FragmentSize;

                var fragmentPacketData = BufferPool.GetBuffer(2048);
                var qpos = 0;

                byte prefixByte = 1;
                prefixByte |= (byte) ((channelID & 0x03) << 6);

                for (var fragmentID = 0; fragmentID < numFragments; fragmentID++)
                    using (var writer = ByteArrayReaderWriter.Get(fragmentPacketData))
                    {
                        writer.Write(prefixByte);
                        writer.Write(channelID);
                        writer.Write(sequence);
                        writer.Write((byte) fragmentID);
                        writer.Write((byte) (numFragments - 1));

                        if (fragmentID == 0) writer.WriteBuffer(packetHeader, packetHeaderBytes);

                        var bytesToCopy = config.FragmentSize;
                        if (qpos + bytesToCopy > length)
                            bytesToCopy = length - qpos;

                        for (var i = 0; i < bytesToCopy; i++)
                            writer.Write(packetData[qpos++]);

                        var fragmentPacketBytes = (int) writer.WritePosition;
                        config.TransmitPacketCallback(fragmentPacketData, fragmentPacketBytes);
                    }

                BufferPool.ReturnBuffer(packetHeader);
                BufferPool.ReturnBuffer(fragmentPacketData);
            }

            return sequence;
        }

        public void ReceivePacket(byte[] packetData, int bufferLength)
        {
            if (bufferLength > config.MaxPacketSize)
                throw new ArgumentOutOfRangeException("Packet is larger than max packet size");
            if (packetData == null)
                throw new InvalidOperationException("Tried to receive null packet!");
            if (bufferLength > packetData.Length)
                throw new InvalidOperationException("Buffer length exceeds actual packet length!");

            var prefixByte = packetData[0];

            if ((prefixByte & 1) == 0)
            {
                // regular packet

                ushort sequence;
                ushort ack;
                uint ackBits;

                byte channelID;

                var packetHeaderBytes = PacketIo.ReadPacketHeader(packetData, 0, bufferLength, out channelID,
                    out sequence, out ack, out ackBits);

                bool isStale;
                lock (receivedPackets)
                {
                    isStale = !receivedPackets.TestInsert(sequence);
                }

                if (!isStale && (prefixByte & 0x80) == 0)
                {
                    if (packetHeaderBytes >= bufferLength)
                        throw new FormatException("Buffer too small for packet data!");

                    var tempBuffer = ObjPool<ByteBuffer>.Get();
                    tempBuffer.SetSize(bufferLength - packetHeaderBytes);
                    tempBuffer.BufferCopy(packetData, packetHeaderBytes, 0, tempBuffer.Length);

                    // process packet
                    config.ProcessPacketCallback(sequence, tempBuffer.InternalBuffer, tempBuffer.Length);

                    // add to received buffer
                    lock (receivedPackets)
                    {
                        var receivedPacketData = receivedPackets.Insert(sequence);

                        if (receivedPacketData == null)
                            throw new InvalidOperationException("Failed to insert received packet!");

                        receivedPacketData.time = time;
                        receivedPacketData.packetBytes = (uint) (config.PacketHeaderSize + bufferLength);
                    }

                    ObjPool<ByteBuffer>.Return(tempBuffer);
                }

                if (!isStale || (prefixByte & 0x80) != 0)
                    for (var i = 0; i < 32; i++)
                    {
                        if ((ackBits & 1) != 0)
                        {
                            var ack_sequence = (ushort) (ack - i);
                            var sentPacketData = sentPackets.Find(ack_sequence);

                            if (sentPacketData != null && !sentPacketData.acked)
                            {
                                sentPacketData.acked = true;

                                if (config.AckPacketCallback != null)
                                    config.AckPacketCallback(ack_sequence);

                                var rtt = (float) (time - sentPacketData.time) * 1000.0f;
                                if (RTT == 0f && rtt > 0f || Math.Abs(RTT - rtt) < 0.00001f)
                                    RTT = rtt;
                                else
                                    RTT += (rtt - RTT) * config.RTTSmoothFactor;
                            }
                        }

                        ackBits >>= 1;
                    }
            }
            else
            {
                // fragment packet

                int fragmentID;
                int numFragments;
                int fragmentBytes;

                ushort sequence;
                ushort ack;
                uint ackBits;

                byte fragmentChannelID;

                var fragmentHeaderBytes = PacketIo.ReadFragmentHeader(packetData, 0, bufferLength, config.MaxFragments,
                    config.FragmentSize,
                    out fragmentID, out numFragments, out fragmentBytes, out sequence, out ack, out ackBits,
                    out fragmentChannelID);

                var reassemblyData = fragmentReassembly.Find(sequence);
                if (reassemblyData == null)
                {
                    reassemblyData = fragmentReassembly.Insert(sequence);

                    // failed to insert into buffer (stale)
                    if (reassemblyData == null)
                        return;

                    reassemblyData.Sequence = sequence;
                    reassemblyData.Ack = 0;
                    reassemblyData.AckBits = 0;
                    reassemblyData.NumFragmentsReceived = 0;
                    reassemblyData.NumFragmentsTotal = numFragments;
                    reassemblyData.PacketBytes = 0;
                    Array.Clear(reassemblyData.FragmentReceived, 0, reassemblyData.FragmentReceived.Length);
                }

                if (numFragments != reassemblyData.NumFragmentsTotal)
                    return;

                if (reassemblyData.FragmentReceived[fragmentID])
                    return;

                reassemblyData.NumFragmentsReceived++;
                reassemblyData.FragmentReceived[fragmentID] = true;

                var tempFragmentData = BufferPool.GetBuffer(2048);
                Buffer.BlockCopy(packetData, fragmentHeaderBytes, tempFragmentData, 0,
                    bufferLength - fragmentHeaderBytes);

                reassemblyData.StoreFragmentData(fragmentChannelID, sequence, ack, ackBits, fragmentID,
                    config.FragmentSize, tempFragmentData, bufferLength - fragmentHeaderBytes);
                BufferPool.ReturnBuffer(tempFragmentData);

                if (reassemblyData.NumFragmentsReceived == reassemblyData.NumFragmentsTotal)
                {
                    // grab internal buffer and pass it to ReceivePacket. Internal buffer will be packet marked as normal packet, so it will go through normal packet path

                    // copy into new buffer to remove preceding offset (used to simplify variable length header handling)
                    var temp = ObjPool<ByteBuffer>.Get();
                    temp.SetSize(reassemblyData.PacketDataBuffer.Length - reassemblyData.HeaderOffset);
                    Buffer.BlockCopy(reassemblyData.PacketDataBuffer.InternalBuffer, reassemblyData.HeaderOffset,
                        temp.InternalBuffer, 0, temp.Length);

                    // receive packet
                    ReceivePacket(temp.InternalBuffer, temp.Length);

                    // return temp buffer
                    ObjPool<ByteBuffer>.Return(temp);

                    // clear reassembly
                    reassemblyData.PacketDataBuffer.SetSize(0);
                    fragmentReassembly.Remove(sequence);
                }
            }
        }
    }
}