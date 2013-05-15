// ***********************************************************************
// <copyright file="FullTableScan.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;
using Starcounter.Query.Sql;
using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
//using Starcounter.Management.Win32;
using Sc.Query.Execution;
using Starcounter.Internal;
using Starcounter.Binding;
using System.Diagnostics;

namespace Starcounter.Query.Execution
{
internal class FullTableScan : ExecutionEnumerator, IExecutionEnumerator
{
    Int32 extentNumber = -1; // To which table this enumerator belongs to.
    UInt64 indexHandle = 0; // Handle to the related index.
    IteratorHelper iterHelper; // Stores cached iterator helper.
    IndexInfo indexInfo = null; // Information about index.

    ILogicalExpression condition = null; // Condition tree for the query.
    Boolean descending = false, // Sorting: Ascending or Descending.
       enumeratorCreated = false; // True after execution enumerator is created.
       //dataStreamChanged = false; // True if data stream has changed.

    FilterEnumerator enumerator = null; // Handle to execution enumerator.
    Row contextObject = null; // This object comes from the outer loop in joins.

    CodeGenFilterPrivate privateFilter = null; // Filter code generator instance.
    Byte[] filterDataStream = null; // Points to the created data stream.
    UInt64 filterHandle = 0; // Contains latest filter handle in use.

    // First and second key defining the full range.
    ByteArrayBuilder firstKeyBuilder = null,
           secondKeyBuilder = null;

    Byte[] firstKeyBuffer = null,
           secondKeyBuffer = null;

    UInt64 keyOID, keyETI; // Saved OID, ETI from recreation key.
    Boolean enableRecreateObjectCheck = false; // Enables check for deleted object during enumerator recreation.
    Boolean triedEnumeratorRecreation = false; // Indicates if we should try enumerator recreation with supplied key.

    Boolean stayAtOffsetkey = false;
    public Boolean StayAtOffsetkey { get { return stayAtOffsetkey; } set { stayAtOffsetkey = value; } }
    Boolean useOffsetkey = true;
    public Boolean UseOffsetkey { get { return useOffsetkey; } set { useOffsetkey = value; } }
    Boolean isAtRecreatedKey = false;
    public Boolean IsAtRecreatedKey { get { return isAtRecreatedKey; } }

    internal FullTableScan(byte nodeId, 
        RowTypeBinding rowTypeBind,
        Int32 extentNum, IndexInfo indexInfo,
        ILogicalExpression queryCond,
        SortOrder sortingType,
        INumericalExpression fetchNumberExpr,
        INumericalExpression fetchOffsetExpr, 
        IBinaryExpression fetchOffsetKeyExpr,
        Boolean innermostExtent, 
        CodeGenFilterPrivate privFilter,
        VariableArray varArr, String query, Boolean topNode)
        : base(nodeId, EnumeratorNodeType.FullTableScan, rowTypeBind, varArr, topNode)
    {
        if (rowTypeBind == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect rowTypeBind.");
        if (varArr == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect varArr.");
        if (queryCond == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect queryCond.");

        extentNumber = extentNum;
        indexHandle = indexInfo.Handle;
        this.indexInfo = indexInfo;
        condition = queryCond;

        descending = (sortingType == SortOrder.Descending);

        this.fetchNumberExpr = fetchNumberExpr;
        this.fetchOffsetExpr = fetchOffsetExpr;
        this.fetchOffsetKeyExpr = fetchOffsetKeyExpr;

        this.innermostExtent = innermostExtent;

        this.query = query;

        iterHelper = IteratorHelper.GetIndex(indexHandle); // Caching index handle.

        // Creating empty enumerator at caching time (without any managed post privateFilter).
        enumerator = new FilterEnumerator(0, 0);

        // Checking if private filter has already been created for us.
        if (privateFilter == null)
        {
            // Query has not been cached before, so we need
            // to create a private filter and shared filter cache.
            privateFilter = new CodeGenFilterPrivate(condition,
                rowTypeBinding,
                extentNumber,
                null, // Current numerical variable types should be determined at execution time.
                new CodeGenFilterCacheShared(4), // Maximum 4 filters can be cached per query.
                0);
        }
        else
        {
            privateFilter = privFilter;
        }

        // Creating full range intervals.
        CreateFullRangeKeys();
    }

    /// <summary>
    /// Creates keys for full range scan.
    /// </summary>
    void CreateFullRangeKeys()
    {
        firstKeyBuilder = new ByteArrayBuilder();
        secondKeyBuilder = new ByteArrayBuilder();

        // Going through all indexes in the combined index.
        for (Int32 i = 0; i < indexInfo.AttributeCount; i++)
        {
            switch (indexInfo.GetTypeCode(i))
            {
                case DbTypeCode.Binary:
                {
                    if (indexInfo.GetSortOrdering(i) == SortOrder.Descending)
                    {
                        firstKeyBuilder.Append(BinaryRangeValue.MAX_VALUE);
                        secondKeyBuilder.Append(BinaryRangeValue.MIN_VALUE);
                    }
                    else
                    {
                        firstKeyBuilder.Append(BinaryRangeValue.MIN_VALUE);
                        secondKeyBuilder.Append(BinaryRangeValue.MAX_VALUE);
                    }
                    break;
                }

                case DbTypeCode.Boolean:
                {
                    if (indexInfo.GetSortOrdering(i) == SortOrder.Descending)
                    {
                        firstKeyBuilder.Append(BooleanRangeValue.MAX_VALUE);
                        secondKeyBuilder.Append(BooleanRangeValue.MIN_VALUE);
                    }
                    else
                    {
                        firstKeyBuilder.Append(BooleanRangeValue.MIN_VALUE);
                        secondKeyBuilder.Append(BooleanRangeValue.MAX_VALUE);
                    }
                    break;
                }

                case DbTypeCode.DateTime:
                {
                    if (indexInfo.GetSortOrdering(i) == SortOrder.Descending)
                    {
                        firstKeyBuilder.Append(DateTimeRangeValue.MAX_VALUE);
                        secondKeyBuilder.Append(DateTimeRangeValue.MIN_VALUE);
                    }
                    else
                    {
                        firstKeyBuilder.Append(DateTimeRangeValue.MIN_VALUE);
                        secondKeyBuilder.Append(DateTimeRangeValue.MAX_VALUE);
                    }
                    break;
                }

                case DbTypeCode.Decimal:
                {
                    if (indexInfo.GetSortOrdering(i) == SortOrder.Descending)
                    {
                        firstKeyBuilder.Append(DecimalRangeValue.MAX_VALUE);
                        secondKeyBuilder.Append(DecimalRangeValue.MIN_VALUE);
                    }
                    else
                    {
                        firstKeyBuilder.Append(DecimalRangeValue.MIN_VALUE);
                        secondKeyBuilder.Append(DecimalRangeValue.MAX_VALUE);
                    }
                    break;
                }

                case DbTypeCode.Int64:
                case DbTypeCode.Int32:
                case DbTypeCode.Int16:
                case DbTypeCode.SByte:
                {
                    if (indexInfo.GetSortOrdering(i) == SortOrder.Descending)
                    {
                        firstKeyBuilder.Append(IntegerRangeValue.MAX_VALUE);
                        secondKeyBuilder.Append(IntegerRangeValue.MIN_VALUE);
                    }
                    else
                    {
                        firstKeyBuilder.Append(IntegerRangeValue.MIN_VALUE);
                        secondKeyBuilder.Append(IntegerRangeValue.MAX_VALUE);
                    }
                    break;
                }

                case DbTypeCode.UInt64:
                case DbTypeCode.UInt32:
                case DbTypeCode.UInt16:
                case DbTypeCode.Byte:
                {
                    if (indexInfo.GetSortOrdering(i) == SortOrder.Descending)
                    {
                        firstKeyBuilder.Append(UIntegerRangeValue.MAX_VALUE);
                        secondKeyBuilder.Append(UIntegerRangeValue.MIN_VALUE);
                    }
                    else
                    {
                        firstKeyBuilder.Append(UIntegerRangeValue.MIN_VALUE);
                        secondKeyBuilder.Append(UIntegerRangeValue.MAX_VALUE);
                    }
                    break;
                }

                case DbTypeCode.Object:
                {
                    if (indexInfo.GetSortOrdering(i) == SortOrder.Descending)
                    {
                        firstKeyBuilder.Append(ObjectRangeValue.MAX_VALUE);
                        secondKeyBuilder.Append(ObjectRangeValue.MIN_VALUE);
                    }
                    else
                    {
                        firstKeyBuilder.Append(ObjectRangeValue.MIN_VALUE);
                        secondKeyBuilder.Append(ObjectRangeValue.MAX_VALUE);
                    }
                    break;
                }

                case DbTypeCode.String:
                {
                    if (indexInfo.GetSortOrdering(i) == SortOrder.Descending)
                    {
                        firstKeyBuilder.Append(StringRangeValue.MAX_VALUE, true);
                        secondKeyBuilder.Append(StringRangeValue.MIN_VALUE, false);
                    }
                    else
                    {
                        firstKeyBuilder.Append(StringRangeValue.MIN_VALUE, false);
                        secondKeyBuilder.Append(StringRangeValue.MAX_VALUE, true);
                    }
                    break;
                }

                case DbTypeCode.Key: {
                    if (indexInfo.GetSortOrdering(i) == SortOrder.Descending) {
                        firstKeyBuilder.Append(UIntegerRangeValue.MAX_VALUE);
                        secondKeyBuilder.Append(UIntegerRangeValue.MIN_VALUE);
                    }
                    else {
                        firstKeyBuilder.Append(UIntegerRangeValue.MIN_VALUE);
                        secondKeyBuilder.Append(UIntegerRangeValue.MAX_VALUE);
                    }
                    break;
                }

                default:
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect typeCode.");
            }
        }

        // Fetching the created buffers.
        firstKeyBuffer = firstKeyBuilder.GetBufferCached();
        secondKeyBuffer = secondKeyBuilder.GetBufferCached();
    }

    /// <summary>
    /// The type binding of the resulting objects of the query.
    /// </summary>
    public ITypeBinding TypeBinding
    {
        get
        {
            if (projectionTypeCode == null)
                return rowTypeBinding;

            // Singleton object.
            if (projectionTypeCode == DbTypeCode.Object)
                return rowTypeBinding.GetPropertyBinding(0).TypeBinding;

            // Singleton non-object.
            return null;
        }
    }

    public Int32 Depth
    {
        get
        {
            return 0;
        }
    }

    Object IEnumerator.Current
    {
        get
        {
            return Current;
        }
    }

    public dynamic Current
    {
        get
        {
            if (currentObject != null)
            {
                switch (projectionTypeCode)
                {
                    case null:
                        return currentObject;

                    case DbTypeCode.Binary:
                        return currentObject.GetBinary(0);

                    case DbTypeCode.Boolean:
                        return currentObject.GetBoolean(0);

                    case DbTypeCode.Byte:
                        return currentObject.GetByte(0);

                    case DbTypeCode.DateTime:
                        return currentObject.GetDateTime(0);

                    case DbTypeCode.Decimal:
                        return currentObject.GetDecimal(0);

                    case DbTypeCode.Double:
                        return currentObject.GetDouble(0);

                    case DbTypeCode.Int16:
                        return currentObject.GetInt16(0);

                    case DbTypeCode.Int32:
                        return currentObject.GetInt32(0);

                    case DbTypeCode.Int64:
                        return currentObject.GetInt64(0);

                    case DbTypeCode.Object:
                        return currentObject.GetObject(0);

                    case DbTypeCode.SByte:
                        return currentObject.GetSByte(0);

                    case DbTypeCode.Single:
                        return currentObject.GetSingle(0);

                    case DbTypeCode.String:
                        return currentObject.GetString(0);

                    case DbTypeCode.UInt16:
                        return currentObject.GetUInt16(0);

                    case DbTypeCode.UInt32:
                        return currentObject.GetUInt32(0);

                    case DbTypeCode.UInt64:
                        return currentObject.GetUInt64(0);

                    default:
                        throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect projectionTypeCode.");
                }
            }

            throw new InvalidOperationException("Enumerator has not started or has already finished.");
        }
    }

    public Row CurrentRow
    {
        get
        {
            if (currentObject != null)
                return currentObject;

            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect currentObject.");
        }
    }

    private unsafe Byte* ValidateAndGetRecreateKey(Byte* rk) {
        Byte* staticDataOffset = ValidateAndGetStaticKeyOffset(rk);
        UInt16 dynDataOffset = (*(UInt16*)(staticDataOffset + 2));
        Debug.Assert(dynDataOffset != 0);
        return rk + dynDataOffset;
    }

    /// <summary>
    /// Tries to recreate enumerator using provided key.
    /// </summary>
    unsafe Boolean TryRecreateEnumerator(Byte* rk) {
        // In order to skip enumerator recreation next time.
        triedEnumeratorRecreation = true;

        Byte* recreationKey = ValidateAndGetRecreateKey(rk);

        // Creating flags.
        UInt32 _flags = sccoredb.SC_ITERATOR_RANGE_INCLUDE_LSKEY | sccoredb.SC_ITERATOR_RANGE_INCLUDE_GRKEY;

        Byte[] lastKey;
        if (descending)
            lastKey = firstKeyBuffer;
        else
            lastKey = secondKeyBuffer;

        // Trying to recreate the enumerator from key.
        if (iterHelper.RecreateEnumerator_CodeGenFilter(recreationKey, enumerator, filterHandle, _flags, filterDataStream, lastKey)) {
            // Indicating that enumerator has been created.
            enumeratorCreated = true;

            // Checking if we found a deleted object.
            //if (!innermostExtent)
            //{
            // Obtaining saved OID and ETI.
            IteratorHelper.RecreateEnumerator_GetObjectInfo(recreationKey, out keyOID, out keyETI);

            // Enabling recreation object check.
            enableRecreateObjectCheck = true;
            //}

            return true;
        } else // Checking if we are in outer enumerator.
            if (!innermostExtent)
                variableArray.FailedToRecreateObject = true;

        return false;
    }

    /// <summary>
    /// Creating extent scan iterator.
    /// Filter should be already available.
    /// Value stream is supplied.
    /// </summary>
    private Boolean CreateEnumerator()
    {
#if false // TODO EOH2: Transaction id
        // Check that the current transaction has not been changed.
        if (Transaction.Current == null)
        {
            if (variableArray.TransactionId != 0)
                throw ErrorCode.ToException(Error.SCERRITERATORNOTOWNED);
        }
        else
        {
            if (variableArray.TransactionId != Transaction.Current.TransactionId)
                throw ErrorCode.ToException(Error.SCERRITERATORNOTOWNED);
        }
#endif

        // Trying to get existing/create new privateFilter.
        filterHandle = privateFilter.GetFilterHandle();
        iterHelper.AddGeneratedFilter(filterHandle);

        // Updating data stream as usual (taking into account
        // the context object from some previous extent).
        filterDataStream = privateFilter.GetDataStream(contextObject);
        iterHelper.AddDataStream(filterDataStream);
        
        // Trying to recreate the enumerator.
        unsafe
        {
            // Using offset key only if enumerator is not already in recreation mode.
            if ((useOffsetkey) && (fetchOffsetKeyExpr != null))
                //&& (variableArray.RecreationKeyData == null))
            {
                fixed (Byte* recrKey = (fetchOffsetKeyExpr as BinaryVariable).Value.Value.GetInternalBuffer())
                {
                    // Checking if recreation key is valid.
                    if ((*(Int32*)recrKey) > IteratorHelper.RK_EMPTY_LEN)
                        return TryRecreateEnumerator(recrKey + 4);
                }
            }

#if false
            // Checking if key data is available and if there is no failure during outer extent recreation.
            if ((variableArray.RecreationKeyData != null) &&
                (!triedEnumeratorRecreation) &&
                (!variableArray.FailedToRecreateObject))
            {
                // Checking if recreation key is valid.
                if ((*(Int32*)variableArray.RecreationKeyData) > IteratorHelper.RK_EMPTY_LEN)
                    return TryRecreateEnumerator(variableArray.RecreationKeyData);
            }
#endif
        }


        // Creating native iterator.
        if (descending)
        {
            iterHelper.GetEnumeratorCached_CodeGenFilter(
                sccoredb.SC_ITERATOR_RANGE_INCLUDE_LSKEY | sccoredb.SC_ITERATOR_RANGE_INCLUDE_GRKEY | sccoredb.SC_ITERATOR_SORTED_DESCENDING,
                secondKeyBuffer,
                firstKeyBuffer,
                enumerator);
        }
        else
        {
            iterHelper.GetEnumeratorCached_CodeGenFilter(
                sccoredb.SC_ITERATOR_RANGE_INCLUDE_LSKEY | sccoredb.SC_ITERATOR_RANGE_INCLUDE_GRKEY,
                firstKeyBuffer,
                secondKeyBuffer,
                enumerator);
        }

        // Indicating that enumerator has been created.
        enumeratorCreated = true;

        // Indicating that data stream has changed.
        //dataStreamChanged = true;

        return true;
    }

    public Boolean MoveNext()
    {
        // Calling enumerator creation code (ranges, flags, etc.).
        if (!enumeratorCreated)
        {
            // Failure to create enumerator can only be caused
            // by being in last position in the index tree.
            if (!CreateEnumerator())
                return false;
        }

        if (counter == 0) {
            if (fetchOffsetExpr != null) {
                Debug.Assert(fetchOffsetKeyExpr == null);
                if (fetchOffsetExpr.EvaluateToInteger(null) != null) {
                    for (int i = 0; i < fetchOffsetExpr.EvaluateToInteger(null).Value; i++)
                        if (!enumerator.MoveNext())
                            return false;
                    counter = 0;
                }
            }
            if (fetchNumberExpr != null)
                if (fetchNumberExpr.EvaluateToInteger(null) != null)
                    fetchNumber = fetchNumberExpr.EvaluateToInteger(null).Value;
                else
                    fetchNumber = 0;

            if (enableRecreateObjectCheck) {
                // Do move next and check if at recreated key
                if ((counter < fetchNumber) && enumerator.MoveNext()) {
                    // Fetching new object information.
                    // TODO/Entity:
                    IObjectProxy dbObject = enumerator.Current as IObjectProxy;

                    // Checking if its the same object.
                    // TODO/Entity:
                    // It should be enough to compare by identity, no?
                    if ((keyOID != dbObject.Identity) && (keyETI != dbObject.ThisHandle)) {
                        isAtRecreatedKey = false;
                        variableArray.FailedToRecreateObject = true;
                    } else
                        isAtRecreatedKey = true;

                    // Disabling any further checks.
                    enableRecreateObjectCheck = false;

                    currentObject = new Row(rowTypeBinding);
                    currentObject.AttachObject(extentNumber, enumerator.Current);
                    counter++;
                    // If stay at recreated key then return
                    if (stayAtOffsetkey || !isAtRecreatedKey)
                        return true;
                    else
                        counter = 0;
                } else {
                    enableRecreateObjectCheck = false;
                    isAtRecreatedKey = false;
                    currentObject = null;
                    return false;
                }
            }
        }
        isAtRecreatedKey = false;

        if ((counter < fetchNumber) && enumerator.MoveNext()) // Note: replicated code
        {
            currentObject = new Row(rowTypeBinding);
            currentObject.AttachObject(extentNumber, enumerator.Current);
            counter++;
            return true;
        }

        currentObject = null;
        return false;
    }

    internal void CreateStaticKeyPart() {
    }

    /// <summary>
    /// Used to populate the recreation key.
    /// </summary>
    public unsafe UInt16 SaveEnumerator(Byte* keyData, UInt16 globalOffset, Boolean saveDynamicDataOnly) {
        // Immediately preventing further accesses to current object.
        currentObject = null;

        // If we already tried to recreate the enumerator and we want to write static data,
        // just return first dynamic data offset.
        if (triedEnumeratorRecreation && (!saveDynamicDataOnly))
            return (*(UInt16*)(keyData + IteratorHelper.RK_FIRST_DYN_DATA_OFFSET));

        UInt16 origGlobalOffset = globalOffset;

        // Position of enumerator.
        UInt16 enumGlobalOffset = (ushort)((nodeId << 2) + IteratorHelper.RK_HEADER_LEN);

        // Writing static data.
        if (!saveDynamicDataOnly) {
            // In order to exclude double copy of last key.
            //dataStreamChanged = false;

            // Emptying static data position for this enumerator.
            (*(UInt16*)(keyData + enumGlobalOffset)) = 0;

            // Saving type of this node
            *((byte*)(keyData + globalOffset)) = (byte)NodeType;
            globalOffset += 1;

            // Saving position of the data for current extent.
            (*(UInt16*)(keyData + enumGlobalOffset)) = origGlobalOffset;

            // Saving absolute position of the first dynamic data.
            (*(UInt16*)(keyData + IteratorHelper.RK_FIRST_DYN_DATA_OFFSET)) = globalOffset;
        } else {
            // Writing dynamic data.

            // Points to dynamic data offset.
            UInt16* dynDataOffset = (UInt16*)(keyData + enumGlobalOffset + 2);

            // Emptying dynamic data position for this enumerator.
            (*dynDataOffset) = 0;

            // Fetching current enumerator key.
            Byte* createdKey = null;
            UInt32 err = 0;

#if false
#endif

                // Not supported for filter iterator.
                enumerator.Dispose();
                throw new NotSupportedException();
            // TODO/Entity:
            IObjectProxy dbObject = enumerator.CurrentRaw as IObjectProxy;
            if (dbObject != null) {
                // Getting current position of the object in iterator.
                err = sccoredb.sc_get_index_position_key(
                    indexInfo.Handle,
                    dbObject.Identity,
                    dbObject.ThisHandle,
                    &createdKey
                    );

#if false
                    // Placing local time of the iterator into recreation key.
                    UInt32 itLocalTime = enumerator.GetIteratorLocalTime();

                    // Saving local time of the iterator.
                    (*(UInt32*)(createdKey + (*(UInt32*)createdKey) - 4)) = itLocalTime;
#endif
            }

            // Disposing iterator.
            enumerator.Dispose();

            // Checking the error.
            if (err != 0)
                throw ErrorCode.ToException(err);

            // Since iterator is closed we have to mark it as so.
            enumerator.MarkAsDisposed();

            // Checking if it was last object.
            if (createdKey == null)
                return origGlobalOffset;

            // Copying the recreation key.
            UInt16 bytesWritten = *((UInt16*)createdKey);

            // Copying the buffer.
            //SqlDebugHelper.PrintByteBuffer("FullTableScan Saving Recreation Key", createdKey, true);
            Kernel32.MoveByteMemory(keyData + globalOffset, createdKey, bytesWritten);
            globalOffset += bytesWritten;

            // Saving position of dynamic data.
            (*dynDataOffset) = origGlobalOffset;
        }

        return globalOffset;
    }

    public Boolean MoveNextSpecial(Boolean force)
    {
        if (!force && MoveNext())
        {
            return true;
        }

        if (counter <= 0 || force)
        {
            // Create a "null" object.
            NullObject nullObj = new NullObject(rowTypeBinding.GetTypeBinding(extentNumber));
            currentObject = new Row(rowTypeBinding);
            currentObject.AttachObject(extentNumber, nullObj);
            counter++;
            return true;
        }

        currentObject = null;
        return false;
    }

    /// <summary>
    /// Resets the enumerator with a context object
    /// (reset already includes iterator disposal).
    /// </summary>
    /// <param name="obj">Context object from another enumerator.</param>
    public override void Reset(Row obj)
    {
        // We are disposing the lowest level internal iterator here.
        enumerator.Dispose();
        enumeratorCreated = false;
        //dataStreamChanged = false;

        enableRecreateObjectCheck = false;
        triedEnumeratorRecreation = false;

        currentObject = null;
        contextObject = obj;

        counter = 0;
        privateFilter.ResetCached(); // Reseting private filters.

        if (obj == null) {
            isAtRecreatedKey = false;
            stayAtOffsetkey = false;
            useOffsetkey = true;
        }
    }

    public override IExecutionEnumerator Clone(RowTypeBinding typeBindingClone, VariableArray varArrClone)
    {
        ILogicalExpression conditionClone = condition.Clone(varArrClone);

        SortOrder sortOrder = (descending == true ? SortOrder.Descending : SortOrder.Ascending);

        INumericalExpression fetchNumberExprClone = null;
        if (fetchNumberExpr != null)
            fetchNumberExprClone = fetchNumberExpr.CloneToNumerical(varArrClone);

        INumericalExpression fetchOffsetExprClone = null;
        if (fetchOffsetExpr != null)
            fetchOffsetExprClone = fetchOffsetExpr.CloneToNumerical(varArrClone);

        IBinaryExpression fetchOffsetKeyExprClone = null;
        if (fetchOffsetKeyExpr != null)
            fetchOffsetKeyExprClone = fetchOffsetKeyExpr.CloneToBinary(varArrClone);

        return new FullTableScan(nodeId, typeBindingClone,
            extentNumber,
            indexInfo,
            conditionClone,
            sortOrder,
            fetchNumberExprClone,
            fetchOffsetExprClone,
            fetchOffsetKeyExprClone,
            innermostExtent, 
            privateFilter.Clone(conditionClone, typeBindingClone),
            varArrClone, query, TopNode);
    }

    public override void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
    {
        stringBuilder.AppendLine(tabs, "FullTableScan(");
        //stringBuilder.AppendLine(tabs + 1, indexHandle.ToString());
        stringBuilder.AppendLine(tabs + 1, indexInfo.Name + " ON " + indexInfo.TableName);
        stringBuilder.AppendLine(tabs + 1, extentNumber.ToString());
        condition.BuildString(stringBuilder, tabs + 1);
        if (descending) stringBuilder.AppendLine(tabs + 1, "Descending");
        else stringBuilder.AppendLine(tabs + 1, "Ascending");
        base.BuildFetchString(stringBuilder, tabs + 1);
        stringBuilder.AppendLine(tabs, ")");
    }

    /// <summary>
    /// Generates compilable code representation of this data structure.
    /// </summary>
    public void GenerateCompilableCode(CodeGenStringGenerator stringGen)
    {
    }

    /// <summary>
    /// Gets the unique name for this enumerator.
    /// </summary>
    public String GetUniqueName(UInt64 seqNumber)
    {
        if (uniqueGenName == null)
            uniqueGenName = "Scan" + extentNumber;

        return uniqueGenName;
    }
}
}
