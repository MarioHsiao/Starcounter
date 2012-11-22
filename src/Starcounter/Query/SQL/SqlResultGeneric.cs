// ***********************************************************************
// <copyright file="SqlResultGeneric.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Starcounter.LucentObjects;
using Starcounter.Query.Execution;
using Sc.Query.Execution;

namespace Starcounter
{

    /// <summary>
    /// 
    /// </summary>
    public class SqlResult<T> : SqlResult, IEnumerable<T> {
        // Creating SQL result with query parameters all given at once.
        internal SqlResult(UInt64 transactionId, String query, Boolean slowSQL, params Object[] sqlParamsValues)
            : base(transactionId, query, slowSQL, sqlParamsValues) {
        }

        // We hide the base First property to return an instance of T instead
        // of a dynamic in case the property is accessed from generic SqlResult
        // instance in order to not polute the calling code with dynamic code
        // when you explicitly specify the type.

        /// <summary>
        /// Obtaining only the first hit/result and disposing the enumerator.
        /// </summary>
        /// <value></value>
        new public T First {
            get {
                // Note that we needed to copy the code because if simply
                // calling base.First the CLR refused to inline this method.

                IExecutionEnumerator execEnum = null;
                dynamic current = null;

                try {
                    execEnum = GetExecutionEnumerator();

                    current = default(T);

                    // Setting the fetch first only flag.
                    execEnum.SetFirstOnlyFlag();

                    if (execEnum.MoveNext())
                        current = execEnum.Current;
                }
                finally {
                    if (execEnum != null) execEnum.Dispose();
                }

                return current;
            }
        }

        // Getting enumerator for manual use.
        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        /// <returns>SqlEnumerator.</returns>
        /// <exception cref="Starcounter.SqlException">Literal in query is not supported. Use variable and parameter instead.</exception>
        new public SqlEnumerator<T> GetEnumerator() {
            // Note that error handling here prevents this method from being
            // inline by caller.

            var e = GetExecutionEnumerator();
            try {
                return new SqlEnumerator<T>(e);
            }
            catch {
                e.Dispose();
                throw;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        IEnumerator<T> IEnumerable<T>.GetEnumerator() {
            return GetEnumerator();
        }
    }
}
