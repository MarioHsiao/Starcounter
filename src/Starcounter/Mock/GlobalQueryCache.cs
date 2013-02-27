// ***********************************************************************
// <copyright file="GlobalQueryCache.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using se.sics.prologbeans;
using Starcounter.Query.Sql;
using System.Text;
using System.Runtime.InteropServices;
using Starcounter.Query.Execution;
using System.Collections.Generic;
using Starcounter;
using System.Threading;

internal sealed class GlobalQueryCache
{
    /// <summary>
    /// Maximum number of unique queries in cache.
    /// </summary>
    private const Int32 DefaultUniqueQueries = 8192;
    //public const Int32 MaxUniqueQueries = 8192;

    // Array containing all unique execution enumerators.
    IExecutionEnumerator[] enumArray = new IExecutionEnumerator[DefaultUniqueQueries];

    // Number of unique cached queries.
    Int32 numUniqueQueries = 0;

    // Dictionary containing indexes into enumerator array.
    Dictionary<String, Int32> indexDict = new Dictionary<String, Int32>();

    internal readonly ulong Generation; 

    internal GlobalQueryCache(ulong generation)
    {
        Generation = generation;
    }

    internal int EnumArrayLength { get { return enumArray.Length; } }

    /// <summary>
    /// Adds a new query to the cache if its already not there.
    /// Mutually exclusive.
    /// </summary>
    internal Int32 AddNewQuery(String query)
    {
        Starcounter.ThreadHelper.SetYieldBlock();
        try
        {
            // Mutually excluding.
            lock (indexDict)
            {
                // First trying to fetch enumerator.
                Int32 enumIndex = GetEnumIndex(query);
                if (enumIndex >= 0)
                {
                    return enumIndex;
                }

                // Query is not cached, adding it.
                // Parser and optimize it
                // Creating enumerator from scratch.
                IExecutionEnumerator newEnum = Starcounter.Query.QueryPreparation.PrepareQuery(query);

                // Assigning unique query ID.
                newEnum.UniqueQueryID = (UInt64)numUniqueQueries;
                enumIndex = numUniqueQueries;

                // Increasing number of unique queries.
                numUniqueQueries++;

                // Checking if its LikeExecEnumerator.
                if (newEnum is LikeExecEnumerator)
                {
                    (newEnum as LikeExecEnumerator).CreateLikeCombinations();
                }

                // Adding to the linear array.
                try
                {
                    enumArray[enumIndex] = newEnum;
                }
                catch (IndexOutOfRangeException)
                {
                    var newEnumArray = new IExecutionEnumerator[enumArray.Length * 2];
                    enumArray.CopyTo(newEnumArray, 0);
                    newEnumArray[enumIndex] = newEnum;
                    Thread.MemoryBarrier();
                    enumArray = newEnumArray;
                }

                // Add to array before dictionary.
                Thread.MemoryBarrier();

                // Adding to dictionary.
                indexDict.Add(query, enumIndex);

                return enumIndex;
            }
        }
        finally
        {
            Starcounter.ThreadHelper.ReleaseYieldBlock();
        }
    }

    /// <summary>
    /// Fetches enumerator index if its cached.
    /// Otherwise returns negative number.
    /// </summary>
    internal Int32 GetEnumIndex(String query)
    {
        // Trying to fetch the enumerator index.
        Int32 enumIndex = -1;
        if (indexDict.TryGetValue(query, out enumIndex))
        {
            return enumIndex;
        }

        // On fail return incorrect index.
        return -1;
    }

    /// <summary>
    /// Returns true if indicated enumerator exists in cache.
    /// </summary>
    internal Boolean EnumExists(Int32 index)
    {
        // Checking if query is cached at all.
        if (enumArray[index] != null)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Returns a clone of SQL enumerator specified by index.
    /// Returns NULL if query is not cached yet.
    /// </summary>
    internal IExecutionEnumerator GetEnumClone(Int32 index)
    {
        IExecutionEnumerator execEnum = enumArray[index];

        // Checking if query is cached at all.
        if (execEnum == null)
        {
            return null;
        }

        // Returning cached enumerator clone.
        return execEnum.CloneCached();
    }

    /// <summary>
    /// Fetches cached enumerator query string.
    /// </summary>
    internal String GetQueryString(Int32 index)
    {
        IExecutionEnumerator execEnum = enumArray[index];

        // Checking if query is cached at all.
        if (execEnum == null)
        {
            return null;
        }

        // Returning cached enumerator query string.
        return execEnum.QueryString;
    }

    /// <summary>
    /// Logs the current status of server SQL query cache.
    /// </summary>
    internal String SQLCacheStatus()
    {
        String cacheStatus = "Server SQL query cache status: Cached unique queries = " + numUniqueQueries + Environment.NewLine;
        for (Int32 i = 0; i < numUniqueQueries; i++)
        {
            cacheStatus += i + ": \"" + enumArray[i].QueryString + "\"" + Environment.NewLine;
        }
        return cacheStatus;
    }
}