// ***********************************************************************
// <copyright file="RangeFlags.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Internal;
using System;

namespace Starcounter
{

    /// <summary>
    /// Enum RangeFlags
    /// </summary>
    [Flags]
    public enum RangeFlags : uint
    {
        /// <summary>
        /// The include lesser key
        /// </summary>
        IncludeFirstKey = sccoredb.SC_ITERATOR_RANGE_INCLUDE_FIRST_KEY,

        /// <summary>
        /// The include greater key
        /// </summary>
        IncludeLastKey = sccoredb.SC_ITERATOR_RANGE_INCLUDE_LAST_KEY,
    }
}
