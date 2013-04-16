using System;
using System.Text;
using System.Web;

namespace Starcounter.Query {
    
    internal static class ObjectIdentityHelpMethods {

        ///<summary>
        /// Base 64 Encoding with URL and Filename Safe Alphabet using UTF-8 character set.
        ///</summary>
        ///<param name="objectNo">The original string</param>
        ///<returns>The Base64 encoded string</returns>
        internal static string Base64ForUrlEncode(UInt64 objectNo) {
            byte[] encbuff = Encoding.UTF8.GetBytes(objectNo.ToString());
            return HttpServerUtility.UrlTokenEncode(encbuff);
        }
        ///<summary>
        /// Decode Base64 encoded string with URL and Filename Safe Alphabet using UTF-8.
        ///</summary>
        ///<param name="objectID">Base64 code</param>
        ///<returns>The decoded string.</returns>
        internal static UInt64 Base64ForUrlDecode(string objectID) {
            byte[] decbuff = HttpServerUtility.UrlTokenDecode(objectID);
            return Convert.ToUInt64(Encoding.UTF8.GetString(decbuff));
        }
    }
}
