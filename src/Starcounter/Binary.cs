// ***********************************************************************
// <copyright file="Binary.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Starcounter.Internal;

namespace Starcounter
{

    /// <summary>
    /// Represents an immutable relatively small block of binary data.
    /// </summary>
    public struct Binary
    {
        /// <summary>
        /// The null
        /// </summary>
        public static readonly Binary Null = new Binary();

        /// <summary>
        /// Max size allowed for unpacked binary data (excluding header).
        /// </summary>
        public const int BINARY_DATA_MAX_SIZE = (1048576 - 1);

        internal static readonly Binary Infinite = ConstructInfinite();

        /// <summary>
        /// Equalses the specified LB1.
        /// </summary>
        /// <param name="lb1">The LB1.</param>
        /// <param name="lb2">The LB2.</param>
        /// <returns>Boolean.</returns>
        public static Boolean Equals(Binary lb1, Binary lb2)
        {
            return DoCompare(lb1, lb2);
        }

        /// <summary>
        /// Implements the ==.
        /// </summary>
        /// <param name="lb1">The LB1.</param>
        /// <param name="lb2">The LB2.</param>
        /// <returns>The result of the operator.</returns>
        public static Boolean operator ==(Binary lb1, Binary lb2)
        {
            return DoCompare(lb1, lb2);
        }

        /// <summary>
        /// Implements the !=.
        /// </summary>
        /// <param name="lb1">The LB1.</param>
        /// <param name="lb2">The LB2.</param>
        /// <returns>The result of the operator.</returns>
        public static Boolean operator !=(Binary lb1, Binary lb2)
        {
            return !DoCompare(lb1, lb2);
        }

        internal static unsafe Binary FromNative(Byte* buffer)
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
            // reserved for storing the length of the binary contents. Header
            // byte is included in the length.
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
            l += 5;
            for (i = 5; i < l; i++)
            {
                if (bc1[i] != bc2[i])
                {
                    return false;
                }
            }
            return true;
        }

        private static Binary ConstructInfinite() {
            var b = new Binary();
            b._buffer = new byte[] { 1, 0, 0, 0, 1 };
            return b;
        }

        //
        // The first 4 bytes in the buffer contains the length of the buffer.
        //
        private Byte[] _buffer;

        /// <summary>
        /// Initializes a new instance of the <see cref="Binary" /> struct.
        /// </summary>
        /// <param name="data">The data.</param>
        public Binary(Byte[] data)
            : this()
        {
            Int32 len;
            if (data != null)
            {
                len = data.Length;
                if (len > BINARY_DATA_MAX_SIZE)
                    throw ErrorCode.ToException(Error.SCERRBINARYVALUEEXCEEDSMAXSIZE,
                        "Maximum possible size is " + BINARY_DATA_MAX_SIZE + 
                        ", while actual size of data is " + len);
                int len2 = len + 1;
                _buffer = new Byte[len2 + 4];
                _buffer[0] = (Byte)len2;
                _buffer[1] = (Byte)(len2 >> 8);
                _buffer[2] = (Byte)(len2 >> 16);
                _buffer[3] = (Byte)(len2 >> 24);
                _buffer[4] = 0;
                Buffer.BlockCopy(data, 0, _buffer, 5, len2 - 1);
            }
            return;
        }

        internal unsafe Binary(ref TupleReaderBase64 tuple) {
            int len = (int)Base64Binary.MeasureNeededSizeToDecode(tuple.GetValueLength());
            if (len > BINARY_DATA_MAX_SIZE)
                throw ErrorCode.ToException(Error.SCERRBINARYVALUEEXCEEDSMAXSIZE,
                    "Maximum possible size is " + BINARY_DATA_MAX_SIZE +
                    ", while actual size of data is " + len);
            int len2 = len + 1;
            _buffer = new Byte[len2 + 4];
            _buffer[0] = (Byte)len2;
            _buffer[1] = (Byte)(len2 >> 8);
            _buffer[2] = (Byte)(len2 >> 16);
            _buffer[3] = (Byte)(len2 >> 24);
            _buffer[4] = 0;
            int written;
            fixed (byte* start = _buffer)
                written = tuple.ReadByteArray(start + 5);
            Debug.Assert(written == len || written == -1);
            if (written == -1) 
                _buffer = null;
        }

        internal unsafe Binary(ref SafeTupleReaderBase64 tuple, int index) {
            int len = Base64Binary.MeasureNeededSizeToDecode(tuple.GetValueLength(index));
            int len2 = len + 1;
            _buffer = new Byte[len2 + 4];
            _buffer[0] = (Byte)len2;
            _buffer[1] = (Byte)(len2 >> 8);
            _buffer[2] = (Byte)(len2 >> 16);
            _buffer[3] = (Byte)(len2 >> 24);
            _buffer[4] = 0;
            int written;
            fixed (byte* start = _buffer)
                written = tuple.ReadByteArray(index, start + 5, len2 - 1);
            Debug.Assert(written == len || written == -1);
            if (written == -1)
                _buffer = null;
        }

        /// <summary>
        /// Gets the is null.
        /// </summary>
        /// <value>The is null.</value>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
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
            ret = _buffer[5];
            if (l == 1)
            {
                return ret;
            }
            ret |= (((Int32)_buffer[6]) << 8);
            if (l == 2)
            {
                return ret;
            }
            ret |= (((Int32)_buffer[7]) << 16);
            if (l == 3)
            {
                return ret;
            }
            ret |= (((Int32)_buffer[8]) << 24);
            return ret;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
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
            shtLength += 5;
            for (Int32 i = 5; i < shtLength; i++)
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
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
                if (bc1[i + 5] == other[i])
                {
                    continue;
                }
                if (bc1[i + 5] < other[i])
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lb"></param>
        /// <returns></returns>
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

            return _buffer[index + 5];
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
                Buffer.BlockCopy(_buffer, 5, ret, 0, len);
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
            // The length is actually 4 bytes but byte 3 always contain 0.
            // Header byte is included in the length.
            return
                ((((int)_buffer[2]) << 16) | (((int)_buffer[1]) << 8) |
                _buffer[0]) - 1;
        }

        private void VerifyNotNull()
        {
            if (_buffer != null)
            {
                return;
            }
            throw new InvalidOperationException("Large binary is null.");
        }

        internal unsafe void WriteToTuple(ref TupleWriterBase64 tuple) {
            if (_buffer == null)
                tuple.WriteByteArray(null);
            else
                fixed (byte* start = _buffer) {
                    tuple.WriteByteArray(start + 5, GetLength());
                }
        }

        internal unsafe void WriteToTuple(ref SafeTupleWriterBase64 tuple) {
            if (_buffer == null)
                tuple.WriteByteArray(null);
            else
                fixed (byte* start = _buffer) {
                    tuple.WriteByteArray(start + 5, GetLength());
                }
        }
    }


    /// <summary>
    /// Class BinaryStream
    /// </summary>
    public sealed class BinaryStream : Stream
    {
        private const Int32 DEFAULT_CAPACITY = (4096 - 5);

        private Byte[] _buffer;
        private Int32 _position;
        private Int32 _length;
        private Boolean _isOpen;
        private Boolean _isWritable;
        private Boolean _isFrozen;

        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryStream" /> class.
        /// </summary>
        public BinaryStream() : this(DEFAULT_CAPACITY) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryStream" /> class.
        /// </summary>
        /// <param name="initialCapacity">The initial capacity.</param>
        public BinaryStream(Int32 initialCapacity)
        {
            _buffer = new Byte[AdjustInput(initialCapacity)];
            _position = 5;
            _length = 5;
            _isOpen = true;
            _isWritable = true;
            _isFrozen = false;
        }

        internal BinaryStream(Binary binary)
        {
            // The large binary has already been verified not to be null before
            // the stream is created.
            _buffer = binary.GetInternalBuffer();
            _position = 5;
            _length = (binary.GetLength() + 5);
            _isOpen = true;
            _isWritable = false;
            _isFrozen = true;
        }

        /// <summary>
        /// 
        /// </summary>
        public override Boolean CanRead
        {
            get
            {
                return _isOpen;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override Boolean CanSeek
        {
            get
            {
                return _isOpen;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override Boolean CanWrite
        {
            get
            {
                return _isWritable;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override void Flush() { }

        /// <summary>
        /// 
        /// </summary>
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

        /// <summary>
        /// 
        /// </summary>
        public override Int64 Length
        {
            get
            {
                return AdjustOutput(_length);
            }
        }

        /// <summary>
        /// 
        /// </summary>
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

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lb"></param>
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
            Buffer.BlockCopy(buffer, 5, _buffer, _position, count);
            _position += count;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Binary ToBinary()
        {
            _isFrozen = true;
            return Binary.FromStream(_buffer, _length);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(Boolean disposing)
        {
            _isOpen = false;
            _isWritable = false;
            base.Dispose(disposing);
        }

        private int AdjustInput(Int32 value)
        {
            return (value + 5);
        }

        private long AdjustInput(Int64 value)
        {
            return (value + 5);
        }

        private int AdjustOutput(Int32 value)
        {
            return (value - 5);
        }

        private long AdjustOutput(Int64 value)
        {
            return (value - 5);
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
