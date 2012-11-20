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
//    /// <summary>
//    /// 
//    /// </summary>
//    public class SqlResult<T> : IEnumerable
//    {
//        UInt64 transactionId; // The handle of the transaction to which this SQL result belongs.
//        String query; // SQL query string.
//        Boolean slowSQL; // Describes if queries with slow executions are allowed or not.
//        Object[] sqlParams; // SQL query parameters, all given at once.

//        // Creating SQL result with query parameters all given at once.
//        internal SqlResult(UInt64 transactionId, String query, Boolean slowSQL, params Object[] sqlParamsValues) 
//        {
//            this.transactionId = transactionId;
//            this.query = query;
//            this.slowSQL = slowSQL;
//            sqlParams = sqlParamsValues;
//        }

//        /// <summary>
//        /// The first item in the result, if there is one, otherwise null.
//        /// </summary>
//        /// <value></value>
//        public dynamic First
//        {
//            get
//            {
//                IExecutionEnumerator execEnum = null;
//                dynamic current = null;

//                try
//                {
//                    execEnum = GetEnumerator() as IExecutionEnumerator;

//                    // Check if the query includes anything non-supported.
//                    if (execEnum.QueryFlags != QueryFlags.None && !slowSQL)
//                    {
//                        if ((execEnum.QueryFlags & QueryFlags.IncludesLiteral) != QueryFlags.None)
//                            throw new SqlException("Literal in query is not supported. Use variable and parameter instead.");

//                        if ((execEnum.QueryFlags & QueryFlags.IncludesAggregation) != QueryFlags.None)
//                            throw new SqlException("Aggregation in query is not supported.");
//                    }

//                    current = null;

//                    // Setting the fetch first only flag.
//                    execEnum.SetFirstOnlyFlag();

//                    if (execEnum.MoveNext())
//                        current = execEnum.Current;
//                }
//                finally
//                {
//                    if (execEnum != null)
//                        execEnum.Dispose();
//                }

//                return current;
//            }
//        }

//        /// <summary>
//        /// Getting enumerator for manual use.
//        /// </summary>
//        /// <returns></returns>
//        public ISqlEnumerator GetEnumerator()
//        {
//#if true // TODO EOH2: Lucent objects.
//            IExecutionEnumerator execEnum = null;
//            execEnum = Scheduler.GetInstance().SqlEnumCache.GetCachedEnumerator(query);
//#else
//            // Obtaining enumerator from query cache or creating it from scratch.
//            IExecutionEnumerator execEnum = null;
//            Boolean isRunningOnClient = ApplicationBackend.Current.BackendKind != ApplicationBackend.Kind.Database;

//            // Obtaining either server side or client side enumerator.
//            if (isRunningOnClient)
//                execEnum = LucentObjectsRuntime.ClientQueryCache.GetCachedEnumerator(query);
//            else
//                execEnum = Scheduler.GetInstance().SqlEnumCache.GetCachedEnumerator(query);
//                //execEnum = Scheduler.GetInstance().ClientExecEnumCache.GetCachedEnumerator(query);
//#endif

//            // Check if the query includes anything non-supported.
//            if (execEnum.QueryFlags != QueryFlags.None && !slowSQL)
//            {
//                if ((execEnum.QueryFlags & QueryFlags.IncludesLiteral) != QueryFlags.None)
//                    throw new SqlException("Literal in query is not supported. Use variable and parameter instead.");

//                if ((execEnum.QueryFlags & QueryFlags.IncludesAggregation) != QueryFlags.None)
//                    throw new SqlException("Aggregation in query is not supported.");
//            }

//            // Setting SQL parameters if any are given.
//            if (sqlParams != null)
//                execEnum.SetVariables(sqlParams);

//            // Prolonging transaction handle.
//            execEnum.TransactionId = transactionId;

//            return new GenericEnumerator<T>(execEnum);
//        }

//        // Implementing the IEnumerable.GetEnumerator() method.
//        IEnumerator IEnumerable.GetEnumerator()
//        {
//            return GetEnumerator();
//        }
//    }

    /// <summary>
    /// 
    /// </summary>
    public class SqlResult<T> : SqlResult, IEnumerable<T>
    {
        // Creating SQL result with query parameters all given at once.
        internal SqlResult(UInt64 transactionId, String query, Boolean slowSQL, params Object[] sqlParamsValues)
            : base(transactionId, query, slowSQL, sqlParamsValues)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        override public ISqlEnumerator GetEnumerator()
        {
            return new GenericEnumerator<T>(base.GetEnumerator() as IExecutionEnumerator);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new GenericEnumerator<T>(base.GetEnumerator() as IExecutionEnumerator);
        }
    }
}
