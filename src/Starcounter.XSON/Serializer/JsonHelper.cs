using System;
using Starcounter.Internal;

namespace Starcounter.Advanced.XSON {
    /// <summary>
    /// 
    /// </summary>
    public static class JsonHelper {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="innerException"></param>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="value"></param>
        public static void ThrowWrongValueTypeException(Exception innerException, string name, string type, string value) {
            throw ErrorCode.ToException(
                            Error.SCERRJSONVALUEWRONGTYPE,
                            innerException,
                            string.Format("Property=\"{0} ({1})\", Value={2}", name, type, value),
                            (msg, e) => {
                                return new FormatException(msg, e);
                            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        public static void ThrowPropertyNotFoundException(string name) {
            throw ErrorCode.ToException(Error.SCERRJSONPROPERTYNOTFOUND, string.Format("Property=\"{0}\"", name));
        }

        //        public static void ThrowPropertyNotFoundException(IntPtr ptr, int size) {
        //            string property = "";
        //            int valueSize;
        //            JsonHelper.ParseString(ptr, size, out property, out valueSize);
        //            ThrowPropertyNotFoundException(property);
        //        }

        //        /// <summary>
        //        /// 
        //        /// </summary>
        //        public static void ThrowUnexpectedEndOfContentException() {
        //            throw ErrorCode.ToException(
        //                            Error.SCERRJSONUNEXPECTEDENDOFCONTENT,
        //                            "",
        //                            (msg, e) => {
        //                                return new FormatException(msg, e);
        //                            });
        //        }

        /// <summary>
        /// 
        /// </summary>
        public static void ThrowInvalidJsonException(string message) {
            throw ErrorCode.ToException(Error.SCERRINVALIDJSONFORINPUT, message);
        }
    }
}
