using System;
using System.Threading.Tasks;

namespace ReliableNetcode
{
    public interface ITransportChannel
    {
        Task SendBytes(ArraySegment<byte> buffer);
        Task ReceiveBytes(ArraySegment<byte> bytes);
        void Close();
    }
}