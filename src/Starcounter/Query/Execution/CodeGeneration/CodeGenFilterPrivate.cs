// ***********************************************************************
// <copyright file="CodeGenFilterPrivate.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;
using Starcounter.Query.Sql;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Starcounter.Binding;

namespace Starcounter.Query.Execution
{
// Simple helper class for the instructions array.
internal sealed class CodeGenFilterInstrArray
{
    UInt32[] instrArr = null; // Represents an instruction array.
    Int32 count = -1; // Number of elements.

    // Constructor.
    public CodeGenFilterInstrArray(UInt32 maxElements)
    {
        instrArr = new UInt32[maxElements];
        count = 0;
    }

    // Cloning constructor.
    public CodeGenFilterInstrArray(UInt32[] instrArr, Int32 count)
    {
        this.instrArr = instrArr;
        this.count = count;
    }
    
    // Clone the filter instruction array.
    public CodeGenFilterInstrArray Clone()
    {
        return new CodeGenFilterInstrArray((UInt32[]) instrArr.Clone(), count);
    }

    // Appends instruction to the end of the array.
    public void Add(UInt32 instrCode)
    {
        instrArr[count] = instrCode;
        count++;
    }

    // Retrieves a reference to the instruction array.
    public UInt32[] GetArrayRef
    {
        get
        {
            return instrArr;
        }
    }

    // Retrieves a number of instructions loaded into array.
    public Int32 Count
    {
        get
        {
            return count;
        }
    }
}

// Private filter is not shared among several SqlResults.
internal sealed class CodeGenFilterPrivate
{
    List<CodeGenFilterNode> dataLeaves = null; // Represents all filter tree data leaves.
    CodeGenFilterCacheShared filterCache = null; // Reference to shared filter cache.
    FilterKeyBuilder filterKey = null; // Data stream representation of the filter.
    Boolean numTypeChanged = false; // Indicates if instruction codes changed as a reflection of variable type change.
    UInt64 filterHandle = 0; // Handle to recently used filter.
    ILogicalExpression queryCondition = null; // Condition tree.
    RowTypeBinding typeBinding = null; // Result type binding.
    DbTypeCode[] numVarTypes = null; // Array of variable types.
    Int32 extentNumber = -1; // Extent number.
    Boolean printFilterOutput = false; // Print debug info?
    
    // Constructor for initial copy and cloning.
    public CodeGenFilterPrivate(ILogicalExpression queryCond,
                                RowTypeBinding rowTypeBinding,
                                Int32 extentNum, // Indicates an extent to which this condition tree belongs.
                                DbTypeCode[] recentNumVarTypes,
                                CodeGenFilterCacheShared sharedFilterCache,
                                UInt64 recentFilterHandle)
    {
        queryCondition = queryCond;
        typeBinding = rowTypeBinding;
        extentNumber = extentNum;
        numVarTypes = recentNumVarTypes;
        filterCache = sharedFilterCache;
        filterHandle = recentFilterHandle;
        filterKey = new FilterKeyBuilder();

        // Filling out data leaves from query condition tree traversal.
        // (also connecting numerical variables with data leaves).
        dataLeaves = new List<CodeGenFilterNode>();
        TraverseConditionTree(dataLeaves, null, "Traversing condition tree at Cloning time");
        if (dataLeaves.Count <= 0)
        {
            // No variables found.
            dataLeaves = null;
        }
    }

    public CodeGenFilterPrivate Clone(ILogicalExpression conditionClone,
                                      RowTypeBinding typeBindingClone)
    {
        // Creating clone of numerical variable types.
        DbTypeCode[] newNumVarTypes = null;
        if (numVarTypes != null)
        {
            newNumVarTypes = (DbTypeCode[]) numVarTypes.Clone();
        }

        // Finally creating a new copy.
        return new CodeGenFilterPrivate(conditionClone,
                                        typeBindingClone,
                                        extentNumber,
                                        newNumVarTypes,
                                        filterCache, // Filter cache is shared.
                                        filterHandle // Recently used filter handle.
                                        );
    }

    // Returns maximum stack size for the filter.
    UInt32 TraverseConditionTree(List<CodeGenFilterNode> dataLeavesRef,
                                 CodeGenFilterInstrArray instrArrayRef,
                                 String prefix)
    {
        // Testing new filter generator.
        StringBuilder filterText = null;
        if (printFilterOutput)
        {
            filterText = new StringBuilder(prefix + ":\n");
        }

        // Calling a recursive operation on queryCondition tree to create
        // managed representation of the filter.
        UInt32 maxStackSize = queryCondition.AppendToInstrAndLeavesList(dataLeavesRef, instrArrayRef, extentNumber, filterText);

        // Checking if stack size is supported.
        if (maxStackSize > CodeGenFilterInstrCodes.FILTER_MAX_STACK_SIZE)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Stack size for the filter is larger than supported.");
        }

        // If data leaves are null, then they should be filled and attached already.
        if (dataLeavesRef == null)
        {
            if (printFilterOutput)
                Console.WriteLine(filterText);

            return maxStackSize;
        }
        
        // Checking if number of values is supported.
        if (dataLeavesRef.Count > CodeGenFilterInstrCodes.FILTER_MAX_VAR_COUNT)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "There are more values for the filter than supported.");
        }

        if (printFilterOutput)
        {
            filterText.Append("Maximum stack size: " + maxStackSize + "\n");
            filterText.Append("Data leaves: " + dataLeavesRef.Count + "\n\n");
            Console.WriteLine(filterText);
        }

        // Determining how many numerical variables are in the query.
        Int32 numVarCount = 0;

        // Either initial creation or already have some numerical variables.
        if ((filterHandle == 0) || (numVarTypes != null))
        {
            foreach (CodeGenFilterNode dataLeaf in dataLeavesRef)
            {
                NumericalVariable varRef = dataLeaf as NumericalVariable;
                if (varRef != null)
                {
                    varRef.AttachPrivateFilter(this, numVarCount);
                    numVarCount++;
                }
            }
        }

        // Allocating numerical variable types array and connecting it with this filter.
        if ((numVarTypes == null) && (numVarCount > 0)) // Indicates initial private filter creation.
        {
            // Since situation can happen only on initial creation.
            numVarTypes = new DbTypeCode[numVarCount];

            // Filling out numerical variable types array.
            foreach (CodeGenFilterNode dataLeaf in dataLeavesRef)
            {
                NumericalVariable varRef = dataLeaf as NumericalVariable;
                if (varRef != null)
                {
                    varRef.VariableTypeChanged();
                }
            }
        }

        return maxStackSize;
    }
    
    // Getting filter handle (newly created or cached).
    public UInt64 GetFilterHandle()
    {
        // Checking if numerical variable types have been changed (or no numerical variables).
        if ((!HasNumTypeChanged) && (filterHandle != 0))
        {
            // None of the instructions have been changed
            // so we can use existing filter handle.
            return filterHandle;
        }

        // We need either to fetch existing filter from cache
        // or create a filter from scratch.
        filterHandle = filterCache.GetFilterIfCached(numVarTypes);

        // There is no corresponding filter cached.
        if (filterHandle == 0)
        {
            // We already know maximum number of instructions which is supported.
            CodeGenFilterInstrArray instrArray = new CodeGenFilterInstrArray(CodeGenFilterInstrCodes.FILTER_MAX_INSTR_COUNT);

            // Traversing through the tree and filling out instructions array.
            // (we have already have fetched and processed data leaves array in constructor).
            UInt32 maxStackSize = TraverseConditionTree(null, instrArray, "Traversing condition tree at Execution time");

            // There is no filter with this instructions array that was cached.
            // Creating filter from scratch and adding it to the cache.
            filterHandle = filterCache.CreateFilter(instrArray, numVarTypes, maxStackSize, DataCount, typeBinding, extentNumber);
        }
        return filterHandle;
    }

    // Getting data stream.
    public Byte[] GetDataStream(Row obj)
    {
        if (dataLeaves != null)
        {
            foreach (CodeGenFilterNode dataLeaf in dataLeaves)
            {
                dataLeaf.AppendToByteArray(filterKey, obj);
            }
        }

        return filterKey.GetBufferCached();
    }

    // Interface to indicate that instruction has been changed.
    public Boolean HasNumTypeChanged
    {
        get
        {
            // Checking if instruction codes have been changed.
            if (numTypeChanged)
            {
                numTypeChanged = false;
                return true;
            }
            return false;
        }
    }

    // Callback, called from numerical variable instance if numerical type changes.
    public void NumericalVariableTypeChanged(Int32 numVarIndex, DbTypeCode newType)
    {
        numVarTypes[numVarIndex] = newType;
        numTypeChanged = true;
    }

    // Reseting this filter after fetching it from cache.
    public void ResetCached()
    {
        filterKey.ResetCached();
    }

    // Calculating number of data leaves.
    public UInt32 DataCount
    {
        get
        {
            if (dataLeaves != null)
            {
                return (UInt32) dataLeaves.Count;
            }
            return 0;
        }
    }
}
}