using Transmit.Utils;

namespace Transmit
{
    internal sealed class SequenceBuffer<T> where T : class, new()
    {
        private const uint NullSequence = 0xFFFFFFFF;
        private readonly T[] _entryData;
        private readonly uint[] _entrySequence;

        public ushort Sequence;

        public SequenceBuffer(int bufferSize)
        {
            Sequence = 0;
            Size = bufferSize;

            _entrySequence = new uint[bufferSize];
            for (var i = 0; i < bufferSize; i++)
                _entrySequence[i] = NullSequence;

            _entryData = new T[bufferSize];
            for (var i = 0; i < bufferSize; i++)
                _entryData[i] = new T();
        }

        public int Size { get; }

        public void Reset()
        {
            Sequence = 0;
            for (var i = 0; i < Size; i++)
                _entrySequence[i] = NullSequence;
        }

        private void RemoveEntries(int startSequence, int finishSequence)
        {
            if (finishSequence < startSequence)
                finishSequence += 65536;

            if (finishSequence - startSequence < Size)
                for (var sequence = startSequence; sequence <= finishSequence; sequence++)
                    _entrySequence[sequence % Size] = NullSequence;
            else
                for (var i = 0; i < Size; i++)
                    _entrySequence[i] = NullSequence;
        }

        public bool TestInsert(ushort sequence)
        {
            return !PacketIo.SequenceLessThan(sequence, (ushort) (this.Sequence - Size));
        }

        public T Insert(ushort sequence)
        {
            if (PacketIo.SequenceLessThan(sequence, (ushort) (this.Sequence - Size)))
                return null;

            if (PacketIo.SequenceGreaterThan((ushort) (sequence + 1), this.Sequence))
            {
                RemoveEntries(this.Sequence, sequence);
                this.Sequence = (ushort) (sequence + 1);
            }

            var index = sequence % Size;
            _entrySequence[index] = sequence;
            return _entryData[index];
        }

        public void Remove(ushort sequence)
        {
            _entrySequence[sequence % Size] = NullSequence;
        }

        public bool Available(ushort sequence)
        {
            return _entrySequence[sequence % Size] == NullSequence;
        }

        public bool Exists(ushort sequence)
        {
            return _entrySequence[sequence % Size] == sequence;
        }

        public T Find(ushort sequence)
        {
            var index = sequence % Size;
            var sequenceNum = _entrySequence[index];
            if (sequenceNum == sequence)
                return _entryData[index];
            return null;
        }

        public T AtIndex(int index)
        {
            var sequenceNum = _entrySequence[index];
            if (sequenceNum == NullSequence)
                return null;

            return _entryData[index];
        }

        public void GenerateAckBits(out ushort ack, out uint ackBits)
        {
            ack = (ushort) (this.Sequence - 1);
            ackBits = 0;

            uint mask = 1;
            for (var i = 0; i < 32; i++)
            {
                var sequence = (ushort) (ack - i);
                if (Exists(sequence))
                    ackBits |= mask;

                mask <<= 1;
            }
        }
    }
}