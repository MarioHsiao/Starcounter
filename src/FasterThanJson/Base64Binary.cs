﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Internal {
    public class Base64Binary {
        public static unsafe uint MeasureNeededSizeToEncode(UInt32 length) {
            return 4 * (length / 3) + ((length % 3 == 0) ? 0 : length % 3 + 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
        public static unsafe uint MeasureNeededSizeToDecode(UInt32 length) {
            return 3 * (length >> 2) + ((length & 0x00000003) == 0 ? 0 : (length & 0x00000003) - 1);
        }
        
        public static unsafe uint Write(byte* buffer, Byte* value, UInt32 length) {
            byte* start = value;
            
            uint triplesNr = length / 3;
            uint reminder = length % 3;
            uint writtenLength = 0;
            
            for (uint i = 0; i <triplesNr; i++) {
                uint triple = *(byte*)value++;
                triple = (triple << 8) | (*(byte*)value++);
                triple = (triple << 8) | (*(byte*)value++);
                Base64Int.WriteBase64x4(triple, buffer);
                writtenLength += 4;
                buffer+= 4;
            }
            switch (reminder) {
                case 1:
                    Base64Int.WriteBase64x2(*(byte*)value, buffer);
                    writtenLength += 2;
                    value += 1;
                    break;
                case 2:
                    Base64Int.WriteBase64x3(*(ushort*)value, buffer);
                    writtenLength += 3;
                    value += 2;
                    break;
            }
            Debug.Assert(value == start + length);
            Debug.Assert(writtenLength != 1);
            return writtenLength;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
        public static unsafe uint Write(byte* buffer, Byte[] value) {
            if (value == null) {
                Base64Int.WriteBase64x1(0, buffer);
                return 1;
            } else
                fixed (byte* valuePtr = value)
                    return Write(buffer, valuePtr, (uint)value.Length);
        }

        public static unsafe uint Read(uint size, byte* ptr, byte* value) {
            if (size == 1)
                throw ErrorCode.ToException(Error.SCERRBADARGUMENTS, 
                    "Byte array to read is null, which cannot be written.");
            uint quarNr = size >> 2;
            uint reminder = size - (quarNr << 2);
            Debug.Assert(reminder != 1);
            byte* writing = value;
            for (uint i = 0; i < quarNr; i++) {
                uint triple = (uint)Base64Int.ReadBase64x4(ptr);
                ptr += 4;
                Debug.Assert((triple & 0xFF000000) == 0);
                *(byte*)writing = (byte)(triple >> 16);
                writing++;
                *(byte*)writing = (byte)((triple & 0x0000FF00) >> 8);
                writing++;
                *(byte*)writing = (byte)(triple & 0x000000FF);
                writing++;
            }
            switch (reminder) {
                case 2:
                    ulong single = Base64Int.ReadBase64x2(ptr);
                    Debug.Assert((single & 0xFFFFFFFFFFFFFF00) == 0);
                    *(byte*)writing = (byte)single;
                    writing++;
                    break;
                case 3:
                    ulong twin = Base64Int.ReadBase64x3(ptr);
                    Debug.Assert((twin & 0xFFFFFFFFFFFF0000) == 0);
                    *(ushort*)writing = (ushort)twin;
                    writing += 2;
                    break;
            }
            Debug.Assert(reminder != 1);
            Debug.Assert(value + MeasureNeededSizeToDecode(size) == writing);
            return (uint)(writing - value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
        public static unsafe bool IsNull(uint size, byte* ptr) {
            return size == 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
        public static unsafe byte[] Read(uint size, byte* ptr) {
            byte[] value;
            if (size != 1) {
                uint length = MeasureNeededSizeToDecode(size);
                value = new byte[length];
                fixed (byte* valuePtr = value) {
                    Read(size, ptr, valuePtr);
                }
            }
            else
                value = null;
            return value;
        }
    }
}
