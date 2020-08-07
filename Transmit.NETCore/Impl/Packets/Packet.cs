using System.Runtime.InteropServices;

namespace ReliableNetcode.Packets
{
    public static class Packet
    {
        public sealed class PrivateConnectionToken
        {
            public ulong ClientId;
            public uint Timeout;
            public uint NumServerAddresses;
            public ServerAddress[] ServerAddresses;
            public byte[] ClientToServerKey = new byte[256];
            public byte[] ServerToClientKey = new byte[256];
            public byte[] UserData = new byte[256];
        }

        public abstract class ServerAddress
        {
            public byte AddressType;
        }

        public sealed class ServerAddressIP4 : ServerAddress
        {
            // IP4 fields
            public byte A;
            public byte B;
            public byte C;
            public byte D;
            public ushort Port;
        }

        public class ServerAddressIP6
        {
            // IP6 fields
            public byte A;
            public byte B;
            public byte C;
            public byte D;
            public byte E;
            public byte F;
            public byte G;
            public byte H;
            public ushort Port;
        }
    }
}