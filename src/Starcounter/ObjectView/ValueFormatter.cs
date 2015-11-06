
using System;

namespace Starcounter.ObjectView {

    public class ValueFormatter {
        
        public string GetBinary(Binary? b) {
            if (!b.HasValue) {
                return Db.NullString;
            }
            return Db.BinaryToHex(b.Value);
        }

        public string GetBoolean(bool? b) {
            if (!b.HasValue) {
                return Db.NullString;
            }

            return b.Value ? bool.TrueString : bool.FalseString;
        }

        public string GetByte(byte? b) {
            if (!b.HasValue) {
                return Db.NullString;
            }
            return b.Value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo);
        }

        public string GetDateTime(DateTime? d) {
            if (!d.HasValue) {
                return Db.NullString;
            }
            return d.Value.ToString();
        }

        public string GetDecimal(decimal? d) {
            if (!d.HasValue) {
                return Db.NullString;
            }
            return d.Value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo);
        }

        public string GetDouble(double? d) {
            if (!d.HasValue) {
                return Db.NullString;
            }
            return d.Value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo);
        }

        public string GetInt16(short? s) {
            if (!s.HasValue) {
                return Db.NullString;
            }
            return s.Value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo);
        }

        public string GetInt32(int? i) {
            if (!i.HasValue) {
                return Db.NullString;
            }
            return i.Value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo);
        }

        public string GetInt64(long? l) {
            if (!l.HasValue) {
                return Db.NullString;
            }
            return l.Value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo);
        }

        public string GetObject(IObjectView o) {
            if (o == null) {
                return Db.NullString;
            }
            return o.ToString();
        }

        public string GetSByte(sbyte? b) {
            if (!b.HasValue) {
                return Db.NullString;
            }
            return b.Value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo);
        }

        public string GetSingle(float? f) {
            if (!f.HasValue) {
                return Db.NullString;
            }
            return f.Value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo);
        }

        public string GetString(string s) {
            if (s == null) {
                return Db.NullString;
            }
            return s;
        }

        public string GetUInt16(ushort? u) {
            if (!u.HasValue) {
                return Db.NullString;
            }
            return u.Value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo);
        }

        public string GetUInt32(uint? u) {
            if (!u.HasValue) {
                return Db.NullString;
            }
            return u.Value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo);
        }

        public string GetUInt64(ulong? u) {
            if (!u.HasValue) {
                return Db.NullString;
            }
            return u.Value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo);
        }
    }
}
