using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Internal {
    public class Base64Binary {
        public static unsafe uint MeasureNeededSizeToEncode(UInt32 length) {
            return 4 * (length / 3) + ((length % 3 == 0) ? 0 : length % 3 + 1);
        }

        public static unsafe uint MeasureNeededSizeToDecode(UInt32 length) {
            return 3 * (length / 4) + (length % 4 == 0 ? 0 : length % 4 - 1);
        }
        
        public static unsafe uint Write(IntPtr buffer, Byte* value, UInt32 length) {
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
            return writtenLength;
        }

        public static unsafe uint Read(uint size, IntPtr ptr, byte* value) {
            uint quarNr = size / 4;
            uint reminder = size % 4;
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
            Debug.Assert(value + MeasureNeededSizeToDecode(size) == writing);
            return (uint)(writing - value);
        }

        public static unsafe byte[] Read(uint size, IntPtr ptr) {
            uint length = MeasureNeededSizeToDecode(size);
            byte[] value = new byte[length];
            fixed (byte* valuePtr = value) {
                Read(size, ptr, valuePtr);
            }
            return value;
        }
    }
}
