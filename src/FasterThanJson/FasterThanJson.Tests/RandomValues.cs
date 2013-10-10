using System;
using System.Diagnostics;
using System.Text;
using Starcounter.Internal;

namespace FasterThanJson.Tests {
    public enum ValueTypes {
        NOVALUE = 0,
        UINT,
        STRING,
        BINARY,
        ULONG,
        INT,
        LONG,
        UINTNULL,
        ULONGNULL,
        INTNULL,
        LONGNULL,
        BOOL,
        BOOLNULL,
        DECIMALLOSSLESS,
        DECIMALNULL
    }

    public static class RandomValues {
        public static UInt32 RandomUInt(Random rnd) {
            Int32 genValue = rnd.Next(Int32.MinValue, Int32.MaxValue);
            Int64 value = (Int64)genValue - Int32.MinValue;
            Trace.Assert(value >= UInt32.MinValue && value <= UInt32.MaxValue);
            return (UInt32)(value);
        }

        public static UInt32? RandomNullableUInt(Random rnd) {
            if (rnd.Next(0, 10) == 0)
                return null;
            return RandomUInt(rnd);
        }

        public static UInt64 RandomULong(Random rnd) {
            UInt64 genVal = (UInt64)(rnd.NextDouble() * UInt64.MaxValue);
            Trace.Assert(genVal >= UInt64.MinValue && genVal <= UInt64.MaxValue);
            return genVal;
        }

        public static UInt64? RandomNullableULong(Random rnd) {
            if (rnd.Next(0, 20) == 0)
                return null;
            return RandomULong(rnd); ;
        }

        public static Int32 RandomInt(Random rnd) {
            Int32 genValue = rnd.Next(Int32.MinValue, Int32.MaxValue);
            return genValue;
        }

        public static Int32? RandomNullableInt(Random rnd) {
            if (rnd.Next(0, 10) == 0)
                return null;
            return RandomInt(rnd); ;
        }

        public static Int64 RandomLong(Random rnd) {
            UInt64 genVal = (UInt64)(rnd.NextDouble() * UInt64.MaxValue);
            Trace.Assert(genVal >= UInt64.MinValue && genVal <= UInt64.MaxValue);
            if (genVal > Int64.MaxValue)
                return (Int64)(genVal - Int64.MaxValue - 1);
            else
                return (Int64)genVal - Int64.MaxValue - 1;
        }

        public static Int64? RandomNullableLong(Random rnd) {
            if (rnd.Next(0,20) == 0)
                return null;
            return RandomLong(rnd);
        }


        public static String RandomString(Random rnd) {
            int length = rnd.Next(0, 200);
            return RandomString(rnd, length);
        }

        public static String RandomString(Random rnd, int length) {
            StringBuilder str = new StringBuilder(length);
            for (int i = 0; i < length; i++) {
                char c = (char)rnd.Next(char.MinValue, char.MaxValue+1);
                while (c >= 0xD800)
                    c = (char)rnd.Next(char.MinValue, char.MaxValue+1);
                str.Append(c);
            }
            return str.ToString();
        }

        public static byte[] RandomByteArray(Random rnd) {
            int length = rnd.Next(0, 500);
            return RandomByteArray(rnd, length);
        }
        public static byte[] RandomByteArray(Random rnd, int length) {
            byte[] value = new byte[length];
            for (int i = 0; i < length; i++)
                value[i] = (byte)rnd.Next(byte.MinValue, byte.MaxValue+1);
            return value;
        }

        public static Boolean RandomBoolean(Random rnd) {
            int gen = rnd.Next(0, 2);
            Debug.Assert(gen ==0 || gen == 1);
            if (gen == 1)
                return true;
            else
                return false;
        }

        public static Boolean? RandomNullabelBoolean(Random rnd) {
            int gen = rnd.Next(0, 3);
            Debug.Assert(gen >= 0 && gen <= 2);
            if (gen == 1)
                return true;
            else if (gen == 0)
                return false;
            else
                return null;
        }

        public static Decimal RandomDecimal(Random rnd) {
            double gen = rnd.NextDouble();
            bool sign = RandomBoolean(rnd);
            int scale = rnd.Next(0, 29);
            decimal val;
            if (sign)
                val = (decimal)(gen * (double)Decimal.MinValue);
            else
                val = (decimal)(gen * (double)Decimal.MaxValue);
            for (int i = 0; i < scale; i++)
                val = val / 10;
            return val;
        }

        public static Decimal RandomX6Decimal(Random rnd) {
            double gen = rnd.NextDouble();
            bool sign = RandomBoolean(rnd);
            decimal val;
            if (sign)
                val = (decimal)((long)(gen * X6Decimal.MinValue));
            else
                val = (decimal)((long)(gen * X6Decimal.MaxValue));
            val *= 0.000001m;
            Debug.Assert(val == X6Decimal.FromRaw(X6Decimal.ToRaw(val)));
            return val;
        }

        public static Decimal? RandomDecimalNullable(Random rnd) {
            if (rnd.Next(0, 10) == 0)
                return null;
            return RandomDecimal(rnd);
        }
    }
}
