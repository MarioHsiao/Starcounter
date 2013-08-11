// ***********************************************************************
// <copyright file="ISqlResult.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections;
using System.Collections.Generic;

namespace Starcounter {
    public interface IRowEnumerator<T> : IEnumerator<T>, IDisposable {
        /// <summary>
        /// Gets offset key of the SQL enumerator if it is possible.
        /// </summary>
        Byte[] GetOffsetKey();
    }
}
