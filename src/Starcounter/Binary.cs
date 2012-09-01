﻿
using Sc.Server.Internal;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Starcounter
{

    /// <summary>
    /// Represents an immutable relatively small block of binary data.
    /// </summary>
    public struct Binary
    {
        public static readonly Binary Null = new Binary();

        public static Boolean Equals(Binary lb1, Binary lb2)
        {
            return DoCompare(lb1, lb2);
        }

        public static Boolean operator ==(Binary lb1, Binary lb2)
        {
            return DoCompare(lb1, lb2);
        }

        public static Boolean operator !=(Binary lb1, Binary lb2)
        {
            return !DoCompare(lb1, lb2);
        }

        public static unsafe Binary FromNative(Byte* buffer)
        {
            Int32 len;
            Byte[] buffer2;
            Binary ret;
            len = (*((Int32*)buffer)) + 4;
            buffer2 = new Byte[len];
            Marshal.Copy((IntPtr)buffer, buffer2, 0, len);
            ret = new Binary();
            ret._buffer = buffer2;
            return ret;
        }

        internal static Binary FromStream(Byte[] buffer, Int32 usage)
        {
            Int32 len;
            Binary ret;
            // [usage] is the usage in the stream which includes the space
            // reserved for storing the length of the binary contents.
            len = (usage - 4);
            buffer[0] = (Byte)len;
            buffer[1] = (Byte)(len >> 8);
            buffer[2] = (Byte)(len >> 16);
            buffer[3] = (Byte)(len >> 24);
            ret = new Binary();
            ret._buffer = buffer;
            return ret;
        }

        private static Boolean DoCompare(Binary lb1, Binary lb2)
        {
            Byte[] bc1;
            Byte[] bc2;
            Int32 l;
            Int32 i;
            bc1 = lb1._buffer;
            bc2 = lb2._buffer;
            if (bc1 == null)
            {
                return (bc2 == null);
            }
            if (bc2 == null)
            {
                return false;
            }
            l = lb1.GetLength();
            if (l != lb2.GetLength())
            {
                return false;
            }
            l += 4;
            for (i = 4; i < l; i++)
            {
                if (bc1[i] != bc2[i])
                {
                    return false;
                }
            }
            return true;
        }

        //
        // The first 4 bytes in the buffer contains the length of the buffer.
        //
        private Byte[] _buffer;

        public Binary(Byte[] data)
            : this()
        {
            Int32 len;
            if (data != null)
            {
                len = data.Length;
                _buffer = new Byte[len + 4];
                _buffer[0] = (Byte)len;
                _buffer[1] = (Byte)(len >> 8);
                _buffer[2] = (Byte)(len >> 16);
                _buffer[3] = (Byte)(len >> 24);
                Buffer.BlockCopy(data, 0, _buffer, 4, len);
            }
            return;
        }

        public Boolean IsNull
        {
            get
            {
                return _buffer == null;
            }
        }

        /// <summary>
        /// Gets the length of the Binary.
        /// </summary>
        public Int32 Length
        {
            get
            {
                VerifyNotNull();
                return GetLength();
            }
        }

        /// <summary>
        /// Gets the internal length of the Binary buffer.
        /// </summary>
        public Int32 InternalLength
        {
            get
            {
                VerifyNotNull();
                return _buffer.Length;
            }
        }

        public override Boolean Equals(Object obj)
        {
            if (obj == null)
            {
                return DoCompare(this, Binary.Null);
            }
            if (obj is Binary)
            {
                return DoCompare(this, ((Binary)obj));
            }
            return false;
        }

        public override Int32 GetHashCode()
        {
            Int32 ret;
            Int32 l;
            // Making it easy for us. We just read the first 4 bytes as an
            // integer and use it as the hash code.
            if (_buffer == null)
            {
                return 0;
            }
            l = GetLength();
            if (l == 0)
            {
                return 0;
            }
            ret = _buffer[2];
            if (l == 1)
            {
                return ret;
            }
            ret |= (((Int32)_buffer[3]) << 8);
            if (l == 2)
            {
                return ret;
            }
            ret |= (((Int32)_buffer[4]) << 16);
            if (l == 3)
            {
                return ret;
            }
            ret |= (((Int32)_buffer[5]) << 24);
            return ret;
        }

        public int CompareTo(object obj)
        {
            if (obj == null)
            {
                return CompareTo(Binary.Null);
            }
            if (obj is Binary)
            {
                return CompareTo((Binary)obj);
            }
            if (obj is Byte[])
            {
                return CompareTo((Byte[])obj);
            }
            throw new ArgumentException("Can only compare Binary or Byte[]");
        }

        public int CompareTo(Binary other)
        {
            Byte[] bc1;
            Byte[] bc2;
            Int32 l1;
            Int32 l2;
            Int32 shtLength;
            bc1 = _buffer;
            bc2 = other._buffer;
            if ((bc1 == null) && (bc2 == null))
            {
                return 0;
            }
            if ((bc1 == null) && (bc2 != null))
            {
                return -1;
            }
            if ((bc1 != null) && (bc2 == null))
            {
                return 1;
            }
            l1 = GetLength();
            l2 = other.GetLength();
            shtLength = (l1 < l2) ? l1 : l2;
            shtLength += 4;
            for (Int32 i = 4; i < shtLength; i++)
            {
                if (bc1[i] == bc2[i])
                {
                    continue;
                }
                if (bc1[i] < bc2[i])
                {
                    return -1;
                }
                return 1;
            }
            if (l1 == l2)
            {
                return 0;
            }
            if (l1 < l2)
            {
                return -1;
            }
            return 1;
        }

        public int CompareTo(byte[] other)
        {
            Byte[] bc1;
            Int32 l1;
            Int32 l2;
            Int32 shtLength;
            bc1 = _buffer;
            if ((bc1 == null) && (other == null))
            {
                return 0;
            }
            if ((bc1 == null) && (other != null))
            {
                return -1;
            }
            if ((bc1 != null) && (other == null))
            {
                return 1;
            }
            l1 = GetLength();
            l2 = other.Length;
            shtLength = (l1 < l2) ? l1 : l2;
            for (Int32 i = 0; i < shtLength; i++)
            {
                if (bc1[i + 4] == other[i])
                {
                    continue;
                }
                if (bc1[i + 4] < other[i])
                {
                    return -1;
                }
                return 1;
            }
            if (l1 == l2)
            {
                return 0;
            }
            if (l1 < l2)
            {
                return -1;
            }
            return 1;
        }

        public Boolean Equals(Binary lb)
        {
            return DoCompare(this, lb);
        }

        /// <summary>
        /// Creates and returns a read-only stream based on the Binary.
        /// </summary>
        public BinaryStream GetStream()
        {
            VerifyNotNull();
            return new BinaryStream(this);
        }

        /// <summary>
        /// Returns the byte at the specified index.
        /// </summary>
        public Byte GetByte(Int32 index)
        {
            VerifyNotNull();
            if (index < 0 || index >= GetLength())
            {
                throw new ArgumentOutOfRangeException("index");
            }

            return _buffer[index + 4];
        }

        /// <summary>
        /// Returns a copy of the Binary as a byte-array.
        /// </summary>
        public Byte[] ToArray()
        {
            Int32 len;
            Byte[] ret;
            if (_buffer != null)
            {
                len = GetLength();
                ret = new Byte[len];
                Buffer.BlockCopy(_buffer, 4, ret, 0, len);
                return ret;
            }
            return null;
        }

        /// <summary>
        /// Returns the reference to internal byte buffer.
        /// </summary>
        internal Byte[] GetInternalBuffer()
        {
            return _buffer;
        }

        internal Int32 GetLength()
        {
            // The length is actually 4 bytes but byte 2 and 3 always contain
            // 0.
            return ((((Int32)_buffer[1]) << 8) | _buffer[0]);
        }

        private void VerifyNotNull()
        {
            if (_buffer != null)
            {
                return;
            }
            throw new InvalidOperationException("Large binary is null.");
        }
    }

    public sealed class BinaryStream : Stream
    {
        private const Int32 DEFAULT_CAPACITY = (4096 - 4);

        private Byte[] _buffer;
        private Int32 _position;
        private Int32 _length;
        private Boolean _isOpen;
        private Boolean _isWritable;
        private Boolean _isFrozen;

        public BinaryStream() : this(DEFAULT_CAPACITY) { }

        public BinaryStream(Int32 initialCapacity)
        {
            _buffer = new Byte[AdjustInput(initialCapacity)];
            _position = 4;
            _length = 4;
            _isOpen = true;
            _isWritable = true;
            _isFrozen = false;
        }

        internal BinaryStream(Binary binary)
        {
            // The large binary has already been verified not to be null before
            // the stream is created.
            _buffer = binary.GetInternalBuffer();
            _position = 4;
            _length = (binary.GetLength() + 4);
            _isOpen = true;
            _isWritable = false;
            _isFrozen = true;
        }

        public override Boolean CanRead
        {
            get
            {
                return _isOpen;
            }
        }

        public override Boolean CanSeek
        {
            get
            {
                return _isOpen;
            }
        }

        public override Boolean CanWrite
        {
            get
            {
                return _isWritable;
            }
        }

        public override void Flush() { }

        public Int32 Capacity
        {
            get
            {
                return AdjustOutput(_buffer.Length);
            }
            set
            {
                if (!_isOpen)
                {
                    ThrowClosedException();
                }
                if (!_isWritable)
                {
                    ThrowReadonlyException();
                }
                ThawIfFrozen();
                value = AdjustInput(value);
                if (value < _buffer.Length)
                {
                    throw new ArgumentOutOfRangeException("value");
                }
                if (value > _buffer.Length)
                {
                    Byte[] dst = new Byte[value];
                    if (_buffer.Length > 0)
                    {
                        Buffer.BlockCopy(_buffer, 0, dst, 0, _buffer.Length);
                    }
                    _buffer = dst;
                }
            }
        }

        public override Int64 Length
        {
            get
            {
                return AdjustOutput(_length);
            }
        }

        public override Int64 Position
        {
            get
            {
                return AdjustOutput(_position);
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value");
                }
                value = AdjustInput(value);
                _position = (Int32)value;
            }
        }

        public override Int32 ReadByte()
        {
            if (!_isOpen)
            {
                ThrowClosedException();
            }
            if (_position >= _length)
            {
                return -1;
            }
            return _buffer[_position++];
        }

        public override Int32 Read(Byte[] buffer, Int32 offset, Int32 count)
        {
            if (!_isOpen)
            {
                ThrowClosedException();
            }
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException("offset");
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count");
            }
            if ((offset + count) > buffer.Length)
            {
                throw new ArgumentException("offset and count is largen the the size of the buffer.");
            }
            Int32 toRead = _length - _position;
            if (toRead > count)
            {
                toRead = count;
            }
            if (toRead <= 0)
            {
                return 0;
            }
            Buffer.BlockCopy(_buffer, _position, buffer, offset, toRead);
            _position += toRead;
            return toRead;
        }

        public override Int64 Seek(Int64 offset, SeekOrigin origin)
        {
            if (!_isOpen)
            {
                ThrowClosedException();
            }
            offset = AdjustInput(offset);
            switch (origin)
            {
                case SeekOrigin.Begin:
                    if (offset < 0)
                    {
                        throw new ArgumentOutOfRangeException("offset");
                    }
                    this._position = (Int32)offset;
                    break;
                case SeekOrigin.Current:
                    if ((offset + _position) < 0)
                    {
                        throw new Exception("Cannot seek to a negative position.");
                    }
                    this._position = (Int32)offset;
                    break;
                case SeekOrigin.End:
                    if ((_length + offset) < 0)
                    {
                        throw new Exception("Cannot seek to a negative position.");
                    }
                    this._position = (_length + (Int32)offset);
                    break;
            }
            return (Int64)Position;
        }

        public override void SetLength(Int64 value)
        {
            if (!_isOpen)
            {
                ThrowClosedException();
            }
            if (!_isWritable)
            {
                ThrowReadonlyException();
            }
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException("value");
            }
            value = AdjustInput(value);
            if (value != (_length))
            {
                ThawIfFrozen();
                Int32 num = (Int32)value;
                if (!EnsureCapacity(num))
                {
                    Array.Clear(_buffer, num, _length - num);
                }
                _length = num;
                if (_position > _length)
                {
                    _position = _length;
                }
            }
        }

        public override void WriteByte(Byte value)
        {
            Int32 nlen;
            if (!_isOpen)
            {
                ThrowClosedException();
            }
            if (!_isWritable)
            {
                ThrowReadonlyException();
            }
            ThawIfFrozen();
            if (_position >= _length)
            {
                nlen = (_position + 1);
                EnsureCapacity(nlen);
                _length = nlen;
            }
            _buffer[_position++] = value;
        }

        public override void Write(Byte[] buffer, Int32 offset, Int32 count)
        {
            if (!_isOpen)
            {
                ThrowClosedException();
            }
            if (!_isWritable)
            {
                ThrowReadonlyException();
            }
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException("offset");
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count");
            }
            if ((offset + count) > buffer.Length)
            {
                throw new ArgumentException("offset and count is larger the the size of the buffer.");
            }
            ThawIfFrozen();
            Int32 num = _position + count;
            if (num > _length)
            {
                EnsureCapacity(num);
                _length = num;
            }
            Buffer.BlockCopy(buffer, offset, _buffer, _position, count);
            _position += count;
        }

        public void Write(Binary lb)
        {
            Byte[] buffer;
            Int32 count;
            Int32 num;
            if (!_isOpen)
            {
                ThrowClosedException();
            }
            if (!_isWritable)
            {
                ThrowReadonlyException();
            }
            buffer = lb.GetInternalBuffer();
            if (buffer == null)
            {
                throw new ArgumentNullException("binary");
            }
            count = lb.GetLength();
            ThawIfFrozen();
            num = _position + count;
            if (num > _length)
            {
                EnsureCapacity(num);
                _length = num;
            }
            Buffer.BlockCopy(buffer, 4, _buffer, _position, count);
            _position += count;
        }

        public Binary ToBinary()
        {
            _isFrozen = true;
            return Binary.FromStream(_buffer, _length);
        }

        protected override void Dispose(Boolean disposing)
        {
            _isOpen = false;
            _isWritable = false;
            base.Dispose(disposing);
        }

        private int AdjustInput(Int32 value)
        {
            return (value + 4);
        }

        private long AdjustInput(Int64 value)
        {
            return (value + 4);
        }

        private int AdjustOutput(Int32 value)
        {
            return (value - 4);
        }

        private long AdjustOutput(Int64 value)
        {
            return (value - 4);
        }

        private Boolean EnsureCapacity(Int32 value)
        {
            Int32 num;
            Byte[] temp;
            if (value <= _buffer.Length)
            {
                return false;
            }
            num = value;
            if (num < 0xFF)
            {
                num = 0xFF;
            }
            if (num < (_buffer.Length * 2))
            {
                num = _buffer.Length * 2;
            }
            temp = new Byte[num];
            Buffer.BlockCopy(_buffer, 0, temp, 0, _buffer.Length);
            _buffer = temp;
            return true;
        }

        private void ThawIfFrozen()
        {
            if (_isFrozen)
            {
                Byte[] copy = new Byte[_buffer.Length];
                Buffer.BlockCopy(_buffer, 0, copy, 0, copy.Length);
                _buffer = copy;
                _isFrozen = false;
            }
        }

        private void ThrowReadonlyException()
        {
            throw new NotSupportedException("The stream is readonly.");
        }

        private void ThrowClosedException()
        {
            throw new ObjectDisposedException(null);
        }
    }
}
