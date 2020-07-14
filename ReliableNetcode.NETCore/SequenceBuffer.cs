using ReliableNetcode.Utils;

namespace ReliableNetcode
{
    internal class SequenceBuffer<T> where T : class, new()
    {
        private const uint NULL_SEQUENCE = 0xFFFFFFFF;
        private readonly T[] entryData;
        private readonly uint[] entrySequence;

        public ushort sequence;

        public SequenceBuffer(int bufferSize)
        {
            sequence = 0;
            Size = bufferSize;

            entrySequence = new uint[bufferSize];
            for (var i = 0; i < bufferSize; i++)
                entrySequence[i] = NULL_SEQUENCE;

            entryData = new T[bufferSize];
            for (var i = 0; i < bufferSize; i++)
                entryData[i] = new T();
        }

        public int Size { get; }

        public void Reset()
        {
            sequence = 0;
            for (var i = 0; i < Size; i++)
                entrySequence[i] = NULL_SEQUENCE;
        }

        public void RemoveEntries(int startSequence, int finishSequence)
        {
            if (finishSequence < startSequence)
                finishSequence += 65536;

            if (finishSequence - startSequence < Size)
                for (var sequence = startSequence; sequence <= finishSequence; sequence++)
                    entrySequence[sequence % Size] = NULL_SEQUENCE;
            else
                for (var i = 0; i < Size; i++)
                    entrySequence[i] = NULL_SEQUENCE;
        }

        public bool TestInsert(ushort sequence)
        {
            return !PacketIO.SequenceLessThan(sequence, (ushort) (this.sequence - Size));
        }

        public T Insert(ushort sequence)
        {
            if (PacketIO.SequenceLessThan(sequence, (ushort) (this.sequence - Size)))
                return null;

            if (PacketIO.SequenceGreaterThan((ushort) (sequence + 1), this.sequence))
            {
                RemoveEntries(this.sequence, sequence);
                this.sequence = (ushort) (sequence + 1);
            }

            var index = sequence % Size;
            entrySequence[index] = sequence;
            return entryData[index];
        }

        public void Remove(ushort sequence)
        {
            entrySequence[sequence % Size] = NULL_SEQUENCE;
        }

        public bool Available(ushort sequence)
        {
            return entrySequence[sequence % Size] == NULL_SEQUENCE;
        }

        public bool Exists(ushort sequence)
        {
            return entrySequence[sequence % Size] == sequence;
        }

        public T Find(ushort sequence)
        {
            var index = sequence % Size;
            var sequenceNum = entrySequence[index];
            if (sequenceNum == sequence)
                return entryData[index];
            return null;
        }

        public T AtIndex(int index)
        {
            var sequenceNum = entrySequence[index];
            if (sequenceNum == NULL_SEQUENCE)
                return null;

            return entryData[index];
        }

        public void GenerateAckBits(out ushort ack, out uint ackBits)
        {
            ack = (ushort) (this.sequence - 1);
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