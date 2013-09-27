// ***********************************************************************
// <copyright file="LargeBinary.cs" company="Starcounter AB">
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
    /// Represents an immutable large block of binary data.
    /// </summary>
    public struct LargeBinary
    {

        /// <summary>
        /// The null
        /// </summary>
        public static readonly LargeBinary Null = new LargeBinary();

        /// <summary>
        /// Equalses the specified LB1.
        /// </summary>
        /// <param name="lb1">The LB1.</param>
        /// <param name="lb2">The LB2.</param>
        /// <returns>Boolean.</returns>
        public static Boolean Equals(LargeBinary lb1, LargeBinary lb2)
        {
            return DoCompare(lb1, lb2);
        }

        /// <summary>
        /// Implements the ==.
        /// </summary>
        /// <param name="lb1">The LB1.</param>
        /// <param name="lb2">The LB2.</param>
        /// <returns>The result of the operator.</returns>
        public static Boolean operator ==(LargeBinary lb1, LargeBinary lb2)
        {
            return DoCompare(lb1, lb2);
        }

        /// <summary>
        /// Implements the !=.
        /// </summary>
        /// <param name="lb1">The LB1.</param>
        /// <param name="lb2">The LB2.</param>
        /// <returns>The result of the operator.</returns>
        public static Boolean operator !=(LargeBinary lb1, LargeBinary lb2)
        {
            return !DoCompare(lb1, lb2);
        }

        internal static unsafe LargeBinary FromNative(Byte* buffer)
        {
            Int32 len;
            Byte[] buffer2;
            LargeBinary ret;
            len = *((Int32*)buffer) + 4;
            buffer2 = new Byte[len];
            Marshal.Copy((IntPtr)buffer, buffer2, 0, len);
            ret = new LargeBinary();
            ret._buffer = buffer2;
            return ret;
        }

        internal static LargeBinary FromStream(Byte[] buffer, Int32 usage)
        {
            Int32 len;
            LargeBinary ret;
            // [usage] is the usage in the stream which includes the space
            // reserved for storing the length of the binary contents.
            len = (usage - 4);
            buffer[0] = (Byte)len;
            buffer[1] = (Byte)(len >> 8);
            buffer[2] = (Byte)(len >> 16);
            buffer[3] = (Byte)(len >> 24);
            ret = new LargeBinary();
            ret._buffer = buffer;
            return ret;
        }

        private static Boolean DoCompare(LargeBinary lb1, LargeBinary lb2)
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

        /// <summary>
        /// Initializes a new instance of the <see cref="LargeBinary" /> struct.
        /// </summary>
        /// <param name="data">The data.</param>
        public LargeBinary(Byte[] data)
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
        
        internal unsafe LargeBinary(ref TupleReaderBase64 tuple) {
            int len = (int)Base64Binary.MeasureNeededSizeToDecode(tuple.GetValueLength());
            _buffer = new Byte[len + 4];
            _buffer[0] = (Byte)len;
            _buffer[1] = (Byte)(len >> 8);
            _buffer[2] = (Byte)(len >> 16);
            _buffer[3] = (Byte)(len >> 24);
            int written;
            fixed (byte* start = _buffer)
                written = tuple.ReadByteArray(start + 4);
            Debug.Assert(written == len || written == -1);
            if (written == -1) 
                _buffer = null;
        }

        internal unsafe LargeBinary(ref SafeTupleReaderBase64 tuple, int index) {
            int len = (int)Base64Binary.MeasureNeededSizeToDecode((uint)tuple.GetValueLength(index));
            _buffer = new Byte[len + 4];
            _buffer[0] = (Byte)len;
            _buffer[1] = (Byte)(len >> 8);
            _buffer[2] = (Byte)(len >> 16);
            _buffer[3] = (Byte)(len >> 24);
            int written;
            fixed (byte* start = _buffer)
                written = tuple.ReadByteArray(index, start + 4, len);
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
        /// Gets the length of the <code>LargeBinary</code>.
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
        /// Indicates whether this instance and a specified object are equal.
        /// </summary>
        /// <param name="obj">Another object to compare to.</param>
        /// <returns>true if <paramref name="obj" /> and this instance are the same type and represent the same value; otherwise, false.</returns>
        public override Boolean Equals(Object obj)
        {
            if (obj == null)
            {
                return DoCompare(this, LargeBinary.Null);
            }
            if (obj is LargeBinary)
            {
                return DoCompare(this, ((LargeBinary)obj));
            }
            return false;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
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

        /// <summary>
        /// Equalses the specified lb.
        /// </summary>
        /// <param name="lb">The lb.</param>
        /// <returns>Boolean.</returns>
        public Boolean Equals(LargeBinary lb)
        {
            return DoCompare(this, lb);
        }

        /// <summary>
        /// Creates and returns a read-only stream based on the LargeBinary.
        /// </summary>
        public LargeBinaryStream GetStream()
        {
            VerifyNotNull();
            return new LargeBinaryStream(this);
        }

        internal Byte[] GetBuffer()
        {
            return _buffer;
        }

        internal Int32 GetLength()
        {
            return (
                       (((Int32)_buffer[3]) << 24) |
                       (((Int32)_buffer[2]) << 16) |
                       (((Int32)_buffer[1]) << 8) |
                       _buffer[0]
                   );
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
                    tuple.WriteByteArray(start + 4, (uint)GetLength());
                }
        }

        internal unsafe void WriteToTuple(ref SafeTupleWriterBase64 tuple) {
            if (_buffer == null)
                tuple.WriteByteArray(null);
            else
                fixed (byte* start = _buffer) {
                    tuple.WriteByteArray(start + 4, (uint)GetLength());
                }
        }
    }

    /// <summary>
    /// Class LargeBinaryStream
    /// </summary>
    public sealed class LargeBinaryStream : Stream
    {

        private const Int32 DEFAULT_CAPACITY = (4096 - 4);

        private Byte[] _buffer;
        private Int32 _position;
        private Int32 _length;
        private Boolean _isOpen;
        private Boolean _isWritable;
        private Boolean _isFrozen;

        /// <summary>
        /// Initializes a new instance of the <see cref="LargeBinaryStream" /> class.
        /// </summary>
        public LargeBinaryStream() : this(DEFAULT_CAPACITY) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="LargeBinaryStream" /> class.
        /// </summary>
        /// <param name="initialCapacity">The initial capacity.</param>
        public LargeBinaryStream(Int32 initialCapacity)
        {
            _buffer = new Byte[AdjustInput(initialCapacity)];
            _position = 4;
            _length = 4;
            _isOpen = true;
            _isWritable = true;
            _isFrozen = false;
        }

        internal LargeBinaryStream(LargeBinary binary)
        {
            // The large binary has already been verified not to be null before
            // the stream is created.
            _buffer = binary.GetBuffer();
            _position = 4;
            _length = (binary.GetLength() + 4);
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
        public void Write(LargeBinary lb)
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
            buffer = lb.GetBuffer();
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

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public LargeBinary ToLargeBinary()
        {
            _isFrozen = true;
            return LargeBinary.FromStream(_buffer, _length);
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
