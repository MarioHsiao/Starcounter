using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter;
using System.Diagnostics;

namespace QueryProcessingTest
{
    [Database]
    public class UnloadReloadDataModel
    {
        public UnloadReloadDataModel()
        {
            bool_true = true;
            bool_false = false;
            bin_val = new Binary(new byte[] { 1, 2, 3, 4 });
            bin_null = Binary.Null;
            byte_min = byte.MinValue;
            byte_max = byte.MaxValue;
            datetime_min = DateTime.MinValue;
            datetime_max = DateTime.MinValue;
            decimal_min = Starcounter.Internal.X6Decimal.MinDecimalValue;
            decimal_max = Starcounter.Internal.X6Decimal.MaxDecimalValue;
            double_min = double.MinValue;
            double_max = double.MaxValue;
            double_neg_inf = double.NegativeInfinity;
            double_pos_inf = double.PositiveInfinity;
            double_nan = double.NaN;
            double_eps = double.Epsilon;
            short_min = short.MinValue;
            short_max = short.MaxValue;
            int_min = int.MinValue;
            int_max = int.MaxValue;
            long_min = long.MinValue;
            long_max = long.MaxValue;
            sbyte_min = sbyte.MinValue;
            sbyte_max = sbyte.MaxValue;
            float_min = float.MinValue;
            float_max = float.MaxValue;
            float_neg_inf = float.NegativeInfinity;
            float_pos_inf = float.PositiveInfinity;
            float_nan = float.NaN;
            float_eps = float.Epsilon;
            string_val = "abcd";
            ushort_min = ushort.MinValue;
            ushort_max = ushort.MaxValue;
            uint_min = uint.MinValue;
            uint_max = uint.MaxValue;
            ulong_min = ulong.MinValue;
            ulong_max = ulong.MaxValue;
        }
        public bool? bool_true { get; set; }
        public bool? bool_false { get; set; }
        public bool? bool_null { get; set; }

        public Binary bin_val { get; set; }
        public Binary bin_null { get; set; }

        public byte? byte_min { get; set; }
        public byte? byte_max { get; set; }
        public byte? byte_null { get; set; }

        public DateTime? datetime_min { get; set; }
        public DateTime? datetime_max { get; set; }
        public DateTime? datetime_null { get; set; }

        public decimal? decimal_min { get; set; }
        public decimal? decimal_max { get; set; }
        public decimal? decimal_null { get; set; }

        public double? double_min { get; set; }
        public double? double_max { get; set; }
        public double? double_neg_inf { get; set; }
        public double? double_pos_inf { get; set; }
        public double? double_nan { get; set; }
        public double? double_eps { get; set; }
        public double? double_null { get; set; }

        public short? short_min { get; set; }
        public short? short_max { get; set; }
        public short? short_null { get; set; }

        public int? int_min { get; set; }
        public int? int_max { get; set; }
        public int? int_null { get; set; }

        public long? long_min { get; set; }
        public long? long_max { get; set; }
        public long? long_null { get; set; }

        public UnloadReloadDataModel object_ref { get; set; }
        public UnloadReloadDataModel object_null { get; set; }

        public sbyte? sbyte_min { get; set; }
        public sbyte? sbyte_max { get; set; }
        public sbyte? sbyte_null { get; set; }

        public float? float_min { get; set; }
        public float? float_max { get; set; }
        public float? float_neg_inf { get; set; }
        public float? float_pos_inf { get; set; }
        public float? float_nan { get; set; }
        public float? float_eps { get; set; }
        public float? float_null { get; set; }

        public string string_val { get; set; }
        public string string_null { get; set; }

        public ushort? ushort_min { get; set; }
        public ushort? ushort_max { get; set; }
        public ushort? ushort_null { get; set; }

        public uint? uint_min { get; set; }
        public uint? uint_max { get; set; }
        public uint? uint_null { get; set; }

        public ulong? ulong_min { get; set; }
        public ulong? ulong_max { get; set; }
        public ulong? ulong_null { get; set; }

    }

    public static class UnloadReloadTest
    {

        private class AbortTransactionException : System.Exception
        {
        }

        public static void Populate()
        {
            Db.Transact(() =>
            {
                if (!Db.SQL<UnloadReloadDataModel>("select d from UnloadReloadDataModel d").Any())
                {
                    var dm = new UnloadReloadDataModel();
                    dm.object_ref = dm;
                }
            });
        }

        public static void Check()
        {
            UnloadReloadDataModel actual = Db.SQL<UnloadReloadDataModel>("select d from UnloadReloadDataModel d").First;

            try
            {
                Db.Transact(() =>
                {
                    UnloadReloadDataModel expected = new UnloadReloadDataModel() { object_ref = actual };

                    foreach (var p in typeof(UnloadReloadDataModel).GetProperties())
                    {
                        var a = p.GetValue(actual);
                        var e = p.GetValue(expected);

                        Trace.Assert((a==null && e == null ) || a.Equals(e));
                    }

                    //intentionally abort transaction to avoid creating unnecessary object in database
                    throw new AbortTransactionException();

                });
            }
            catch(AbortTransactionException) //expected exception
            { }


        }

    }
}
