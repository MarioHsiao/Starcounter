﻿// ***********************************************************************
// <copyright file="ExecutionEnumerator.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Binding;
using Starcounter.Internal;
using Starcounter.Query.Optimization;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;

namespace Starcounter.Query.Execution
{
// Implementation for base execution enumerator class.
internal abstract class ExecutionEnumerator
{
    protected CompositeObject currentObject = null; // Represents latest successfully retrieved object.
    protected VariableArray variableArray = null; // Array with variables from query.
    protected Int64 counter = 0; // Number of successful hits (retrieved objects).
    protected String query = null; // Original SQL query which this enumerator belongs to.
    protected CompositeTypeBinding compTypeBinding = null; // Type binding for the enumerator.
    protected Nullable<DbTypeCode> projectionTypeCode = null; // If singleton projection, then the DbTypeCode of that singleton, otherwise null.
    protected UInt64 uniqueQueryID = 0; // Uniquely identifies query it belongs to.
    protected String uniqueGenName = null; // Uniquely identifies the scan during code generation.
    protected Boolean hasCodeGeneration = false; // Indicates if code generation is done for this enumerator.

    protected INumericalExpression fetchNumberExpr = null; // Represents fetch literal or variable.
    protected Int64 fetchNumber = Int64.MaxValue; // Maximum fetch number.
    protected IBinaryExpression fetchOffsetKeyExpr = null; // Represents offset key literal or variable.
    protected Boolean innermostExtent = false; // True, if this execution-enumerator represents the innermost extent, otherwise false.

    // Cache-related properties.
    protected LinkedList<IExecutionEnumerator> enumCacheListFrom = null; // From which cache list this enumerator came from.
    protected LinkedListNode<IExecutionEnumerator> enumListNode = null; // Node with this execution enumerator.

    /// <summary>
    /// Default constructor.
    /// </summary>
    internal ExecutionEnumerator(CompositeTypeBinding compTypeBind, VariableArray varArray)
    {
        if (compTypeBind == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect compTypeBind.");
        if (varArray == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect variables clone.");

        compTypeBinding = compTypeBind;
        variableArray = varArray;

        if (varArray != null && (varArray.QueryFlags & QueryFlags.SingletonProjection) != 0)
        {
            projectionTypeCode = compTypeBinding.GetPropertyBinding(0).TypeCode;
        }
    }

    /// <summary>
    /// Sets the transaction handle value.
    /// </summary>
    public UInt64 TransactionId
    {
        set { variableArray.TransactionId = value; }
    }

    /// <summary>
    /// Uniquely identifies the corresponding query.
    /// </summary>
    public UInt64 UniqueQueryID
    {
        get
        {
            return uniqueQueryID;
        }

        set
        {
            uniqueQueryID = value;
        }
    }

    /// <summary>
    /// Just returns reference to variable array.
    /// </summary>
    public VariableArray VarArray
    {
        get
        {
            return variableArray;
        }
    }

    /// <summary>
    /// Indicates if code generation is possible for this enumerator.
    /// </summary>
    public Boolean HasCodeGeneration
    {
        get
        {
            return hasCodeGeneration;
        }

        set
        {
            hasCodeGeneration = value;
        }
    }

    /// <summary>
    /// Gets the number of successful hits.
    /// </summary>
    public Int64 Counter
    {
        get
        {
            return counter;
        }
    }

    /// <summary>
    /// The SQL query this SQL enumerator executes.
    /// </summary>
    public String Query
    {
        get
        {
            return query;
        }
    }

    /// <summary>
    /// The SQL query this SQL enumerator executes.
    /// </summary>
    public String QueryString
    {
        get
        {
            return query;
        }
    }

    /// <summary>
    /// Gets the type binding of the composite object.
    /// </summary>
    public virtual CompositeTypeBinding CompositeTypeBinding
    {
        get
        {
            return compTypeBinding;
        }
    }

    /// <summary>
    /// If the projection is a singleton, then the DbTypeCode of that singleton, otherwise null.
    /// </summary>
    public Nullable<DbTypeCode> ProjectionTypeCode
    {
        get
        {
            return projectionTypeCode;
        }
    }

    /// <summary>
    /// Gets the number of variables in the SQL query.
    /// </summary>
    public Int32 VariableCount
    {
        get
        {
            return variableArray.Length;
        }
    }

    /// <summary>
    /// Returns the flags describing the query.
    /// </summary>
    public QueryFlags QueryFlags
    {
        get
        {
            return variableArray.QueryFlags;
        }
    }

    /// <summary>
    /// Sets the fetch first only flag.
    /// </summary>
    public virtual void SetFirstOnlyFlag()
    {
        // Do nothing.
    }

    /// <summary>
    /// Depending on query flags, populates the flags value.
    /// </summary>
    public unsafe virtual void PopulateQueryFlags(UInt32 * flags)
    {
        if ((QueryFlags & QueryFlags.IncludesSorting) != 0)
        {
            * flags |= SqlConnectivityInterface.FLAG_HAS_SORTING;
        }

        if ((QueryFlags & QueryFlags.IncludesAggregation) != 0)
        {
            * flags |= SqlConnectivityInterface.FLAG_HAS_AGGREGATION;
        }

        if ((QueryFlags & QueryFlags.IncludesFetchVariable) != 0)
        {
            * flags |= SqlConnectivityInterface.FLAG_FETCH_VARIABLE;
        }

        if ((QueryFlags & QueryFlags.IncludesOffsetKeyVariable) != 0)
        {
            * flags |= SqlConnectivityInterface.FLAG_RECREATION_KEY_VARIABLE;
        }

        if ((QueryFlags & QueryFlags.SingletonProjection) == 0)
        {
            * flags |= SqlConnectivityInterface.FLAG_HAS_PROJECTION;
        }
    }

    /// <summary>
    /// Populates query information such as fetch literal/variable info and recreation key info.
    /// </summary>
    public unsafe UInt32 GetInfo(
        Byte infoType,
        UInt64 param,
        Byte *results,
        UInt32 maxBytes,
        UInt32 *outLenBytes)
    {
        switch (infoType)
        {
            case SqlConnectivityInterface.GET_FETCH_VARIABLE:
            {
                (*(UInt32 *)results) = (UInt32)((fetchNumberExpr as Variable).Number);
                *outLenBytes = 4;
                break;
            }

            case SqlConnectivityInterface.GET_RECREATION_KEY_VARIABLE:
            {
                (*(UInt32 *)results) = (UInt32)((fetchOffsetKeyExpr as Variable).Number);
                *outLenBytes = 4;
                break;
            }
        }
        return 0;
    }

    /// <summary>
    /// Gets offset key if its possible.
    /// </summary>
    public virtual Byte[] GetOffsetKey()
    {
        // Checking if any object was fetched.
        if (currentObject == null)
            return null;

        Int32 globalOffset = 0;
        IExecutionEnumerator execEnum = this as IExecutionEnumerator;

        // Getting the amount of leaves in execution tree.
        Int32 leavesNum = execEnum.CompositeTypeBinding.ExtentOrder.Count;
        globalOffset = ((leavesNum << 3) + IteratorHelper.RK_HEADER_LEN);

        // Using cache temp buffer.
        Byte[] tempBuffer = Scheduler.GetInstance().SqlEnumCache.TempBuffer;

        unsafe
        {
            fixed (Byte* recreationKey = tempBuffer)
            {
                // Saving number of enumerators.
                (*(Int32*)(recreationKey + IteratorHelper.RK_ENUM_NUM_OFFSET)) = leavesNum;

                // Saving static data (or obtaining absolute position of the first dynamic data).
                globalOffset = execEnum.SaveEnumerator(recreationKey, globalOffset, false);

                // Saving dynamic data.
                globalOffset = execEnum.SaveEnumerator(recreationKey, globalOffset, true);

                // Saving full recreation key length.
                (*(Int32*)recreationKey) = globalOffset;

                // Successfully recreated the key.
                if (globalOffset > IteratorHelper.RK_EMPTY_LEN)
                {
                    // Allocating space for offset key.
                    Byte[] offsetKey = new Byte[globalOffset];

                    // Copying the recreation key into provided user buffer.
                    Buffer.BlockCopy(tempBuffer, 0, offsetKey, 0, globalOffset);

                    // Returning the key.
                    return offsetKey;
                }
            }
        }

        // Was not able to fetch the key.
        return null;
    }

    /// <summary>
    /// Enumerator reset functionality.
    /// </summary>
    public abstract void Reset(CompositeObject obj);

    /// <summary>
    /// Resets the enumerator.
    /// </summary>
    public void Reset()
    {
        // Resetting the transaction identifier.
        //variableArray.TransactionId = Transaction.Current.TransactionId;

        // Calling underlying enumerator reset.
        Reset(null);
    }

    /// <summary>
    /// Returns the enumerator back to the cache.
    /// </summary>
    internal virtual void ReturnToCache()
    {
        // Returning this enumerator back to the cache.
        enumCacheListFrom.AddLast(enumListNode);
    }

    /// <summary>
    /// Disposes the enumerator and returns it to the cache.
    /// </summary>
    public void Dispose()
    {
        Reset(null);
        ReturnToCache();
    }

    /// <summary>
    /// Should be called when attached to a cache.
    /// </summary>
    /// <param name="fromCache">Cache where this enumerator should be returned.</param>
    public void AttachToCache(LinkedList<IExecutionEnumerator> fromCache)
    {
        // Attaching to the specified cache.
        enumCacheListFrom = fromCache;

        // Creating cache node from this execution enumerator.
        enumListNode = new LinkedListNode<IExecutionEnumerator>((IExecutionEnumerator) this);
    }

    // We need to refer to the main clone method.
    public abstract IExecutionEnumerator Clone(CompositeTypeBinding typeBindingClone, VariableArray varArrClone);

    /// <summary>
    /// Creating the clone of enumerator.
    /// </summary>
    /// <returns></returns>
    public IExecutionEnumerator CloneCached()
    {
        // Re-creating the variable array with the certain variables number.
        VariableArray varArrayClone = new VariableArray(variableArray.Length);

        // Pass on the QueryFlags to varArrayClone.
        varArrayClone.QueryFlags = variableArray.QueryFlags;

        // Calling main clone method of related execution enumerator.
        CompositeTypeBinding compTypeBindingClone = null;
        if (compTypeBinding != null)
        {
            compTypeBindingClone = compTypeBinding.Clone(varArrayClone);
        }

        IExecutionEnumerator newExecEnum = Clone(compTypeBindingClone, varArrayClone);

        // Transferring unique query ID.
        newExecEnum.UniqueQueryID = uniqueQueryID;

        // Attaching new enumerator to existing cache.
        newExecEnum.AttachToCache(enumCacheListFrom);

        return newExecEnum;
    }

    /// <summary>
    /// Does the continuous object properties fill up into the dedicated buffer.
    /// </summary>
    /// <param name="results">The results.</param>
    /// <param name="resultsMaxBytes">The results max bytes.</param>
    /// <param name="resultsNum">The results num.</param>
    /// <param name="flags">The flags.</param>
    /// <returns>UInt32.</returns>
    public virtual unsafe UInt32 FillupFoundObjectIDs(Byte * results, UInt32 resultsMaxBytes, UInt32 * resultsNum, UInt32 * flags)
    {
        UInt64 slotsNum = 0;
        UInt32 hitsCount = 0;
        UInt64 *slotsBuf = ((UInt64 *) results) + 1;
        UInt32 bytesWritten = 8;

        // Checking how many hits we can process.
        UInt32 maxSlotsNum = resultsMaxBytes / 24;
        slotsNum = *resultsNum;

        // Determining maximum slots number.
        if ((slotsNum <= 0) || (maxSlotsNum < slotsNum))
        {
            slotsNum = maxSlotsNum;
        }

        // Taking this object as ISqlEnumerator.
        IExecutionEnumerator thisEnum = this as IExecutionEnumerator;

        // Checking if we need to recreate the enumerator and move to the last position.
        if (variableArray.RecreationKeyData != null)
        {
            //Application.Profiler.Start("Recreation MoveNext", 4);
            thisEnum.MoveNext();
            //Application.Profiler.Stop(4);

            // Resetting the recreation key data.
            variableArray.RecreationKeyData = null;
        }

        // Just filling up the array.
        //Application.Profiler.Start("Cycled MoveNext", 5);
        while ((slotsNum > 0) && (thisEnum.MoveNext()))
        {
            Entity dbObject = thisEnum.Current as Entity;

            // Checking if object is not null.
            if (dbObject != null)
            {
                slotsBuf[0] = dbObject.ThisRef.ETI;
                slotsBuf[1] = dbObject.ThisRef.ObjectID;
                slotsBuf[2] = dbObject.TableId;
            }
            else
            {
                slotsBuf[0] = 0;
                slotsBuf[1] = 0;
                slotsBuf[2] = 0;
            }

            slotsBuf += 3;
            bytesWritten += 24;

            slotsNum--;
            hitsCount++;
        }
        //Application.Profiler.Stop(5);

        // Checking if more objects exist.
        if (slotsNum <= 0)
        {
            *flags |= SqlConnectivityInterface.FLAG_MORE_RESULTS;
        }

        // Copying the number of copied object infos.
        * resultsNum = hitsCount;

        // Setting the length in bytes.
        ( *(UInt32 *)results) = bytesWritten;

        return 0;
    }

    // Concrete build string method should be defined in every execution enumerator.
    public abstract void BuildString(MyStringBuilder stringBuilder, Int32 tabs);

    /// <summary>
    /// Returns a string presentation of the execution enumerator including
    /// a specification of the type of the returned objects and the execution plan.
    /// </summary>
    /// <returns>A string presentation of the execution enumerator.</returns>
    public override String ToString()
    {
        MyStringBuilder stringBuilder = new MyStringBuilder();

        if (compTypeBinding != null)
        {
            compTypeBinding.BuildString(stringBuilder, 0);
        }

        BuildString(stringBuilder, 0);
        return stringBuilder.ToString();
    }

    /// <summary>
    /// Initializes all query variables from given buffer.
    /// </summary>
    /// <param name="queryParamsBuf">Byte array with data for all variables.</param>
    public unsafe void InitVariablesFromBuffer(Byte *queryParamsBuf)
    {
        if (variableArray.Length > 0)
        {
            variableArray.InitFromBuffer(queryParamsBuf);
        }
    }

    /// <summary>
    /// Sets a value to an SQL variable.
    /// </summary>
    /// <param name="index">The order number of the variable starting at 0.</param>
    /// <param name="value">The new value of the variable.</param>
    public virtual void SetVariable(Int32 index, SByte value)
    {
        variableArray.GetElement(index).SetValue(value);
    }
    public virtual void SetVariable(Int32 index, Int16 value)
    {
        variableArray.GetElement(index).SetValue(value);
    }
    public virtual void SetVariable(Int32 index, Int32 value)
    {
        variableArray.GetElement(index).SetValue(value);
    }
    public virtual void SetVariable(Int32 index, Int64 value)
    {
        variableArray.GetElement(index).SetValue(value);
    }
    public virtual void SetVariable(Int32 index, Byte value)
    {
        variableArray.GetElement(index).SetValue(value);
    }
    public virtual void SetVariable(Int32 index, UInt16 value)
    {
        variableArray.GetElement(index).SetValue(value);
    }
    public virtual void SetVariable(Int32 index, UInt32 value)
    {
        variableArray.GetElement(index).SetValue(value);
    }
    public virtual void SetVariable(Int32 index, UInt64 value)
    {
        variableArray.GetElement(index).SetValue(value);
    }
    public virtual void SetVariable(Int32 index, String value)
    {
        variableArray.GetElement(index).SetValue(value);
    }
    public virtual void SetVariable(Int32 index, Decimal value)
    {
        variableArray.GetElement(index).SetValue(value);
    }
    public virtual void SetVariable(Int32 index, Double value)
    {
        variableArray.GetElement(index).SetValue(value);
    }
    public virtual void SetVariable(Int32 index, Single value)
    {
        variableArray.GetElement(index).SetValue(value);
    }
    public virtual void SetVariable(Int32 index, IObjectView value)
    {
        variableArray.GetElement(index).SetValue(value);
    }
    public virtual void SetVariable(Int32 index, Boolean value)
    {
        variableArray.GetElement(index).SetValue(value);
    }
    public virtual void SetVariable(Int32 index, DateTime value)
    {
        variableArray.GetElement(index).SetValue(value);
    }
    public virtual void SetVariable(Int32 index, Binary value)
    {
        variableArray.GetElement(index).SetValue(value);
    }
    public virtual void SetVariable(Int32 index, Byte[] value)
    {
        variableArray.GetElement(index).SetValue(value);
    }
    public virtual void SetVariable(Int32 index, Object value)
    {
        variableArray.GetElement(index).SetValue(value);
    }

    public virtual void SetVariableToNull(Int32 index)
    {
        // Special case when value of variable is null.
        variableArray.GetElement(index).SetNullValue();
    }

    /// <summary>
    /// Sets values to all SQL variables in the SQL query.
    /// </summary>
    /// <param name="sqlParams">The SQL params.</param>
    /// <exception cref="System.ArgumentException">Incorrect number of SQL parameters, which should be:</exception>
    public virtual void SetVariables(Object[] sqlParams)
    {
        Int32 numVariables = variableArray.Length;

        if (numVariables != sqlParams.Length)
        {
            throw new ArgumentException("Incorrect number of SQL parameters, which should be: " + numVariables);
        }

        // Running throw all variables in the array.
        for (Int32 i = 0; i < sqlParams.Length; i++)
        {
            if (sqlParams[i] != null)
            {
                variableArray.GetElement(i).SetValue(sqlParams[i]);
            }
            else
            {
                variableArray.GetElement(i).SetNullValue();
            }
        }
    }
}
}