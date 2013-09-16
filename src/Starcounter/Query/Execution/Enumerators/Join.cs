// ***********************************************************************
// <copyright file="Join.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;
using Starcounter.Binding;
using Starcounter.Query.Sql;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Starcounter.Internal;

namespace Starcounter.Query.Execution
{
internal class Join : ExecutionEnumerator, IExecutionEnumerator
{
    JoinType joinType;
    IExecutionEnumerator leftEnumerator;
    IExecutionEnumerator rightEnumerator;
    Row contextObject;
    ILogicalExpression postFilterCondition;
    //Boolean triedEnumeratorRecreation = false; // Indicates if we should try enumerator recreation with supplied key.
    private Boolean stayAtOffsetkey = false;
    private Boolean useOffsetkey = true;

    internal Join(byte nodeId, RowTypeBinding rowTypeBind,
        JoinType type,
        IExecutionEnumerator leftEnum,
        IExecutionEnumerator rightEnum,
        ILogicalExpression postFilterCond,
        INumericalExpression fetchNumExpr,
        INumericalExpression fetchOffsetExpr,
        IBinaryExpression fetchOffsetkeyExpr,
        VariableArray varArr, String query, Boolean topNode)
        : base(nodeId, EnumeratorNodeType.Join, rowTypeBind, varArr, topNode, 2)
    {
        if (rowTypeBind == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect rowTypeBind.");
        if (varArr == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect varArr.");
        if (leftEnum == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect leftEnum.");
        if (rightEnum == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect rightEnum.");
        Debug.Assert(OffsetTuppleLength == 2);
        // if (leftEnum.RowTypeBinding != rowTypeBind)
        //    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incompatible input enumerator leftEnum.");
        // if (rightEnum.RowTypeBinding != rowTypeBind)
        //    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incompatible input enumerator rightEnum.");

        joinType = type;
        leftEnumerator = leftEnum;
        rightEnumerator = rightEnum;
        postFilterCondition = postFilterCond;
        contextObject = null;
        currentObject = null;

        fetchNumberExpr = fetchNumExpr;
        fetchNumber = Int64.MaxValue;
        this.fetchOffsetExpr = fetchOffsetExpr;
        fetchOffsetKeyExpr = fetchOffsetkeyExpr;

        this.query = query;
    }

    public Boolean StayAtOffsetkey {
        get { return stayAtOffsetkey; }
        set {
            stayAtOffsetkey = value;
            rightEnumerator.StayAtOffsetkey = value;
        }
    }

    public Boolean UseOffsetkey {
        get { return useOffsetkey; }
        set {
            useOffsetkey = value;
            leftEnumerator.UseOffsetkey = value;
            rightEnumerator.UseOffsetkey = value;
        }
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
            if (rightEnumerator.Counter > 1)
            {
                return leftEnumerator.Depth + rightEnumerator.Depth + 1;
            }
            return leftEnumerator.Depth;
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

    internal IExecutionEnumerator LeftEnumerator {
        get { return leftEnumerator; }
    }

    internal IExecutionEnumerator RightEnumerator {
        get { return rightEnumerator; }
    }

    /// <summary>
    /// Resets the enumerator with a context object.
    /// </summary>
    /// <param name="obj">Context object from another enumerator.</param>
    public override void Reset(Row obj)
    {
        currentObject = null;
        contextObject = obj;
        counter = 0;
        //triedEnumeratorRecreation = false;

        leftEnumerator.Reset(contextObject);
        rightEnumerator.Reset();
        if (obj == null) { // On Dispose
            stayAtOffsetkey = false;
            useOffsetkey = true;
            leftEnumerator.StayAtOffsetkey = true;
            leftEnumerator.UseOffsetkey = true;
            rightEnumerator.StayAtOffsetkey = false;
            rightEnumerator.UseOffsetkey = true;
        }
    }

    // TODO: Not create a new Row when not necessary.
    private Row MergeObjects(Row obj1, Row obj2)
    {
        Row obj = new Row(rowTypeBinding);
        for (Int32 i = 0; i < rowTypeBinding.TypeBindingCount; i++)
        {
            IObjectView element1 = null;
            IObjectView element2 = null;
            if (obj1 != null)
            {
                element1 = obj1.AccessObject(i);
            }
            if (obj2 != null)
            {
                element2 = obj2.AccessObject(i);
            }
            if (element1 != null && element2 == null)
            {
                obj.AttachObject(i, element1);
            }
            else if (element1 == null && element2 != null)
            {
                obj.AttachObject(i, element2);
            }
            else if (element1 == null && element2 == null)
            {
                obj.AttachObject(i, null);
            }
            else
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Conflicting objects.");
            }
        }
        return obj;
    }

    public Boolean MoveNext()
    {
        // If first call to MoveNext then move to first item in enumerator1.
        if (counter == 0)
        {
            if (fetchOffsetKeyExpr != null)
                ValidOffsetkeyOrNull();
            if (fetchNumberExpr != null)
            {
                if (fetchNumberExpr.EvaluateToInteger(null) != null)
                    fetchNumber = fetchNumberExpr.EvaluateToInteger(null).Value;
                else
                    fetchNumber = 0;
                if (counter >= fetchNumber)
                {
                    currentObject = null;
                    return false;
                }
            }

            leftEnumerator.StayAtOffsetkey = true;
            if (leftEnumerator.MoveNext())
            {
                Row rightContext = MergeObjects(contextObject, leftEnumerator.CurrentRow);
                rightEnumerator.Reset(rightContext);
                // Call to create enumerator on right one if left at recreated key
                if (!leftEnumerator.IsAtRecreatedKey)
                    rightEnumerator.UseOffsetkey = false;
            }
            else
            {
                currentObject = null;
                return false;
            }
            // Pass offset
            long tmpCounter = counter;
            if (fetchOffsetExpr != null)
                for (int i = 0; i < fetchOffsetExpr.EvaluateToInteger(null).Value; i++)
                    NextRightAndLeftEnumerators();
            counter = tmpCounter;
        }
        else if (counter >= fetchNumber)
        {
            currentObject = null;
            return false;
        }

        Row tempCurrentObject = null;
        while (NextRightAndLeftEnumerators())
        {
            tempCurrentObject = MergeObjects(leftEnumerator.CurrentRow, rightEnumerator.CurrentRow);
            if (postFilterCondition == null || postFilterCondition.Evaluate(tempCurrentObject) == TruthValue.TRUE)
            {
                currentObject = tempCurrentObject;
                counter++;
                return true;
            }
        }
        return false;
    }

    private Boolean NextRightAndLeftEnumerators() {
        // Loop until a new combination of one object from enumerator1 and one object from enumerator2 is found.
        if (joinType == JoinType.LeftOuter) {
            while (!rightEnumerator.MoveNextSpecial(false)) {
                if (leftEnumerator.MoveNext()) {
                    Row rightContext = MergeObjects(contextObject, leftEnumerator.CurrentRow);
                    rightEnumerator.Reset(rightContext);
                    rightEnumerator.UseOffsetkey = false;
                } else {
                    currentObject = null;
                    return false;
                }
            }
        } else {
            while (!rightEnumerator.MoveNext()) {
                if (leftEnumerator.MoveNext()) {
                    Row rightContext = MergeObjects(contextObject, leftEnumerator.CurrentRow);
                    rightEnumerator.Reset(rightContext);
                    rightEnumerator.UseOffsetkey = false;
                } else {
                    currentObject = null;
                    return false;
                }
            }
        }
        return true;
    }

    public Boolean MoveNextSpecial(Boolean force)
    {
        if (!force && MoveNext())
        {
            return true;
        }
        else if (counter == 0 || force)
        {
            // Force the creation of NullObjects on the input enumerators.
            leftEnumerator.MoveNextSpecial(true);
            Row rightContext = MergeObjects(contextObject, leftEnumerator.CurrentRow);
            rightEnumerator.Reset(rightContext);
            rightEnumerator.MoveNextSpecial(true);

            // Create new currentObject.
            currentObject = MergeObjects(leftEnumerator.CurrentRow, rightEnumerator.CurrentRow);
            counter++;
            return true;
        }
        else
        {
            currentObject = null;
            return false;
        }
    }

    public unsafe short SaveEnumerator(ref TupleWriterBase64 enumerators, short expectedNodeId) {
        expectedNodeId = leftEnumerator.SaveEnumerator(ref enumerators, expectedNodeId);
        if (expectedNodeId == -1)
            return -1;
        expectedNodeId = rightEnumerator.SaveEnumerator(ref enumerators, expectedNodeId);
        if (expectedNodeId == -1)
            return -1;
        return SaveJoinEnumerator(ref enumerators, expectedNodeId);
    }

    unsafe short SaveJoinEnumerator(ref TupleWriterBase64 enumerators, short expectedNodeId) {
        Debug.Assert(expectedNodeId == nodeId);
        Debug.Assert(OffsetTuppleLength == 2);
        TupleWriterBase64 tuple = new TupleWriterBase64(enumerators.AtEnd, OffsetTuppleLength, OFFSETELEMNETSIZE);
        tuple.SetTupleLength(enumerators.AvailableSize);
        // Static data for validation
        tuple.WriteSafeULong((byte)NodeType);
        tuple.WriteSafeULong(nodeId);
        enumerators.HaveWritten(tuple.SealTuple());
        return (short)(expectedNodeId + 1);
    }

#if false
    /// <summary>
    /// Saves the underlying enumerator state.
    /// </summary>
    public unsafe UInt16 SaveEnumerator(Byte* keyData, UInt16 globalOffset, Boolean saveDynamicDataOnly) {
        UInt16 offset = leftEnumerator.SaveEnumerator(keyData, globalOffset, saveDynamicDataOnly);
        offset = rightEnumerator.SaveEnumerator(keyData, offset, saveDynamicDataOnly);
        offset = SaveJoinEnumerator(keyData, offset, saveDynamicDataOnly);
        return offset;
    }

    public unsafe UInt16 SaveJoinEnumerator(Byte* keyData, UInt16 globalOffset, Boolean saveDynamicDataOnly) {
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
            //rangeChanged = false;

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
        }
        return globalOffset;
    }
#endif

#if false // Old implementation
    private unsafe void ValidateAndGetRecreateKey(Byte* rk) {
        ValidateNodeAndReturnOffsetReader(rk);
#if false // Not in use
        UInt16 dynDataOffset = (*(UInt16*)(staticDataOffset + 2));
        Debug.Assert(dynDataOffset != 0);
        return rk + dynDataOffset;
#endif
    }
#endif

    internal Boolean ValidOffsetkeyOrNull() {
        if (useOffsetkey && fetchOffsetKeyExpr != null) {
            // In order to skip enumerator recreation next time.
            //triedEnumeratorRecreation = true;
            unsafe {
                fixed (Byte* recrKeyBuffer = (fetchOffsetKeyExpr as BinaryVariable).Value.Value.GetInternalBuffer()) {
                    Byte* recrKey = recrKeyBuffer + 4; // Skip buffer length
                    // Checking if recreation key is valid.
                    if ((*(UInt16*)recrKey) > IteratorHelper.RK_EMPTY_LEN) {
                        ValidateNodeAndReturnOffsetReader(recrKey, OffsetTuppleLength);
                    }
                }
            }
        }
        return true;
    }

    /// <summary>
    /// Depending on query flags, populates the flags value.
    /// </summary>
    public unsafe override void PopulateQueryFlags(UInt32* flags)
    {
        leftEnumerator.PopulateQueryFlags(flags);
        rightEnumerator.PopulateQueryFlags(flags);
    }

    private void DebugPresent(String str, Row compObj)
    {
        String objIds = str + "Join ";
        for (Int32 i = 0; i < rowTypeBinding.TypeBindingCount; i++)
        {
            IObjectView obj = compObj.AccessObject(i);
            if (obj != null)
            {
                objIds += i + ":" + obj.TypeBinding.Name + ":" + obj.ToString() + " ";
            }
            else
            {
                objIds += i + ":" + Starcounter.Db.NullString + " ";
            }
        }
        LogSources.Sql.Debug(objIds);
    }

    public override IExecutionEnumerator Clone(RowTypeBinding rowTypeBindClone, VariableArray varArrClone)
    {
        INumericalExpression fetchNumberExprClone = null;
        if (fetchNumberExpr != null)
            fetchNumberExprClone = fetchNumberExpr.CloneToNumerical(varArrClone);
        INumericalExpression fetchOffsetExprClone = null;
        if (fetchOffsetExpr != null)
            fetchOffsetExprClone = fetchOffsetExpr.CloneToNumerical(varArrClone);
        IBinaryExpression fetchOffsetKeyExprClone = null;
        if (fetchOffsetKeyExpr != null)
            fetchOffsetKeyExprClone = fetchOffsetKeyExpr.CloneToBinary(varArrClone);
        ILogicalExpression postFilterConditionClone = null;
        if (postFilterCondition != null)
            postFilterConditionClone = postFilterCondition.Clone(varArrClone);

        return new Join(nodeId, rowTypeBindClone, joinType, leftEnumerator.Clone(rowTypeBindClone, varArrClone),
            rightEnumerator.Clone(rowTypeBindClone, varArrClone), postFilterConditionClone, 
            fetchNumberExprClone, fetchOffsetExprClone, fetchOffsetKeyExprClone, 
            varArrClone, query, TopNode);
    }

    public override void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
    {
        stringBuilder.AppendLine(tabs, "Join(");
        stringBuilder.AppendLine(tabs + 1, joinType.ToString());
        leftEnumerator.BuildString(stringBuilder, tabs + 1);
        rightEnumerator.BuildString(stringBuilder, tabs + 1);
        if (postFilterCondition != null)
            postFilterCondition.BuildString(stringBuilder, tabs + 1);
        base.BuildFetchString(stringBuilder, tabs + 1);
        stringBuilder.AppendLine(tabs, ")");
    }

    /// <summary>
    /// Generates compilable code representation of this data structure.
    /// </summary>
    public void GenerateCompilableCode(CodeGenStringGenerator stringGen)
    {
        String enumName = GetUniqueName(stringGen.SeqNumber());
        String leftEnumName = leftEnumerator.GetUniqueName(stringGen.SeqNumber());
        String rightEnumName = rightEnumerator.GetUniqueName(stringGen.SeqNumber());

        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.GLOBAL_DATA, "INTERNAL_DATA Scan *g_" + enumName + " = 0;" + CodeGenStringGenerator.ENDL);
        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.INIT_DATA, "g_" + enumName + " = new Scan(0" + ", " + enumName + "_CalculateRange);");

        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.DECLARATIONS, "INTERNAL_FUNCTION INT32 " + enumName + "_MoveNext();");
        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "INTERNAL_FUNCTION INT32 " + enumName + "_MoveNext()");
        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "{");
        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "return g_" + enumName + "->MoveNext();");
        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "}" + CodeGenStringGenerator.ENDL);

        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.DECLARATIONS, "INTERNAL_FUNCTION INT32 " + enumName + "_Reset();");
        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "INTERNAL_FUNCTION INT32 " + enumName + "_Reset()");
        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "{");
        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "return g_" + enumName + "->Reset();");
        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "}" + CodeGenStringGenerator.ENDL);

        if (joinType == JoinType.LeftOuter)
        {

        }
        else
        {

        }
    }

    /// <summary>
    /// Gets the unique name for this enumerator.
    /// </summary>
    public String GetUniqueName(UInt64 seqNumber)
    {
        if (uniqueGenName == null)
            uniqueGenName = "Join" + seqNumber;

        return uniqueGenName;
    }

    public Boolean IsAtRecreatedKey { get { return leftEnumerator.IsAtRecreatedKey && rightEnumerator.IsAtRecreatedKey; } }
}
}
