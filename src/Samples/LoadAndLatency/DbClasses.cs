using System;
using System.Text;
using System.Threading;
using Starcounter;
using System.Diagnostics;

namespace LoadAndLatency
{
    // Data types for queries.
    public enum QueryDataTypes
    {
        DATA_INTEGER,
        DATA_STRING,
        DATA_DATETIME,
        DATA_STRING_LIKE,
        DATA_STRING_STARTS_WITH,
        DATA_DECIMAL
    }

    /// <summary>
    /// This class contains all variants of primitive
    /// data types supported by Starcounter.
    /// </summary>
    [Database]
    public class TestClass
    {
        public TestClass(
            Nullable<Boolean> prop_boolean_,

            Nullable<SByte> prop_sbyte_,
            Nullable<Byte> prop_byte_,

            Nullable<Int16> prop_int16_,
            Nullable<UInt16> prop_uint16_,

            Nullable<Int32> prop_int32_,
            Nullable<UInt32> prop_uint32_,

            Nullable<Int64> prop_int64_,
            Nullable<UInt64> prop_uint64_,

            Nullable<Decimal> prop_decimal_,
            Nullable<Double> prop_double_,
            Nullable<Single> prop_single_,

            Nullable<DateTime> prop_datetime_,

            Binary prop_binary_,
            LargeBinary prop_large_binary_,

            String prop_string_)
        {
            prop_boolean = prop_boolean_;

            prop_byte = prop_byte_;
            prop_int16 = prop_int16_;
            prop_uint16 = prop_uint16_;
            prop_int32 = prop_int32_;
            prop_uint32 = prop_uint32_;

            prop_int64 = prop_int64_;
            prop_uint64 = prop_uint64_;

            prop_decimal = prop_decimal_;
            prop_double = prop_double_;
            prop_single = prop_single_;
            prop_datetime = prop_datetime_;
            prop_binary = prop_binary_;
            prop_large_binary = prop_large_binary_;

            prop_string = prop_string_;
            prop_sbyte = prop_sbyte_;

            prop_int64_update = 0;
            prop_string_update = null;

            prop_int64_cycler = 0;
            prop_int64_update = 0;
            prop_string_update = "Nothing";
            prop_decimal_update = 0;
        }

        // The first property will get index 3 because of the upper entity class.
        // Bringing these fields to the beginning for testing purposes.
        public Nullable<Int64> prop_int64;
        public String prop_string;

        public Nullable<DateTime> prop_datetime;
        public Nullable<Decimal> prop_decimal;

        public Binary prop_binary;

        public Nullable<Double> prop_double;

        public Nullable<Int32> prop_int32;
        public Nullable<UInt32> prop_uint32;

        public Nullable<Single> prop_single;
        public Nullable<UInt64> prop_uint64;

        public Nullable<Boolean> prop_boolean;

        public Nullable<SByte> prop_sbyte;
        public Nullable<Byte> prop_byte;
        public Nullable<Int16> prop_int16;
        public Nullable<UInt16> prop_uint16;

        public LargeBinary prop_large_binary;

        // Checksum calculation always on Int64 property.
        public Int64 GetCheckSum()
        {
            return prop_int64.Value;
        }

        // Doing simple update.
        public Int64 prop_int64_cycler;
        public Int64 prop_int64_update;
        public String prop_string_update;
        public Decimal prop_decimal_update;

        /// <summary>
        /// Performs a simple update on given data type.
        /// </summary>
        public void DoSimpleUpdate(String[,] shuffled_string_array, Int32 numColumns, QueryDataTypes dataType, Int32 workerId)
        {
            // Depending on data type.
            switch (dataType)
            {
                // Integer.
                case QueryDataTypes.DATA_INTEGER:
                case QueryDataTypes.DATA_DATETIME: // DateTime update is approximately the same as Integer.
                {
                    prop_int64_update++;
                    break;
                }

                // String.
                case QueryDataTypes.DATA_STRING:
                case QueryDataTypes.DATA_STRING_LIKE:
                case QueryDataTypes.DATA_STRING_STARTS_WITH:
                {
                    prop_string_update = shuffled_string_array[workerId, prop_int64_cycler];
                    break;
                }

                // Decimal.
                case QueryDataTypes.DATA_DECIMAL:
                {
                    prop_decimal_update++;
                    break;
                }
            }

            // Cycling values.
            prop_int64_cycler++;
            if (prop_int64_cycler >= numColumns)
                prop_int64_cycler = 0;
        }
    }

    // Simple class that is used for testing insert/delete performance.
    [Database]
    public class SimpleObject
    {
        public Int64 fetchInt;
        public Int64 updateInt;
        public String updateString;

        // Simple constructor.
        public SimpleObject(Int64 initValue)
        {
            fetchInt = initValue;
            updateInt = 0;
            updateString = "String";
        }

        /// <summary>
        /// Returns value of a fetched property.
        /// </summary>
        public Int64 FetchInt()
        {
            return fetchInt;
        }

        /// <summary>
        /// Updates additional property.
        /// </summary>
        public void UpdateInt()
        {
            updateInt++;
        }

        /// <summary>
        /// Shadow index update.
        /// </summary>
        public void UpdateIntShadow()
        {
            fetchInt = -fetchInt;
        }

        /// <summary>
        /// String/Int update to the same value.
        /// </summary>
        public void UpdateSameStringInt()
        {
            updateString = "String";
            updateInt = 123;
        }
    }
}