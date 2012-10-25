// ***********************************************************************
// <copyright file="SqlInterfaces.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections;
using System.Collections.Generic;
using se.sics.prologbeans;
using Starcounter.Binding;
using Starcounter.Query.Execution;
using Starcounter.Query.Sql;

namespace Starcounter
{
    /// <summary>
    /// Public interface for a non-generic enumerator of the result of an SQL query.
    /// </summary>
    public interface ISqlEnumerator : IEnumerator, IDisposable
    {
        /// <summary>
        /// Moves to the next of the resulting objects of the query.
        /// </summary>
        /// <returns>True if there is a next object, otherwise false.</returns>
        new Boolean MoveNext();

        /// <summary>
        /// Gets the current object (IObjectView) in the result of the query.
        /// </summary>
        new dynamic Current { get; }

        /// <summary>
        /// Resets the result by setting the cursor at the position before the first object.
        /// </summary>
        new void Reset();

        /// <summary>
        /// Releases unmanaged resources.
        /// </summary>
        new void Dispose();

        /// <summary>
        /// The SQL query this SQL enumerator executes.
        /// </summary>
        String Query { get; }

        /// <summary>
        /// The type binding of the resulting objects of the query.
        /// </summary>
        ITypeBinding TypeBinding { get; }

        /// <summary>
        /// Counts the number of returned objects.
        /// </summary>
        Int64 Counter { get; }

        /// <summary>
        /// Gets offset key of the SQL enumerator if it is possible.
        /// </summary>
        Byte[] GetOffsetKey();
    }
}
