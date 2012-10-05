using System;
using System.Collections.Generic;

using Starcounter;
using Starcounter.Internal;
using Starcounter.Query.Sql;
using Sc.Server.Binding;
using Sc.Server.Internal;
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
    LinkedListNode<LinkedList<IExecutionEnumerator>>[] enumArray = new LinkedListNode<LinkedList<IExecutionEnumerator>>[GlobalQueryCache.MaxUniqueQueries];

    // Total number of cached enumerators in this cache.
    Int32 totalCachedEnum = 0;

    // Index of the last used enumerator.
    Int32 lastUsedEnumIndex = 0;

    // Just a temporary buffer.
    internal Byte[] TempBuffer = new Byte[SqlConnectivityInterface.RECREATION_KEY_MAX_BYTES];

    // References to global cache to be able run query during invalidation period
    GlobalQueryCache globalQueryCache = Scheduler.GlobalCache;

    /// <summary>
    /// If true, then this local cache has to be invalidated. It is set on through Scheduler.
    /// </summary>
    Boolean toInvalidate = false;

    /// <summary>
    /// Gets an already existing enumerator given the unique query ID.
    /// </summary>
    internal IExecutionEnumerator GetCachedEnumerator(Int32 uniqueQueryId)
    {
        IExecutionEnumerator execEnum = null;

        // Getting the enumerator list.
        LinkedListNode<LinkedList<IExecutionEnumerator>> enumListListNode = enumArray[uniqueQueryId];
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

    /// <summary>
    /// Resets local variable to empty cache. Resets global cache references to
    /// current global cache values (i.e., after invalidation of global reset).
    /// This method assumed is not to be called concurrently.
    /// </summary>
    public void InvalidateCache()
    {
        if (toInvalidate)
        {
            lock (Scheduler.InvalidateLock)
            {
                globalQueryCache = Scheduler.GlobalCache;
                toInvalidate = false;
            }
            enumArray = new LinkedListNode<LinkedList<IExecutionEnumerator>>[GlobalQueryCache.MaxUniqueQueries];
            totalCachedEnum = 0;
            lastUsedEnumIndex = 0;
        }
    }

    internal void InvalidateCache(bool force)
    {
        toInvalidate = true;
        InvalidateCache();
    }

    internal void SetToInvalidate()
    {
        toInvalidate = true;
    }
}
}
