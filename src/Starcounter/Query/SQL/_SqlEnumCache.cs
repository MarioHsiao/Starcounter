// ***********************************************************************
// <copyright file="_SqlEnumCache.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;

using Starcounter;
using Starcounter.Internal;
using Starcounter.Query.Sql;
using Starcounter.Query.Execution;

namespace Starcounter.Query.Sql
{
/// <summary>
/// This cache is used for storing SQL enumerators corresponding to queries.
/// This cache shared between threads of one virtual processor.
/// </summary>
public sealed class SqlEnumCache
{
    // Used for fast access to needed enumerator with unique query ID. Never flushed, only extends.
    LinkedListNode<LinkedList<IExecutionEnumerator>>[] enumArray;

    // Total number of cached enumerators in this cache.
    Int32 totalCachedEnum = 0;

    // Index of the last used enumerator.
    Int32 lastUsedEnumIndex = 0;

    // Just a temporary buffer.
    internal Byte[] TempBuffer = new Byte[SqlConnectivityInterface.RECREATION_KEY_MAX_BYTES];

    // References to global cache to be able run query during invalidation period
    GlobalQueryCache globalQueryCache = Scheduler.GlobalCache;

    internal SqlEnumCache()
    {
        enumArray = new LinkedListNode<LinkedList<IExecutionEnumerator>>[globalQueryCache.EnumArrayLength];
    }

    /// <summary>
    /// Gets an already existing enumerator given the unique query ID.
    /// </summary>
    internal IExecutionEnumerator GetCachedEnumerator(Int32 uniqueQueryId)
    {
        IExecutionEnumerator execEnum = null;

        // Getting the enumerator list.
        LinkedListNode<LinkedList<IExecutionEnumerator>> enumListListNode;
        try
        {
            enumListListNode = enumArray[uniqueQueryId];
        }
        catch (IndexOutOfRangeException)
        {
            var newEnumArrayLength = globalQueryCache.EnumArrayLength;
            var newEnumArray = new LinkedListNode<LinkedList<IExecutionEnumerator>>[newEnumArrayLength];
            enumArray.CopyTo(newEnumArray, 0);
            enumArray = newEnumArray;
            enumListListNode = null;
        }
        if (enumListListNode != null)
        {
            // Getting enumerator list inside the node.
            LinkedList<IExecutionEnumerator> enumList = enumListListNode.Value;

            // Checking if there are any enumerators in the list.
            if (enumList.Count == 0)
            {
                // Always using first cached enumerator for cloning (because of dynamic ranges).
                execEnum = globalQueryCache.GetEnumClone(uniqueQueryId);

                // Increasing the number of enumerators.
                totalCachedEnum++;

                // Giving the cache where all subsequent enumerators should be returned.
                execEnum.AttachToCache(enumList);
            }
            else
            {
                // Cutting last enumerator.
                execEnum = enumList.Last.Value;
                enumList.RemoveLast();
            }
        }
        else
        {
            // Fetching existing enumerator from the global cache.
            execEnum = globalQueryCache.GetEnumClone(uniqueQueryId);

            // Increasing the number of enumerators
            totalCachedEnum++;

            // Creating new list for enumerators of the same query.
            LinkedList<IExecutionEnumerator> newEnumList = new LinkedList<IExecutionEnumerator>();

            // Creating node with enumerator list identified by query and adding it to cache dictionary.
            enumListListNode = new LinkedListNode<LinkedList<IExecutionEnumerator>>(newEnumList);

            // Adding new enumerator to the array.
            enumArray[uniqueQueryId] = enumListListNode;

            // Giving the cache where all subsequent enumerators should be returned.
            execEnum.AttachToCache(newEnumList);
        }

        // Adding to the sorting list.
        lastUsedEnumIndex = uniqueQueryId;

        return execEnum;
    }

    /// <summary>
    /// Gets an already existing enumerator corresponding to the query from the cache or creates a new one.
    /// </summary>
    internal IExecutionEnumerator GetCachedEnumerator(String query)
    {
        // Trying last used enumerator.
        if (query == globalQueryCache.GetQueryString(lastUsedEnumIndex))
        {
            return GetCachedEnumerator(lastUsedEnumIndex);
        }

        // We have to ask dictionary for the index.
        Int32 enumIndex = globalQueryCache.GetEnumIndex(query);

        // Checking if its completely new query.
        if (enumIndex < 0)
        {
            enumIndex = globalQueryCache.AddNewQuery(query);
            if (totalCachedEnum == 0) // Cache was reset
                enumIndex = globalQueryCache.AddNewQuery(query);
        }

        // Fetching existing enumerator using index.
        return GetCachedEnumerator(enumIndex);
    }

    /// <summary>
    /// Logs the current status of server SQL query cache.
    /// </summary>
    internal String SQLCacheStatus()
    {
        return String.Format("Server SQL query cache status: Totally amount of enumerators = {0}.", totalCachedEnum);
    }

    internal void InvalidateCache(GlobalQueryCache globalQueryCache)
    {
        this.globalQueryCache = globalQueryCache;
        enumArray = new LinkedListNode<LinkedList<IExecutionEnumerator>>[globalQueryCache.EnumArrayLength];
        totalCachedEnum = 0;
        lastUsedEnumIndex = 0;
    }
}
}
