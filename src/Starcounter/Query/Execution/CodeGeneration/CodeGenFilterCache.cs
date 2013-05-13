// ***********************************************************************
// <copyright file="CodeGenFilterCache.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;
using Starcounter.Query.Sql;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Starcounter.Internal;
using Starcounter.Binding;

namespace Starcounter.Query.Execution
{
// Represents previously created filter.
// Filter cache is a list of these entries.
internal sealed class CodeGenFilterCacheEntry
{
    DbTypeCode[] numVarTypes = null; // Array with numerical variables data types.
    UInt64 filterHandle = 0; // Handle to generated filter.

    // Constructor.
    public CodeGenFilterCacheEntry(DbTypeCode[] varTypes, UInt64 filterHandle)
    {
        this.filterHandle = filterHandle;
        this.numVarTypes = varTypes;
    }

    // Retrieving filter handle.
    public UInt64 FilterHandle
    {
        get
        {
            return filterHandle;
        }
    }
    
    /// <summary>
    /// Disposes corresponding filter.
    /// </summary>
    public void Dispose()
    {
        unsafe
        {
            CodeGenFilterNativeInterface.release_filter(filterHandle);
        }
        filterHandle = 0;
        numVarTypes = null;
    }

    /// <summary>
    /// Checks if two filters are the same (by checking numerical variable types).
    /// </summary>
    /// <param name="newNumVarTypes">Variables type array to compare with.</param>
    /// <returns>Filter handle if filters are the same, 0 otherwise.</returns>
    public UInt64 CompareAndGetFilter(DbTypeCode[] newNumVarTypes)
    {
        // Running throw every variable type.
        for (Int32 i = 0; i < newNumVarTypes.Length; i++)
        {
            if (numVarTypes[i] != newNumVarTypes[i])
            {
                // Different variable types found.
                return 0;
            }
        }

        // This filter can be re-used from cache.
        return filterHandle;
    }
}

// Represents a bunch previously created filters.
// Shared filter cache is shared among several SqlResults.
internal sealed class CodeGenFilterCacheShared
{
    // Represents all previously cached filters.
    CodeGenFilterCacheEntry[] cachedFilters = null;
    UInt32 cachedCount = 0; // Number of cached filters.

    // Constructor.
    public CodeGenFilterCacheShared(UInt32 maximumFilters)
    {
        cachedFilters = new CodeGenFilterCacheEntry[maximumFilters];
    }

    /// <summary>
    /// Add created filter to the cache.
    /// </summary>
    public void AddNewFilter(DbTypeCode[] varTypes, UInt64 filterHandle)
    {
        CodeGenFilterCacheEntry newCacheEntry = new CodeGenFilterCacheEntry(varTypes, filterHandle);
        // Exception will be triggered when filter cache is overflowed.
        cachedFilters[cachedCount] = newCacheEntry;
        cachedCount++;
    }

    /// <summary>
    /// Getting previously generated code filter if it exist.
    /// </summary>
    public UInt64 GetFilterIfCached(DbTypeCode[] numVarTypes)
    {
        // Checking if there are any numerical variables.
        if (numVarTypes == null)
        {
            if (cachedCount == 1)
            {
                return cachedFilters[0].FilterHandle;
            }
            return 0;
        }
        
        // Iterating through all filters being cached previously.
        for (Int32 i = 0; i < cachedCount; i++)
        {
            UInt64 filterHandle = cachedFilters[i].CompareAndGetFilter(numVarTypes);
            if (filterHandle != 0) return filterHandle;
        }

        // Indicating that there is no cached filter
        // with specified variable types found.
        return 0;
    }

    /// <summary>
    /// Disposes all cached filters.
    /// </summary>
    public void Dispose()
    {
        // Iterating through all filters being cached previously.
        for (Int32 i = 0; i < cachedCount; i++)
        {
            cachedFilters[i].Dispose();
        }
        cachedCount = 0;
        cachedFilters = null;
    }

    /// <summary>
    /// Creating filter using instructions, maximum stack size, number of data values, etc.
    /// Automatically adds new filter to the cache.
    /// </summary>
    /// <param name="instrArray">Array of filter instructions.</param>
    /// <param name="varTypes">Array with numerical variable types (null if no numerical variables).</param>
    /// <param name="maxStackSize">The maximum size of the stack needed for filter input values.</param>
    /// <param name="dataCount">Number of supplied data values.</param>
    /// <param name="typeBinding">Result type binding for the corresponding extent.</param>
    /// <param name="extentNumber">ID of the corresponding extent.</param>
    /// <returns>Handle to the created filter or exception on error.</returns>
    public UInt64 CreateFilter(CodeGenFilterInstrArray instrArray,
                               DbTypeCode[] varTypes,
                               UInt32 maxStackSize,
                               UInt32 dataCount,
                               RowTypeBinding typeBinding,
                               Int32 extentNumber)
    {
        // Checking that all needed parameters are initialized properly.
        if (instrArray == null ||
            instrArray.Count <= 0 ||
            maxStackSize == 0 ||
            extentNumber < 0 ||
            typeBinding == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "CreateFilter: incorrect input parameters.");
        }

        // Getting type binding for the class.
        TypeBinding tb = (typeBinding.GetTypeBinding(extentNumber) as TypeBinding);
        if (tb == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "CreateFilter: can't find type binding for the underlying type.");
        }

        UInt32 errorCode, instrCount = (UInt32) instrArray.Count;
        UInt64 newFilterHandle = 0;
        unsafe
        {
            fixed (UInt32* instrPointer = instrArray.GetArrayRef)
            {
                // Using native interface to create a filter.
                errorCode = CodeGenFilterNativeInterface.create_filter(tb.TableId,
                                                                       maxStackSize,
                                                                       dataCount,
                                                                       instrCount,
                                                                       instrPointer,
                                                                       &newFilterHandle);
            }
        }
        
        // Translating error occurred during filter creation.
        if (errorCode != 0)
        {
            throw ErrorCode.ToException(errorCode);
        }
        
        // Adding filter to the cache.
        AddNewFilter(varTypes, newFilterHandle);
        
        // Also returning the filter handle, if needed.
        return newFilterHandle;
    }
}

}