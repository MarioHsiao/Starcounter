using System;
using Starcounter.Internal;

namespace Starcounter.XSON {
    /// <summary>
    /// 
    /// </summary>
    public static class ExceptionHelper {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="innerException"></param>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="value"></param>
        public static void ThrowWrongValueType(Exception innerException, string name, string type, string value) {
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
        public static void ThrowPropertyNotFound(string name) {
            throw ErrorCode.ToException(Error.SCERRJSONPROPERTYNOTFOUND, string.Format("Property=\"{0}\"", name));
        }
        
        /// <summary>
        /// 
        /// </summary>
        public static void ThrowInvalidJson(string message) {
            throw ErrorCode.ToException(Error.SCERRINVALIDJSONFORINPUT, message);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public static void ThrowUnexpectedEndOfContent(string message) {
            throw ErrorCode.ToException(Error.SCERRJSONUNEXPECTEDENDOFCONTENT, message);
        }
    }
}
