#if false
// ***********************************************************************
// <copyright file="ClientQueryCache.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;

using Starcounter;
using Starcounter.Query.Sql;
using Starcounter.Query.Execution;
using System.Threading;
using Starcounter.Internal;

namespace Starcounter.Query.Sql
{
    /// <summary>
    /// This cache is used for storing already processed SQL enumerators on client.
    /// </summary>
    public sealed class ClientQueryCache
    {
        // Main dictionary enumerator cache. Flushed when full.
        readonly Dictionary<String, LinkedListNode<LinkedList<IExecutionEnumerator>>> enumListDict = null;

        // Last usage time sorted enumerator list. Flushed when full.
        readonly LinkedList<LinkedList<IExecutionEnumerator>> enumListListSorted = null;

        // Current number of cached enumerators in dictionary.
        Int32 totalCachedEnum = 0;

        // Maximum tries for fast search in sorted cached enumerators.
        const Int32 MaxFastLookups = 8;

        // Number of unique queries that were processed for this virtual processor (not necessarily cached amount).
        UInt64 numUniqueQueries = 0;

        // Maximum cache size is the parameter.
        internal ClientQueryCache()
        {
            enumListDict = new Dictionary<String, LinkedListNode<LinkedList<IExecutionEnumerator>>>();
            enumListListSorted = new LinkedList<LinkedList<IExecutionEnumerator>>();
        }

        /// <summary>
        /// Logs the current status of server SQL query cache.
        /// </summary>
        internal void SQLCacheStatus()
        {
            Console.WriteLine("Client SQL query cache status: totally cached: {0}, unique queries: {1}.", totalCachedEnum, numUniqueQueries);
        }

        /// <summary>
        /// Gets an already existing enumerator corresponding to the query from the cache or creates a new one.
        /// </summary>
        internal IExecutionEnumerator GetCachedEnumerator(String query)
        {
            // NOTE: "Remove last, Add last, Clone first" convention is used.
            // Three possible cases:
            // 1. Dictionary entry does not exist with this query.
            // 2. Dictionary entry exists but contains only one element because all enumerators are taken and occupied.
            // 3. Dictionary entry contains one or more cached enumerators.

            IExecutionEnumerator execEnum = null; // What will be returned.
            LinkedListNode<LinkedList<IExecutionEnumerator>> enumListListNode = enumListListSorted.Last;

            // Before making a complete search for the query - try sorted list for a possible hit.
            Int32 fastLookups = MaxFastLookups;
            LinkedList<IExecutionEnumerator> tempEnumList = null;

            while ((enumListListNode != null) && (fastLookups > 0)) // Iterating in a reversed way.
            {
                tempEnumList = enumListListNode.Value;
                execEnum = tempEnumList.Last.Value; // Assuming that we will have many cached enumerators.

                if (execEnum.Query == query) // Same query for the cached enumerator.
                {
                    Int32 cachedEnumCount = tempEnumList.Count;
                    if (cachedEnumCount == 1) // Cloning enumerator if only one left.
                    {
                        // Always using first cached enumerator for cloning (because of dynamic ranges).
                        execEnum = execEnum.CloneCached();
                        totalCachedEnum++; // We have added new enumerator.
                    }
                    else
                    {
                        // More than one free cached enumerator is in the list.
                        tempEnumList.RemoveLast(); // Remember that we always removing the last element.
                    }

                    // Updating the rating in any of the case when enumerator was cached before.
                    if (enumListListSorted.Last != enumListListNode)
                    {
                        // Move the current enumerator list to the end of the popularity list.
                        enumListListSorted.Remove(enumListListNode);
                        enumListListSorted.AddLast(enumListListNode);
                    }
                    break;
                }

                // Didn't find any correct execution enumerator yet, trying other...
                enumListListNode = enumListListNode.Previous;
                fastLookups--;
                execEnum = null;
            }

            // Enumerator was not found during initial lookups.
            if (execEnum == null) // We didn't have any cache fast hit.
            {
                // Hopefully we still have some cached queries in the dictionary.
                if ((enumListListNode != null) && (enumListDict.TryGetValue(query, out enumListListNode) == true))
                {
                    // Enumerator has been previously cached in database.
                    tempEnumList = enumListListNode.Value;
                    Int32 cachedEnumCount = tempEnumList.Count;
                    execEnum = tempEnumList.Last.Value; // Assuming that we will have many.
                    if (cachedEnumCount == 1)
                    {
                        // Always using first cached enumerator for cloning (because of dynamic ranges).
                        // The first one is the last one here and vice versa:).
                        execEnum = execEnum.CloneCached();
                        totalCachedEnum++; // We have added new enumerator.
                    }
                    else
                    {
                        // More than one enumerator is in the list.
                        tempEnumList.RemoveLast(); // Remember that we always removing the last element.
                    }

                    // Updating the rating in any of the case when enumerator was cached before.
                    if (enumListListSorted.Last != enumListListNode)
                    {
                        // Move the current enumerator list to the end of the popularity list.
                        enumListListSorted.Remove(enumListListNode);
                        enumListListSorted.AddLast(enumListListNode);
                    }
                }
                // No cached enumerator found at all - obtaining unique query ID from server.
                else
                {
                    // Sending SQL query for processing and receiving back the unique query ID.
                    UInt64 uniqueQueryID = 0;

                    // Asking the server for unique query ID (also creates the server-side enumerator).
                    UInt32 errCode = 1;
                    UInt32 queryFlags = 0;
                    unsafe
                    {
                        errCode = SqlConnectivityInterface.SqlConn_GetQueryUniqueId(
                            query,
                            &uniqueQueryID,
                            &queryFlags);
                    }

                    // Checking important flags.
                    if ((queryFlags & SqlConnectivityInterface.FLAG_HAS_PROJECTION) != 0)
                    {
                        throw ErrorCode.ToException(
                            Error.SCERRSINGLEOBJECTPROJECTION,
                            (msg, ex) => new NotImplementedException(msg, ex)
                        );
                    }

                    // Checking that everything went fine.
                    if (errCode != 0)
                        SqlConnectivity.ThrowConvertedServerError(errCode);

                    // Creating an instance of client Sql enumerator.
                    execEnum = new UserSqlEnumerator(query, uniqueQueryID, new VariableArray(0), this, queryFlags, null);

                    // Giving the cache where all subsequent enumerators should be returned.
                    LinkedList<IExecutionEnumerator> newEnumList = new LinkedList<IExecutionEnumerator>();
                    execEnum.AttachToCache(newEnumList);

                    // Returning the original enumerator back to cache.
                    ((ExecutionEnumerator)execEnum).ReturnToCache();

                    // Creating the clone of enumerator.
                    execEnum = execEnum.CloneCached();

                    // Creating node with enumerator list identified by query and adding it to cache dictionary.
                    enumListListNode = new LinkedListNode<LinkedList<IExecutionEnumerator>>(newEnumList);
                    enumListDict.Add(query, enumListListNode);

                    // We also need to add this new node to popularity list at the end.
                    enumListListSorted.AddLast(enumListListNode);

                    // We have added two new enumerators.
                    totalCachedEnum += 2;

                    // One more unique query processed.
                    numUniqueQueries++;
                }
            }

            // Finally returning fetched execution enumerator.
            return execEnum;
        }
    }
}
#endif
