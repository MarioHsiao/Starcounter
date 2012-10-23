﻿// ***********************************************************************
// <copyright file="DbHelper.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Binding;
using Starcounter.Internal;
using System;

namespace Starcounter
{

    /// <summary>
    /// Class DbHelper
    /// </summary>
    public static class DbHelper
    {

        /// <summary>
        /// Strings the compare.
        /// </summary>
        /// <param name="str1">The STR1.</param>
        /// <param name="str2">The STR2.</param>
        /// <returns>System.Int32.</returns>
        /// <exception cref="System.ArgumentNullException">str1</exception>
        public static int StringCompare(string str1, string str2)
        {
            // TODO: Implement efficient string comparison.
            UInt32 ec;
            Int32 result;
            if (str1 == null)
            {
                throw new ArgumentNullException("str1");
            }
            if (str2 == null)
            {
                throw new ArgumentNullException("str2");
            }
            ec = sccoredb.SCCompareUTF16Strings(str1, str2, out result);
            if (ec == 0)
            {
                return result;
            }
            throw ErrorCode.ToException(ec);
        }

        /// <summary>
        /// Froms the ID.
        /// </summary>
        /// <param name="oid">The oid.</param>
        /// <returns>Entity.</returns>
        public static Entity FromID(ulong oid)
        {
            Boolean br;
            UInt16 codeClassIdx;
            ulong addr;
            unsafe
            {
                br = sccoredb.Mdb_OIDToETIEx(oid, &addr, &codeClassIdx);
            }
            if (br)
            {
                if (addr != sccoredb.INVALID_RECORD_ADDR)
                {
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
        /// Note that all the method does is to read the object identifier from
        /// the proxy. It doesn't check if the proxy is valid in any way, the
        /// underlying object may for example be have been deleted in which
        /// case the method returns the identifier the object had.
        /// </remarks>
        public static UInt64 GetObjectID(Entity obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }
            return obj.ThisRef.ObjectID;
        }
    }
}
