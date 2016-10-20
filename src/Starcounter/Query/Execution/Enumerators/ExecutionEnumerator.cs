// ***********************************************************************
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
using System.Diagnostics;

namespace Starcounter.Query.Execution
{
// Implementation for base execution enumerator class.
internal abstract class ExecutionEnumerator
{
    protected static int OFFSETELEMNETSIZE = 2; // Initial size of offset element in tuple writer
    protected static byte OffsetRootHeaderLength; // Length of the header of offset key tuple.
    protected readonly byte nodeId; // Unique node identifier in execution tree. Top node has largest nodeId.
    protected readonly byte OffsetTuppleLength; // Length of the tuple with offset of this enumerator.
    protected Row currentObject = null; // Represents latest successfully retrieved object.
    protected VariableArray variableArray = null; // Array with variables from query.
    protected Int64 counter = 0; // Number of successful hits (retrieved objects).
    protected String query = null; // Original SQL query which this enumerator belongs to.
    protected RowTypeBinding rowTypeBinding = null; // Type binding for the enumerator.
    protected Nullable<DbTypeCode> projectionTypeCode = null; // If singleton projection, then the DbTypeCode of that singleton, otherwise null.
    protected PropertyMapping propertyBinding = null; // If singleton projection, then the PropertyBinding of that singleton, otherwise null.
    protected UInt64 uniqueQueryID = 0; // Uniquely identifies query it belongs to.
    protected String uniqueGenName = null; // Uniquely identifies the scan during code generation.
    protected Boolean hasCodeGeneration = false; // Indicates if code generation is done for this enumerator.

#if false // Not in use or not needed
    protected byte totalNodeNr = 0; // Total number of nodes in execution tree if it is the top node (i.e., nodeId=0);
    internal byte TotalNodeNr { get { return totalNodeNr; } set { totalNodeNr = value; } }
    protected byte[] staticOffsetKeyPart; // Stores static part of the offset key related only to this node
    protected int staticOffsetKeyPartLength = 0; // Stores the length of the offset key static part
#endif
    internal readonly EnumeratorNodeType NodeType;
    internal readonly Boolean TopNode = false;

    protected INumericalExpression fetchNumberExpr = null; // Represents fetch literal or variable.
    protected Int64 fetchNumber = Int64.MaxValue; // Maximum fetch number.
    protected INumericalExpression fetchOffsetExpr = null; // Represents offset literal or variable.
    protected IBinaryExpression fetchOffsetKeyExpr = null; // Represents offset key literal or variable.
    protected Boolean innermostExtent = false; // True, if this execution-enumerator represents the innermost extent, otherwise false.

    // Cache-related properties.
    protected LinkedList<IExecutionEnumerator> enumCacheListFrom = null; // From which cache list this enumerator came from.
    protected LinkedListNode<IExecutionEnumerator> enumListNode = null; // Node with this execution enumerator.

    protected Boolean isBisonParserUsed = false;

    /// <summary>
    /// Default constructor.
    /// </summary>
    internal ExecutionEnumerator(Byte nodeId, EnumeratorNodeType nodeType, RowTypeBinding rowTypeBind, VariableArray varArray,
        Boolean topNode, byte offsetTuppleLength)
    {
        this.nodeId = nodeId;
        NodeType = nodeType;
        rowTypeBinding = rowTypeBind;
        variableArray = varArray;
        TopNode = topNode;
        OffsetTuppleLength = offsetTuppleLength;

        if (varArray != null && (varArray.QueryFlags & QueryFlags.SingletonProjection) != 0)
        {
            propertyBinding = (PropertyMapping)rowTypeBinding.GetPropertyBinding(0);
            projectionTypeCode = propertyBinding.TypeCode;
        }
    }

    public byte NodeId { get { return nodeId; } }

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
    /// Gets the type binding of the Row.
    /// </summary>
    public virtual RowTypeBinding RowTypeBinding
    {
        get
        {
            return rowTypeBinding;
        }
    }

    /// <summary>
    /// If the projection is a singleton, then the DbTypeCode of that singleton, otherwise null.
    /// </summary>
    public virtual Nullable<DbTypeCode> ProjectionTypeCode
    {
        get
        {
            return projectionTypeCode;
        }
    }

    public virtual IPropertyBinding PropertyBinding {
        get {
            return propertyBinding;
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
    /// Gets first found literal value represented as a string.
    /// </summary>
    public String LiteralValue {
        get { return variableArray.LiteralValue; }
    }

    /// <summary>
    /// Returns if Bison-parser was used in creation of the enumerator.
    /// It will be removed when Prolog-parser is deprecated.
    /// </summary>
    public Boolean IsBisonParserUsed {
        get { return isBisonParserUsed; }
        internal set { isBisonParserUsed = value; }
    }

    public Object ProjectObject(Row currentObject, DbTypeCode? projectionTypeCode) {
        if (currentObject != null) {
            switch (projectionTypeCode) {
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
        throw ErrorCode.ToException(Error.SCERRINVALIDCURRENT, (m, e) => new InvalidOperationException(m));
    }

    public Object Current {
        get {
            return ProjectObject(currentObject, projectionTypeCode);
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

        IExecutionEnumerator execEnum = this as IExecutionEnumerator;
        // Using cache temp buffer.
        Byte[] tempBuffer = Scheduler.GetInstance().SqlEnumCache.TempBuffer;
        int bytesWritten = 0;
        unsafe {
            fixed (Byte* recreationKey = tempBuffer) {
                Debug.Assert(TopNode);
                OffsetRootHeaderLength = 1;
                byte nodesNum = (byte)(NodeId + 1);
                SafeTupleWriterBase64 root = new SafeTupleWriterBase64(recreationKey, (uint)(OffsetRootHeaderLength + 1), OFFSETELEMNETSIZE, tempBuffer.Length);
                // General validation data
                root.WriteULong(nodesNum); // Saving number of enumerators
                // Saving enumerator data
                SafeTupleWriterBase64 enumerators = new SafeTupleWriterBase64(root.AtEnd, nodesNum, OFFSETELEMNETSIZE, root.AvailableSize);
                if (execEnum.SaveEnumerator(ref enumerators, 0) == -1)
                    return null;
                //execEnum.SaveEnumerator(recreationKey, 0, true);
                root.HaveWritten(enumerators.SealTuple());
                bytesWritten = root.SealTuple();
            }
        }
        // Allocating space for offset key.
        Byte[] offsetKey = new Byte[bytesWritten];

        // Copying the recreation key into provided user buffer.
        Buffer.BlockCopy(tempBuffer, 0, offsetKey, 0, (int)bytesWritten);

        // Returning the key.
        return offsetKey;
    }

    protected unsafe SafeTupleReaderBase64 ValidateNodeAndReturnOffsetReader(byte* key, byte tupleLength) {
        SafeTupleReaderBase64 root = new SafeTupleReaderBase64(key, (uint)(OffsetRootHeaderLength + 1));
        Debug.Assert(OffsetRootHeaderLength == 1);
        byte nodesNum = (byte)root.ReadULong(0);
        if (TopNode && nodesNum != (nodeId+1))
            throw ErrorCode.ToException(Error.SCERRINVALIDOFFSETKEY, "Unexpected number of nodes in execution plan. Actual number of nodes is " +
                (nodeId + 1) + ", while the offset key contains " + nodesNum + " nodes.");
        SafeTupleReaderBase64 enumerators = new SafeTupleReaderBase64(root.GetPosition(1), nodesNum);
        SafeTupleReaderBase64 thisEnumerator = new SafeTupleReaderBase64(enumerators.GetPosition(nodeId), tupleLength);
        EnumeratorNodeType keyNodeType = (EnumeratorNodeType)thisEnumerator.ReadULong(0);
        if (keyNodeType != NodeType)
            throw ErrorCode.ToException(Error.SCERRINVALIDOFFSETKEY, "Unexpected node type in execution plan. Current node type " +
                NodeType.ToString() + ", while the offset key contains node type " + (keyNodeType).ToString() + ".");
        Debug.Assert(thisEnumerator.ReadULong(1) == nodeId);
        return thisEnumerator;
    }

#if false // Old implementation
    protected unsafe Byte* ValidateAndGetStaticKeyOffset(byte* key) {
        if (TopNode) {
            byte nodeNrs = (*(Byte*)(key + IteratorHelper.RK_NODE_NUM_OFFSET));
            if (nodeNrs != (NodeId+1))
                throw ErrorCode.ToException(Error.SCERRINVALIDOFFSETKEY, "Unexpected number of nodes in execution plan. Actual number of nodes is " +
                    (NodeId+1) + ", while the offset key contains " + nodeNrs + " nodes.");
        }
        Byte* staticDataOffset = key + (nodeId << 2) + IteratorHelper.RK_HEADER_LEN;
        Byte* staticData = key + (*(UInt16*)(staticDataOffset));
        EnumeratorNodeType keyNodeType = (EnumeratorNodeType)(*staticData);
        if (keyNodeType != NodeType)
            throw ErrorCode.ToException(Error.SCERRINVALIDOFFSETKEY, "Unexpected node type in execution plan. Current node type " +
                NodeType.ToString() + ", while the offset key contains node type " + (keyNodeType).ToString() + ".");
        return staticDataOffset;
    }
#endif

    /// <summary>
    /// Enumerator reset functionality.
    /// </summary>
    public abstract void Reset(Row obj);

    /// <summary>
    /// Resets the enumerator.
    /// </summary>
    public virtual void Reset() {
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
    public abstract IExecutionEnumerator Clone(RowTypeBinding typeBindingClone, VariableArray varArrClone);

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
        varArrayClone.LiteralValue = variableArray.LiteralValue;

        // Calling main clone method of related execution enumerator.
        RowTypeBinding rowTypeBindingClone = null;
        if (rowTypeBinding != null)
        {
            rowTypeBindingClone = rowTypeBinding.Clone(varArrayClone);
        }

        IExecutionEnumerator newExecEnum = Clone(rowTypeBindingClone, varArrayClone);

        // Transferring unique query ID.
        newExecEnum.UniqueQueryID = uniqueQueryID;

        // Attaching new enumerator to existing cache.
        newExecEnum.AttachToCache(enumCacheListFrom);

        return newExecEnum;
    }

    // Concrete build string method should be defined in every execution enumerator.
    public abstract void BuildString(MyStringBuilder stringBuilder, Int32 tabs);

    public void BuildFetchString(MyStringBuilder stringBuilder, Int32 tabs) {
        if (fetchNumberExpr != null) {
            stringBuilder.AppendLine(tabs, "Fetch Number(");
            fetchNumberExpr.BuildString(stringBuilder, tabs + 1);
            stringBuilder.AppendLine(tabs, ")");
        }
        if (fetchOffsetExpr != null)
        {
            stringBuilder.AppendLine(tabs, "Fetch Offset(");
            fetchOffsetExpr.BuildString(stringBuilder, tabs+1);
            stringBuilder.AppendLine(tabs, ")");
        }
        if (fetchOffsetKeyExpr != null) {
            stringBuilder.AppendLine(tabs, "Fetch Offset Key(");
            fetchOffsetKeyExpr.BuildString(stringBuilder, tabs+1);
            stringBuilder.AppendLine(tabs, ")");
        }
    }

    /// <summary>
    /// Returns a string presentation of the execution enumerator including
    /// a specification of the type of the returned objects and the execution plan.
    /// </summary>
    /// <returns>A string presentation of the execution enumerator.</returns>
    public override String ToString()
    {
        MyStringBuilder stringBuilder = new MyStringBuilder();

        if (RowTypeBinding != null)
        {
            RowTypeBinding.BuildString(stringBuilder, 0);
        }

        BuildString(stringBuilder, 0);
        return stringBuilder.ToString();
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