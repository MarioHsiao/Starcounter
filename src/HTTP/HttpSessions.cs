// ***********************************************************************
// <copyright file="HttpStructs.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace HttpStructs
{
    class GlobalSessions
    {
        // All global sessions.
        public static GlobalSessions AllSessions = new GlobalSessions();

        // Unique Apps number.
        Int64 apps_unique_session_num_ = 0;

        /// <summary>
        /// Generates new Apps unique number.
        /// </summary>
        /// <returns></returns>
        public UInt64 GenerateUniqueNumber()
        {
            return (UInt64)Interlocked.Increment(ref apps_unique_session_num_);
        }
    }
}