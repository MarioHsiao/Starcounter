using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Starcounter.ABCIPC {
    
    //  50: OK without additional data
    //  51: OK with parameters
    //  52: Still working, no data. Recieve again, for same request.
    //  53: Still working, with data. Recieve again, for same request.
    //  80: Fail without additional data
    //  81: Fail with carry.
    //  82: Unknown message (no handler)
    //  83: Wrong signature (not parameters, for example)
    //  84: Exception in handler (contains carry, e.g. e.ToString());
    public sealed class Reply {

        internal static class Protocol {

            public static Reply Parse(string stringReply) {
                Reply.ReplyType replyType;
                int code;
                string carry;

                code = int.Parse(stringReply.Substring(0, 2));
                replyType = (Reply.ReplyType)code;

                if (Reply.TypeHasCarry(replyType)) {
                    carry = stringReply.Substring(2);
                } else {
                    Trace.Assert(stringReply.Length == 2);
                    carry = null;
                }

                return new Reply(replyType, carry);
            }

            public static string MakeString(ReplyType type) {
                return MakeString(type, null);
            }

            public static string MakeString(ReplyType type, string carry) {
                string result = null;

                switch (type) {
                    case ReplyType.OK:
                    case ReplyType.Progress:
                    case ReplyType.Fail:
                    case ReplyType.BadSignature:
                        Trace.Assert(
                            carry == null, 
                            string.Format("Reply {0} does not support a carry", Enum.GetName(typeof(ReplyType), type))
                            );
                        result = ((int)type).ToString("D2");
                        break;

                    case ReplyType.OKWithCarry:
                    case ReplyType.ProgressWithCarry:
                    case ReplyType.FailWithCarry:
                    case ReplyType.HandlerException:
                    case ReplyType.UnknownMessage:
                        carry = carry ?? string.Empty;
                        result = string.Concat(((int)type).ToString("D2"), carry);
                        break;
                }

                return result;
            }
        }

        internal enum ReplyType {
            OK = 50,
            OKWithCarry = 51,
            Progress = 52,
            ProgressWithCarry = 53,
            Fail = 80,
            FailWithCarry = 81,
            UnknownMessage = 82,
            BadSignature = 83,
            HandlerException = 84
        }

        internal readonly ReplyType _type;
        internal readonly string _carry;

        public bool IsResponse {
            get {
                return _type != ReplyType.Progress && _type != ReplyType.ProgressWithCarry;
            }
        }

        public bool IsSuccess {
            get {
                return _type == ReplyType.OK || _type == ReplyType.OKWithCarry || !IsResponse;
            }
        }

        public bool HasCarry {
            get {
                return TypeHasCarry(_type);
            }
        }

        internal static Reply.ReplyType TypeFromResult(bool result) {
            return TypeFromResult(result, null);
        }

        internal static Reply.ReplyType TypeFromResult(bool result, string carry) {
            if (carry == null) {
                return result ? ReplyType.OK : ReplyType.Fail;
            }
            return result ? ReplyType.OKWithCarry : ReplyType.FailWithCarry;
        }

        internal static bool TypeHasCarry(Reply.ReplyType type) {
            return
                type == ReplyType.OKWithCarry ||
                type == ReplyType.ProgressWithCarry ||
                type == ReplyType.FailWithCarry ||
                type == ReplyType.HandlerException ||
                type == ReplyType.UnknownMessage;

        }

        internal Reply(Reply.ReplyType type, string carry) {
            _type = type;
            _carry = carry;
        }

        public bool TryGetCarry(out string carry) {
            if (HasCarry) {
                carry = _carry;
                return true;
            }
            carry = null;
            return false;
        }

        public override string ToString() {
            string c;
            if (TryGetCarry(out c)) {
                return string.Format("{0}:{1}", Enum.GetName(typeof(ReplyType), _type), c);
            } else {
                return Enum.GetName(typeof(ReplyType), _type);
            }
        }
    }
}
