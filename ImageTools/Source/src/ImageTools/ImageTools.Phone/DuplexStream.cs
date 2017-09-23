using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace ImageTools
{
    public class DuplexStream : Stream
    {
        public DuplexStream()
        {
            this.Data = new List<byte>(4096);
        }

        public override bool CanRead
        {
            get
            {
                return true;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return true;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return true;
            }
        }

        public override long Length
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override long Position { get; set; }

        protected List<byte> Data { get; set; }

        public override void Close()
        {
            //this.Data = new List<byte>(0);
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    this.Position = (int)offset;
                    break;

                case SeekOrigin.Current:
                    this.Position += (int)offset;
                    break;

                case SeekOrigin.End:
                    throw new NotSupportedException();
            }

            return this.Position;
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int availableBytes = this.Data.Count - (int)this.Position;

            count = Math.Min(count, availableBytes);

            if (count < 0)
            {
                return 0;
            }

            this.Data.CopyTo((int)this.Position, buffer, offset, count);

            this.Position += count;

            return count;
        }

        public override int ReadByte()
        {
            if (this.Position >= this.Data.Count)
            {
                return -1;
            }

            var result = this.Data[(int)this.Position];

            this.Position++;

            return result;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            this.Data.AddRange(new CollectionSegment<byte>(buffer, offset, count));
        }

        public override void WriteByte(byte value)
        {
            this.Data.Add(value);
        }

        public byte[] GetBinaryData()
        {
            return this.Data.ToArray();
        }

        private class CollectionSegment<T> : ICollection<T>
        {
            public CollectionSegment(T[] source, int offset, int size)
            {
                this.Source = source;
                this.Offset = offset;
                this.Size = size;
            }

            private int Offset { get; set; }

            private int Size { get; set; }

            private T[] Source { get; set; }

            public IEnumerator<T> GetEnumerator()
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }

            public void Add(T item)
            {
                throw new NotImplementedException();
            }

            public void Clear()
            {
                throw new NotImplementedException();
            }

            public bool Contains(T item)
            {
                throw new NotImplementedException();
            }

            public void CopyTo(T[] array, int arrayIndex)
            {
                Array.Copy(this.Source, this.Offset + arrayIndex, array, 0, this.Size - arrayIndex);
            }

            public bool Remove(T item)
            {
                throw new NotImplementedException();
            }

            public int Count
            {
                get
                {
                    return this.Size - this.Offset;
                }
            }

            public bool IsReadOnly
            {
                get
                {
                    return true;
                }
            }
        }
    }
}
