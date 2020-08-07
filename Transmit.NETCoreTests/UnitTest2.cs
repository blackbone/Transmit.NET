using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using NUnit.Framework;
using Transmit;

namespace UnitTestProject1
{
    [TestFixture]
    public class UnitTest2
    {
        [TestCase]
        public async Task StartSocket()
        {
            var ep1 = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 10001);
            var ep2 = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 10002);

            ITransportChannel sock1 = new SocketTransport(ep1, ep2);
            ITransportChannel sock2 = new SocketTransport(ep2, ep1);

            var buffer = new byte[4];
            await sock1.SendBytes(new byte[] {1, 2, 3});
            await sock2.ReceiveBytes(buffer);

            sock1.Close();
            sock2.Close();
        }

        [TestCase]
        public async Task StartEndpoint()
        {
            var ep1 = new IPEndPoint(IPAddress.Any, 10001);
            var ep2 = new IPEndPoint(IPAddress.Any, 10002);

            ITransportChannel sock1 = new SocketTransport(ep1, ep2);
            ITransportChannel sock2 = new SocketTransport(ep2, ep1);

            var rep1 = new ReliableEndpoint(sock1,
                QosType.Reliable,
                QosType.Unreliable,
                QosType.UnreliableOrdered);
            var rep2 = new ReliableEndpoint(sock2,
                QosType.Reliable,
                QosType.Unreliable,
                QosType.UnreliableOrdered);
        }

        private class SocketTransport : ITransportChannel
        {
            private readonly EndPoint _remoteEP;
            private readonly Socket _socket;

            private SocketReceiveFromResult _receiveResult;

            private ulong _sent = 0;
            private ulong _received = 0;

            public SocketTransport(EndPoint localEP, EndPoint remoteEP)
            {
                _remoteEP = remoteEP;

                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                _socket.Bind(localEP);
            }

            public async Task SendBytes(ArraySegment<byte> buffer)
            {
                _sent += (ulong) await _socket.SendToAsync(buffer, SocketFlags.None, _remoteEP);
            }

            public async Task ReceiveBytes(ArraySegment<byte> buffer)
            {
                _receiveResult = await _socket.ReceiveFromAsync(buffer, SocketFlags.None, _remoteEP);
                _received += (ulong) _receiveResult.ReceivedBytes;
            }

            public void Close()
            {
                _socket.Close();
            }
        }
    }
}