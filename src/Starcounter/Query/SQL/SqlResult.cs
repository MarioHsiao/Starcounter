// ***********************************************************************
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
    /// Class SqlResult
    /// </summary>
    public class SqlResult : IEnumerable
    {
        UInt64 transactionId; // The handle of the transaction to which this SQL result belongs.
        String query; // SQL query string.
        Object[] sqlParams; // SQL query parameters, all given at once.
        Boolean slowSQL; // Describes if queries with slow executions are allowed or not.

        // Creating SQL result with query parameters all given at once.
        internal SqlResult(UInt64 transactionId, String query, Boolean slowSQL, params Object[] sqlParamsValues)
        {
            this.transactionId = transactionId;
            this.query = query;
            this.slowSQL = slowSQL;
            sqlParams = sqlParamsValues;
        }

        // Obtaining only the first hit/result and disposing the enumerator.
        /// <summary>
        /// Gets the first.
        /// </summary>
        /// <value>The first.</value>
        /// <exception cref="Starcounter.SqlException">Literal in query is not supported. Use variable and parameter instead.</exception>
        public dynamic First
        {
            get
            {
                IExecutionEnumerator execEnum = null;
                IObjectView current = null;

                try
                {
                    execEnum = GetEnumerator() as IExecutionEnumerator;

                    // Check if the query includes anything non-supported.
                    if (execEnum.QueryFlags != QueryFlags.None && !slowSQL)
                    {
                        if ((execEnum.QueryFlags & QueryFlags.IncludesLiteral) != QueryFlags.None)
                            throw new SqlException("Literal in query is not supported. Use variable and parameter instead.");

                        if ((execEnum.QueryFlags & QueryFlags.IncludesAggregation) != QueryFlags.None)
                            throw new SqlException("Aggregation in query is not supported.");
                    }

                    current = null;

                    // Setting the fetch first only flag.
                    execEnum.SetFirstOnlyFlag();

                    if (execEnum.MoveNext())
                        current = execEnum.Current;
                }
                finally
                {
                    if (execEnum != null)
                        execEnum.Dispose();
                }

                return current;
            }
        }

        // Getting enumerator for manual use.
        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        /// <returns>ISqlEnumerator.</returns>
        /// <exception cref="Starcounter.SqlException">Literal in query is not supported. Use variable and parameter instead.</exception>
        public ISqlEnumerator GetEnumerator()
        {
#if true // TODO EOH2: Lucent objects.
            IExecutionEnumerator execEnum = null;
            execEnum = Scheduler.GetInstance().SqlEnumCache.GetCachedEnumerator(query);
#else
            // Obtaining enumerator from query cache or creating it from scratch.
            IExecutionEnumerator execEnum = null;
            Boolean isRunningOnClient = ApplicationBackend.Current.BackendKind != ApplicationBackend.Kind.Database;

            // Obtaining either server side or client side enumerator.
            if (isRunningOnClient)
                execEnum = LucentObjectsRuntime.ClientQueryCache.GetCachedEnumerator(query);
            else
                execEnum = Scheduler.GetInstance().SqlEnumCache.GetCachedEnumerator(query);
                //execEnum = Scheduler.GetInstance().ClientExecEnumCache.GetCachedEnumerator(query);
#endif

            // Check if the query includes anything non-supported.
            if (execEnum.QueryFlags != QueryFlags.None && !slowSQL)
            {
                if ((execEnum.QueryFlags & QueryFlags.IncludesLiteral) != QueryFlags.None)
                    throw new SqlException("Literal in query is not supported. Use variable and parameter instead.");

                if ((execEnum.QueryFlags & QueryFlags.IncludesAggregation) != QueryFlags.None)
                    throw new SqlException("Aggregation in query is not supported.");
            }

            // Setting SQL parameters if any are given.
            if (sqlParams != null)
                execEnum.SetVariables(sqlParams);

            // Prolonging transaction handle.
            execEnum.TransactionId = transactionId;

            return execEnum;
        }

        // Implementing the IEnumerable.GetEnumerator() method.
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
