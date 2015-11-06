
using System;

namespace Starcounter.ObjectView {

    public class ValueFormatter {
        
        public virtual string GetBinary(Binary? b) {
            if (!b.HasValue) {
                return Db.NullString;
            }
            return Db.BinaryToHex(b.Value);
        }

        public virtual string GetBoolean(bool? b) {
            if (!b.HasValue) {
                return Db.NullString;
            }

            return b.Value ? bool.TrueString : bool.FalseString;
        }

        public virtual string GetByte(byte? b) {
            if (!b.HasValue) {
                return Db.NullString;
            }
            return b.Value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo);
        }

        public virtual string GetDateTime(DateTime? d) {
            if (!d.HasValue) {
                return Db.NullString;
            }
            return d.Value.ToString();
        }

        public virtual string GetDecimal(decimal? d) {
            if (!d.HasValue) {
                return Db.NullString;
            }
            return d.Value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo);
        }

        public virtual string GetDouble(double? d) {
            if (!d.HasValue) {
                return Db.NullString;
            }
            return d.Value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo);
        }

        public virtual string GetInt16(short? s) {
            if (!s.HasValue) {
                return Db.NullString;
            }
            return s.Value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo);
        }

        public virtual string GetInt32(int? i) {
            if (!i.HasValue) {
                return Db.NullString;
            }
            return i.Value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo);
        }

        public virtual string GetInt64(long? l) {
            if (!l.HasValue) {
                return Db.NullString;
            }
            return l.Value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo);
        }

        public virtual string GetObject(IObjectView o) {
            if (o == null) {
                return Db.NullString;
            }
            return o.ToString();
        }

        public virtual string GetSByte(sbyte? b) {
            if (!b.HasValue) {
                return Db.NullString;
            }
            return b.Value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo);
        }

        public virtual string GetSingle(float? f) {
            if (!f.HasValue) {
                return Db.NullString;
            }
            return f.Value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo);
        }

        public virtual string GetString(string s) {
            if (s == null) {
                return Db.NullString;
            }
            return s;
        }

        public virtual string GetUInt16(ushort? u) {
            if (!u.HasValue) {
                return Db.NullString;
            }
            return u.Value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo);
        }

        public virtual string GetUInt32(uint? u) {
            if (!u.HasValue) {
                return Db.NullString;
            }
            return u.Value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo);
        }

        public virtual string GetUInt64(ulong? u) {
            if (!u.HasValue) {
                return Db.NullString;
            }
            return u.Value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo);
        }
    }
}
