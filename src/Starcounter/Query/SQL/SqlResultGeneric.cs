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
    public class SqlResult<T> : Rows<T>, IEnumerable, IEnumerable<T> {



        protected UInt64 transactionId; // The handle of the transaction to which this SQL result belongs.
        protected String query; // SQL query string.
        protected Object[] sqlParams; // SQL query parameters, all given at once.
        protected Boolean slowSQL; // Describes if queries with slow executions are allowed or not.

        // Creating SQL result with query parameters all given at once.
        internal SqlResult(UInt64 transactionId, String query, Boolean slowSQL, params Object[] sqlParamsValues) {
            this.transactionId = transactionId;
            this.query = query;
            this.slowSQL = slowSQL;
            sqlParams = sqlParamsValues;
        }



        // Getting enumerator for manual use.
        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        /// <returns>SqlEnumerator.</returns>
        /// <exception cref="Starcounter.SqlException">Literal in query is not supported. Use variable and parameter instead.</exception>
        public override IRowEnumerator<T> GetEnumerator() {
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


        // Implementing the IEnumerable.GetEnumerator() method.
        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        internal IExecutionEnumerator GetExecutionEnumerator() {
            IExecutionEnumerator execEnum = null;
#if true // TODO: Lucent objects.
#else
            // Obtaining enumerator from query cache or creating it from scratch.
            Boolean isRunningOnClient = ApplicationBackend.Current.BackendKind != ApplicationBackend.Kind.Database;

            // Obtaining either server side or client side enumerator.
            if (isRunningOnClient)
                execEnum = LucentObjectsRuntime.ClientQueryCache.GetCachedEnumerator(query);
            else
                execEnum = Scheduler.GetInstance().SqlEnumCache.GetCachedEnumerator(query);
                //execEnum = Scheduler.GetInstance().ClientExecEnumCache.GetCachedEnumerator(query);
#endif
            try {
                execEnum = Scheduler.GetInstance().SqlEnumCache.GetCachedEnumerator(query);

                // Check if the query includes anything non-supported.
                if (execEnum.QueryFlags != QueryFlags.None && !slowSQL) {
                    if ((execEnum.QueryFlags & QueryFlags.IncludesAggregation) != QueryFlags.None)
                        throw new SqlException("Aggregation in query is not supported.");

                    if ((execEnum.QueryFlags & QueryFlags.IncludesLiteral) != QueryFlags.None)
                        throw new SqlException("Literal in query is not supported. Use variable and parameter instead.");
                }

                // Setting SQL parameters if any are given.
                if (sqlParams != null)
                    execEnum.SetVariables(sqlParams);

                // Prolonging transaction handle.
                execEnum.TransactionId = transactionId;

                return execEnum;
            }
            catch {
                if (execEnum != null) execEnum.Dispose();
                throw;
            }
        }



        // We hide the base First property to return an instance of T instead
        // of a dynamic in case the property is accessed from generic SqlResult
        // instance in order to not polute the calling code with dynamic code
        // when you explicitly specify the type.

        /// <summary>
        /// Obtaining only the first hit/result and disposing the enumerator.
        /// </summary>
        /// <value></value>
        public override T First {
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


//        public override IRowEnumerator GetEnumerator() {
//            return (IRowEnumerator)GetEnumerator();
//        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        IEnumerator<T> IEnumerable<T>.GetEnumerator() {
            return (IEnumerator<T>)GetEnumerator();
        }



    }
}
