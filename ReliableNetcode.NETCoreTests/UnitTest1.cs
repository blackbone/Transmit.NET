using System;
using System.Collections.Generic;
using NUnit.Framework;
using ReliableNetcode;

namespace UnitTestProject1
{
    [TestFixture]
    public class UnitTest1
    {
        // tests with randomness are run this many times to ensure tests are unlikely to randomly pass
        private const int RANDOM_RUNS = 100;

        /// <summary>
        ///     All messages are sent through the Reliable channel with no packet loss. All messages should be received.
        /// </summary>
        [TestCase]
        public void TestBasicSending()
        {
            var sentPackets = new List<byte>();
            var receivedPackets = new List<byte>();

            var endpoint1 = new ReliableEndpoint();
            endpoint1.ReceiveCallback = (buffer, size) =>
            {
                if (buffer[0] == 0)
                    receivedPackets.Add(buffer[1]);
            };
            endpoint1.TransmitCallback = (buffer, size) => { endpoint1.ReceivePacket(buffer, size); };

            for (var i = 0; i < 100; i++)
            {
                sentPackets.Add((byte) i);
                var test = new byte[256];
                test[0] = 0;
                test[1] = (byte) i;
                endpoint1.SendMessage(test, 256, QosType.Reliable);
            }

            var iterations = 0;
            for (var i = 0; i < 5000; i++)
            {
                endpoint1.UpdateFastForward(1.0);
                iterations++;

                if (receivedPackets.Count == sentPackets.Count) break;
            }

            if (receivedPackets.Count == sentPackets.Count)
            {
                var compare = true;
                for (var i = 0; i < receivedPackets.Count; i++)
                    if (receivedPackets[i] != sentPackets[i])
                    {
                        compare = false;
                        break;
                    }

                if (!compare) throw new Exception("Received packet contents differ!");
            }
            else
            {
                throw new Exception(sentPackets.Count - receivedPackets.Count + " packets not received");
            }
        }

        /// <summary>
        ///     Test ushort message ID wrapping
        /// </summary>
        [TestCase]
        public void TestIDWrapping()
        {
            var sentPackets = new List<byte>();
            var receivedPackets = new List<byte>();

            var endpoint1 = new ReliableEndpoint();
            endpoint1.ReceiveCallback = (buffer, size) =>
            {
                if (buffer[0] == 0)
                    receivedPackets.Add(buffer[1]);
            };
            endpoint1.TransmitCallback = (buffer, size) => { endpoint1.ReceivePacket(buffer, size); };

            for (var i = 0; i < 68000; i++)
            {
                sentPackets.Add((byte) i);
                var test = new byte[256];
                test[0] = 0;
                test[1] = (byte) i;
                endpoint1.SendMessage(test, 256, QosType.Reliable);

                endpoint1.UpdateFastForward(0.1);
            }

            while (receivedPackets.Count < sentPackets.Count) endpoint1.UpdateFastForward(1.0);

            if (receivedPackets.Count == sentPackets.Count)
            {
                var compare = true;
                for (var i = 0; i < receivedPackets.Count; i++)
                    if (receivedPackets[i] != sentPackets[i])
                    {
                        compare = false;
                        break;
                    }

                if (!compare) throw new Exception("Received packet contents differ!");
            }
            else
            {
                throw new Exception(sentPackets.Count - receivedPackets.Count + " packets not received");
            }
        }

        /// <summary>
        ///     Packets are sent through the Reliable channel with random packet loss. All packets should be received
        /// </summary>
        [TestCase]
        public void TestReliability()
        {
            var rand = new Random();

            for (var run = 0; run < RANDOM_RUNS; run++)
            {
                Console.WriteLine("RUN: " + run);

                var sentPackets = new List<byte>();
                var receivedPackets = new List<byte>();

                var droppedPackets = 0;

                var endpoint1 = new ReliableEndpoint();
                endpoint1.ReceiveCallback = (buffer, size) =>
                {
                    if (buffer[0] == 0)
                        receivedPackets.Add(buffer[1]);
                };
                endpoint1.TransmitCallback = (buffer, size) =>
                {
                    if (rand.Next(100) > 50)
                        endpoint1.ReceivePacket(buffer, size);
                    else
                        droppedPackets++;
                };

                for (var i = 0; i < 100; i++)
                {
                    sentPackets.Add((byte) i);
                    var test = new byte[256];
                    test[0] = 0;
                    test[1] = (byte) i;
                    endpoint1.SendMessage(test, 256, QosType.Reliable);

                    endpoint1.UpdateFastForward(0.1);
                }

                var iterations = 0;
                while (receivedPackets.Count < sentPackets.Count)
                {
                    endpoint1.UpdateFastForward(1.0);
                    iterations++;
                }

                Console.WriteLine("Dropped packets: " + droppedPackets);

                if (receivedPackets.Count == sentPackets.Count)
                {
                    var compare = true;
                    for (var i = 0; i < receivedPackets.Count; i++)
                        if (receivedPackets[i] != sentPackets[i])
                        {
                            compare = false;
                            break;
                        }

                    if (!compare) throw new Exception("Received packet contents differ!");
                }
                else
                {
                    throw new Exception(sentPackets.Count - receivedPackets.Count + " packets not received");
                }
            }
        }

        /// <summary>
        ///     All packets are sent through the Unreliable channel with no packet loss. All packets should be received.
        /// </summary>
        [TestCase]
        public void TestBasicUnreliable()
        {
            var sentPackets = new List<byte>();
            var receivedPackets = new List<byte>();

            var endpoint1 = new ReliableEndpoint();
            endpoint1.ReceiveCallback = (buffer, size) =>
            {
                if (buffer[0] == 0)
                    receivedPackets.Add(buffer[1]);
            };
            endpoint1.TransmitCallback = (buffer, size) => { endpoint1.ReceivePacket(buffer, size); };

            for (var i = 0; i < 100; i++)
            {
                sentPackets.Add((byte) i);
                var test = new byte[256];
                test[0] = 0;
                test[1] = (byte) i;
                endpoint1.SendMessage(test, 256, QosType.Unreliable);
            }

            if (receivedPackets.Count == sentPackets.Count)
            {
                var compare = true;
                for (var i = 0; i < receivedPackets.Count; i++)
                    if (receivedPackets[i] != sentPackets[i])
                    {
                        compare = false;
                        break;
                    }

                if (!compare) throw new Exception("Received packet contents differ!");
            }
            else
            {
                throw new Exception(sentPackets.Count - receivedPackets.Count + " packets not received");
            }
        }

        /// <summary>
        ///     All packets are sent through the UnreliableOrdered channel with no packet loss. All packets should be received.
        /// </summary>
        [TestCase]
        public void TestBasicUnreliableOrdered()
        {
            var sentPackets = new List<byte>();
            var receivedPackets = new List<byte>();

            var endpoint1 = new ReliableEndpoint();
            endpoint1.ReceiveCallback = (buffer, size) =>
            {
                if (buffer[0] == 0)
                    receivedPackets.Add(buffer[1]);
            };
            endpoint1.TransmitCallback = (buffer, size) => { endpoint1.ReceivePacket(buffer, size); };

            for (var i = 0; i < 100; i++)
            {
                sentPackets.Add((byte) i);
                var test = new byte[256];
                test[0] = 0;
                test[1] = (byte) i;
                endpoint1.SendMessage(test, 256, QosType.UnreliableOrdered);
            }

            if (receivedPackets.Count == sentPackets.Count)
            {
                var compare = true;
                for (var i = 0; i < receivedPackets.Count; i++)
                    if (receivedPackets[i] != sentPackets[i])
                    {
                        compare = false;
                        break;
                    }

                if (!compare) throw new Exception("Received packet contents differ!");
            }
            else
            {
                throw new Exception(sentPackets.Count - receivedPackets.Count + " packets not received");
            }
        }

        /// <summary>
        ///     All packets are sent through the UnreliableOrdered channel with random reordering. Received packets should be in
        ///     order.
        /// </summary>
        [TestCase]
        public void TestUnreliableOrderedSequence()
        {
            var rand = new Random();

            for (var run = 0; run < RANDOM_RUNS; run++)
            {
                Console.WriteLine("RUN: " + run);

                var sentPackets = new List<byte>();
                var receivedPackets = new List<byte>();

                var testQueue = new List<byte[]>();

                var endpoint1 = new ReliableEndpoint();
                endpoint1.ReceiveCallback = (buffer, size) =>
                {
                    if (buffer[0] == 0)
                        receivedPackets.Add(buffer[1]);
                };
                endpoint1.TransmitCallback = (buffer, size) =>
                {
                    var index = testQueue.Count;
                    if (rand.Next(100) >= 50)
                        index = rand.Next(testQueue.Count);

                    var item = new byte[size];
                    Buffer.BlockCopy(buffer, 0, item, 0, size);

                    testQueue.Insert(index, item);
                };

                // semi-randomly enqueue packets
                for (var i = 0; i < 10; i++)
                {
                    sentPackets.Add((byte) i);
                    var test = new byte[256];
                    test[0] = 0;
                    test[1] = (byte) i;
                    endpoint1.SendMessage(test, 256, QosType.UnreliableOrdered);
                }

                // now dequeue all packets
                while (testQueue.Count > 0)
                {
                    var item = testQueue[0];
                    testQueue.RemoveAt(0);

                    endpoint1.ReceivePacket(item, item.Length);
                }

                // and verify that packets aren't out of order or duplicated
                var processed = new List<int>();
                var sequence = 0;
                for (var i = 0; i < receivedPackets.Count; i++)
                {
                    if (receivedPackets[i] < sequence) throw new Exception("Found out-of-order packet!");

                    if (processed.Contains(receivedPackets[i]))
                        throw new Exception("Found duplicate packet!");

                    processed.Add(receivedPackets[i]);
                    sequence = receivedPackets[i];
                }

                Console.WriteLine("Dropped packets: " + (sentPackets.Count - receivedPackets.Count));
            }
        }
    }
}