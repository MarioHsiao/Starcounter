// ***********************************************************************
// <copyright file="DbHelper.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Advanced;
using Starcounter.Binding;
using Starcounter.Internal;
using System;
using System.Web;
using System.Text;

namespace Starcounter {

    /// <summary>
    /// Class DbHelper
    /// </summary>
    public static class DbHelper {

        /// <summary>
        /// Strings the compare.
        /// </summary>
        /// <param name="str1">The STR1.</param>
        /// <param name="str2">The STR2.</param>
        /// <returns>System.Int32.</returns>
        /// <exception cref="System.ArgumentNullException">str1</exception>
        public static int StringCompare(string str1, string str2) {
            // TODO: Implement efficient string comparison.
            UInt32 ec;
            Int32 result;
            if (str1 == null) {
                throw new ArgumentNullException("str1");
            }
            if (str2 == null) {
                throw new ArgumentNullException("str2");
            }
            ec = sccoredb.SCCompareUTF16Strings(str1, str2, out result);
            if (ec == 0) {
                return result;
            }
            throw ErrorCode.ToException(ec);
        }

        /// <summary>
        /// Froms the ID.
        /// </summary>
        /// <param name="oid">The oid.</param>
        /// <returns>Entity.</returns>
        public static IObjectView FromID(ulong oid) {
            Boolean br;
            UInt16 codeClassIdx;
            ulong addr;
            unsafe {
                br = sccoredb.Mdb_OIDToETIEx(oid, &addr, &codeClassIdx);
            }
            if (br) {
                if (addr != sccoredb.INVALID_RECORD_ADDR) {
                    return Bindings.GetTypeBinding(codeClassIdx).NewInstance(addr, oid);
                }
                return null;
            }
            throw ErrorCode.ToException(sccoredb.Mdb_GetLastError());
        }

        /// <summary>
        /// Returns the object identifier of the specified object.
        /// </summary>
        /// <remarks>
        /// Note that all this method does is to read the object identifier from
        /// the proxy. It doesn't check if the proxy is valid in any way, the
        /// underlying object may for example be have been deleted in which
        /// case the method returns the identifier the object had.
        /// </remarks>
        /// <param name="obj">The object to get the identity from.</param>
        /// <exception cref="ArgumentNullException">
        /// Raised if <paramref name="obj"/> is null.</exception>
        /// <exception cref="InvalidCastException">Raise if Starcounter don't
        /// recognize the type of <paramref name="obj"/> as a type it knows
        /// how to get a database identity from.</exception>
        /// <returns>
        /// The unique object identity of the given object.
        /// </returns>
        public static ulong GetObjectNo(this object obj) {
            var bindable = obj as IBindable;
            if (bindable == null) {
                if (obj == null) {
                    throw new ArgumentNullException("obj");
                } else {
                    throw ErrorCode.ToException(
                        Error.SCERRCODENOTENHANCED,
                        string.Format("Don't know how to get the identity from objects with type {0}.", obj.GetType()),
                        (msg, inner) => { return new InvalidCastException(msg, inner); });
                }
            }

            return bindable.Identity;
        }

        /// <summary>
        /// Returns web friendly string of object identity.
        /// </summary>
        /// <param name="obj">The object to get the identity from.</param>
        /// <returns>The string</returns>
        public static string GetObjectID(this object obj) {
            var bindable = obj as IBindable;
            if (bindable == null) {
                if (obj == null) {
                    throw new ArgumentNullException("obj");
                } else {
                    throw ErrorCode.ToException(
                        Error.SCERRCODENOTENHANCED,
                        string.Format("Don't know how to get the identity from objects with type {0}.", obj.GetType()),
                        (msg, inner) => { return new InvalidCastException(msg, inner); });
                }
            }

            return Base64ForUrlEncode(bindable.Identity);
        }

        /// <summary>
        /// Returns the object identifier of the given <see cref="IBindable"/>
        /// instance.
        /// </summary>
        /// <param name="obj">The object to get the identity from.</param>
        /// <exception cref="ArgumentNullException">
        /// Raised if <paramref name="obj"/> is null.</exception>
        /// <returns>
        /// The unique object identity of the given object.
        /// </returns>
        public static ulong GetObjectNo(this IBindable obj) {
            if (obj == null) {
                throw new ArgumentNullException("obj");
            }
            return obj.Identity;
        }

        /// <summary>
        /// Returns web friendly string of the object identity of the given 
        /// <see cref="IBindable"/> instance.
        /// </summary>
        /// <param name="obj">The object to get the identity from.</param>
        /// <returns>The string</returns>
        public static string GetObjectID(this IBindable obj) {
            if (obj == null) {
                throw new ArgumentNullException("obj");
            }
            return Base64ForUrlEncode(obj.Identity);
        }

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