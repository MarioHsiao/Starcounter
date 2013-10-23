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
        /// Gets the current item (row) in the result of the query.
        /// </summary>
        new dynamic Current { get; }

        /// <summary>
        /// The SQL query this SQL enumerator executes.
        /// </summary>
        String Query { get; }

        /// <summary>
        /// If the projection is an (Entity or Row) object, then the type binding of that object, otherwise null.
        /// </summary>
        ITypeBinding TypeBinding { get; }

        /// <summary>
        /// If the projection is a singleton, then the DbTypeCode of that singleton, otherwise null.
        /// </summary>
        Nullable<DbTypeCode> ProjectionTypeCode { get; }

        /// <summary>
        /// If the project is a singleton, the the property of the singleton, otherwise null.
        /// </summary>
        IPropertyBinding PropertyBinding { get; }

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
