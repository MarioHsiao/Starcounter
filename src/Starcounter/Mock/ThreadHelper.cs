// ***********************************************************************
// <copyright file="ThreadHelper.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Internal;
using System;

namespace Starcounter
{

    /// <summary>
    /// Class ThreadHelper
    /// </summary>
    public static class ThreadHelper
    {

        /// <summary>
        /// Sets the yield block.
        /// </summary>
        public static void SetYieldBlock()
        {
            uint r = sccorelib.cm3_set_yblk((IntPtr)0);
            if (r == 0) return;
            throw ErrorCode.ToException(r);
        }

        /// <summary>
        /// Releases the yield block.
        /// </summary>
        public static void ReleaseYieldBlock()
        {
            uint r = sccorelib.cm3_rel_yblk((IntPtr)0);
            if (r == 0) return;
            ExceptionManager.HandleInternalFatalError(r);
        }
    }
}
