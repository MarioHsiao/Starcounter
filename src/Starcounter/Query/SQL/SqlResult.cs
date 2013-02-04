﻿// ***********************************************************************
// <copyright file="SqlResult.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections;
using System.Diagnostics;
using Starcounter.LucentObjects;
using Starcounter.Query.Execution;
using Sc.Query.Execution;

namespace Starcounter
{
    /// <summary>
    /// 
    /// </summary>
    public class SqlResult : IEnumerable {
        /// <summary>
        /// 
        /// </summary>
        protected UInt64 transactionId; // The handle of the transaction to which this SQL result belongs.
        
        /// <summary>
        /// 
        /// </summary>
        protected String query; // SQL query string.
        
        /// <summary>
        /// 
        /// </summary>
        protected Object[] sqlParams; // SQL query parameters, all given at once.
        
        /// <summary>
        /// 
        /// </summary>
        protected Boolean slowSQL; // Describes if queries with slow executions are allowed or not.

        // Creating SQL result with query parameters all given at once.
        internal SqlResult(UInt64 transactionId, String query, Boolean slowSQL, params Object[] sqlParamsValues) {
            this.transactionId = transactionId;
            this.query = query;
            this.slowSQL = slowSQL;
            sqlParams = sqlParamsValues;
        }

        /// <summary>
        /// Obtaining only the first hit/result and disposing the enumerator.
        /// </summary>
        /// <value></value>
        public dynamic First {
            get {
                IExecutionEnumerator execEnum = null;
                dynamic current = null;

                try {
                    execEnum = GetExecutionEnumerator();

                    current = null;

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
        public SqlEnumerator GetEnumerator() {
            // Note that error handling here prevents this method from being
            // inline by caller.

            var e = GetExecutionEnumerator();
            try {
                return new SqlEnumerator(e);
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
    }
}
