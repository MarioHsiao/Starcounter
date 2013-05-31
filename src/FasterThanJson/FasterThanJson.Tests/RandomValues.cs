using System;
using System.Diagnostics;
using System.Text;

namespace FasterThanJson.Tests {
    public enum ValueTypes {
        NOVALUE = 0,
        UINT,
        STRING,
        BINARY
    }

    public static class RandomValues {
        public static UInt32 RandomUInt(Random rnd) {
            Int32 genValue = rnd.Next(Int32.MinValue, Int32.MaxValue);
            Int64 value = (Int64)genValue - Int32.MinValue;
            Trace.Assert(value >= UInt32.MinValue && value <= UInt32.MaxValue);
            return (UInt32)(value);
        }

        public static String RandomString(Random rnd) {
            int length = rnd.Next(0, 200);
            StringBuilder str = new StringBuilder(length);
            for (int i = 0; i < length; i++)
                str.Append((char)rnd.Next(UInt16.MinValue, UInt16.MaxValue));
            return str.ToString();
        }

        public static byte[] RandomBinary(Random rnd) {
            int length = rnd.Next(0, 500);
            byte[] value = new byte[length];
            for (int i = 0; i < length; i++)
                value[i] = (byte)rnd.Next(byte.MinValue, byte.MaxValue);
            return value;
        }
    }
}
