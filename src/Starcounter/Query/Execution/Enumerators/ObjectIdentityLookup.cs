using System;
using System.Collections;
using System.Diagnostics;
using Starcounter.Binding;

namespace Starcounter.Query.Execution {
    /// <summary>
    /// Enumerator with unique access for an object based on ObjectId, which
    /// either given numerical as ObjectNo or string as ObjectID.
    /// </summary>
    internal class ObjectIdentityLookup : ExecutionEnumerator, IExecutionEnumerator {
        Int32 extentNumber;
        Row contextObject;
        ulong currectObjectId;

        IValueExpression expression;
        ILogicalExpression condition;

        Boolean stayAtOffsetkey = false;
        public Boolean StayAtOffsetkey { get { return stayAtOffsetkey; } set { stayAtOffsetkey = value; } }
        Boolean useOffsetkey = true;
        public Boolean UseOffsetkey { get { return useOffsetkey; } set { useOffsetkey = value; } }
        Boolean isAtRecreatedKey = false;
        public Boolean IsAtRecreatedKey { get { return isAtRecreatedKey; } }

        //Boolean enableRecreateObjectCheck = false; // Enables check for deleted object during enumerator recreation.
        Boolean triedEnumeratorRecreation = false; // Indicates if we should try enumerator recreation with supplied key.
        internal ObjectIdentityLookup(byte nodeId, RowTypeBinding rowTypeBind,
        Int32 extNum,
        IValueExpression expr,
        ILogicalExpression cond,
        INumericalExpression fetchNumExpr,
        INumericalExpression fetchOffsetExpr,
        IBinaryExpression fetchOffsetKeyExpr,
        VariableArray varArr, String query, Boolean topNode)
            : base(nodeId, EnumeratorNodeType.ObjectIdentityLookup, rowTypeBind, varArr, topNode) {
            if (rowTypeBind == null)
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect rowTypeBind.");
            if (varArr == null)
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect varArr.");
            if (expr == null)
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect expr.");

            extentNumber = extNum;

            currentObject = null;
            contextObject = null;
            expression = expr;
            condition = cond;

            this.fetchNumberExpr = fetchNumberExpr;
            this.fetchOffsetExpr = fetchOffsetExpr;
            this.fetchOffsetKeyExpr = fetchOffsetKeyExpr;

            this.query = query;
        }

        /// <summary>
        /// The type binding of the resulting objects of the query.
        /// </summary>
        public ITypeBinding TypeBinding {
            get {
                if (projectionTypeCode == null)
                    return rowTypeBinding;

                // Singleton object.
                if (projectionTypeCode == DbTypeCode.Object)
                    return rowTypeBinding.GetPropertyBinding(0).TypeBinding;

                // Singleton non-object.
                return null;
            }
        }

        Object IEnumerator.Current {
            get {
                return Current;
            }
        }

        public dynamic Current {
            get {
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
                throw new InvalidOperationException("Enumerator has not started or has already finished.");
            }
        }

        public Row CurrentRow {
            get {
                if (currentObject != null)
                    return currentObject;

                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect currentObject.");
            }
        }

        public Int32 Depth {
            get {
                return 0;
            }
        }

        public Boolean MoveNext() {
            // Since this enumerator emits up to one object, it is not possible to use offset key and not stay at it.
            if (useOffsetkey && !stayAtOffsetkey && fetchOffsetKeyExpr != null) {
                SameAsOffsetkeyOrNull(null);
                currentObject = null;
                return false;
            }
            if (counter == 0) {
                if (fetchNumberExpr != null && (fetchNumberExpr.EvaluateToInteger(null) == null || fetchNumberExpr.EvaluateToInteger(null).Value <= 0)) {
                    currentObject = null;
                    return false;
                }
                if (fetchOffsetExpr != null) {
                    Debug.Assert(fetchOffsetKeyExpr == null);
                    if (fetchOffsetExpr.EvaluateToInteger(null) != null && fetchOffsetExpr.EvaluateToInteger(null).Value != 0) {
                        currentObject = null;
                        return false;
                    }
                }
                // Lookup object.
                IObjectView obj = EvaluateToObject();
                // Check for null, that the object is in the current extent and check condition.
                if (obj == null || InCurrentExtent(obj) == false || !SameAsOffsetkeyOrNull(obj) || condition.Instantiate(contextObject).Filtrate(obj) == false) {
                    currentObject = null;
                    return false;
                } else {
                    // Create new currentObject.
                    currentObject = new Row(rowTypeBinding);
                    currentObject.AttachObject(extentNumber, obj);
                    counter++;
                    return true;
                }
            } else {
                currentObject = null;
                return false;
            }
        }

        public Boolean MoveNextSpecial(Boolean force) {
            if (!force && MoveNext()) {
                return true;
            } else if (counter == 0 || force) {
                // Create a NullObject.
                NullObject nullObj = new NullObject(rowTypeBinding.GetTypeBinding(extentNumber));
                currentObject = new Row(rowTypeBinding);
                currentObject.AttachObject(extentNumber, nullObj);
                counter++;
                return true;
            } else {
                currentObject = null;
                return false;
            }
        }

        /// <summary>
        /// Evaluates the expression and look up the corresponding object
        /// </summary>
        /// <returns></returns>
        IObjectView EvaluateToObject() {
            if (expression is IStringExpression)
                currectObjectId = DbHelper.Base64ForUrlDecode((expression as IStringExpression).EvaluateToString(contextObject));
            else
                currectObjectId = (ulong)(expression as INumericalExpression).EvaluateToUInteger(contextObject);
            return DbHelper.FromID(currectObjectId);
        }


        /// <summary>
        /// Controls that the object obj is in the current extent (the extent of this reference-lookup).
        /// The object's type must be equal to or a subtype of the extent type.
        /// </summary>
        /// <param name="obj">The object to control.</param>
        /// <returns>True, if the object is in the current extent, otherwise false.</returns>
        internal Boolean InCurrentExtent(IObjectView obj) {
            if (rowTypeBinding.GetTypeBinding(extentNumber) is TypeBinding && obj.TypeBinding is TypeBinding) {
                TypeBinding extentTypeBind = rowTypeBinding.GetTypeBinding(extentNumber) as TypeBinding;
                TypeBinding tmpTypeBind = obj.TypeBinding as TypeBinding;
                return tmpTypeBind.SubTypeOf(extentTypeBind);
            } else {
                return false;
            }
        }

        private unsafe Byte* ValidateAndGetRecreateKey(Byte* rk) {
            Byte* staticDataOffset = ValidateAndGetStaticKeyOffset(rk);
            UInt16 dynDataOffset = (*(UInt16*)(staticDataOffset + 2));
            Debug.Assert(dynDataOffset != 0);
            return rk + dynDataOffset;
        }

        internal Boolean SameAsOffsetkeyOrNull(IObjectView obj) {
            if (useOffsetkey && fetchOffsetKeyExpr != null) {
                // In order to skip enumerator recreation next time.
                triedEnumeratorRecreation = true;
                unsafe {
                    fixed (Byte* recrKeyBuffer = (fetchOffsetKeyExpr as BinaryVariable).Value.Value.GetInternalBuffer()) {
                        Byte* recrKey = recrKeyBuffer + 4; // Skip buffer length
                        // Checking if recreation key is valid.
                        if ((*(UInt16*)recrKey) > IteratorHelper.RK_EMPTY_LEN) {
                            Byte* recreationKey = ValidateAndGetRecreateKey(recrKey);
                            if (obj == null) // Moving out from offset key on first MoveNext
                                return false;
                            // Check if current object matches stored in the recreation key
                            if (currectObjectId == (*(ulong*)recreationKey))
                                isAtRecreatedKey = true;
                            else {
                                isAtRecreatedKey = false;
                                variableArray.FailedToRecreateObject = true;
                                throw ErrorCode.ToException(Error.SCERRINVALIDOFFSETKEY, "Inconsistency between ObjectNo value from the offset key and obtained for this query.");
                            }
                        }
                    }
                }
                return isAtRecreatedKey;
            } 
            return true;
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

                // Storing object id
                *((ulong*)(keyData + globalOffset)) = currectObjectId;
                globalOffset += sizeof(ulong);

                // Saving position of dynamic data.
                (*dynDataOffset) = origGlobalOffset;

            }
            return globalOffset;
        }

        /// <summary>
        /// Resets the enumerator with a context object.
        /// </summary>
        /// <param name="obj">Context object from another enumerator.</param>
        public override void Reset(Row obj) {
            contextObject = obj;
            currentObject = null;
            counter = 0;
            triedEnumeratorRecreation = false;

            if (obj == null) {
                stayAtOffsetkey = false;
                useOffsetkey = true;
            }
        }

        public override IExecutionEnumerator Clone(RowTypeBinding rowTypeBindClone, VariableArray varArrClone) {
            INumericalExpression fetchNumberExprClone = null;
            if (fetchNumberExpr != null)
                fetchNumberExprClone = fetchNumberExpr.CloneToNumerical(varArrClone);
            INumericalExpression fetchOffsetExprClone = null;
            if (fetchOffsetExpr != null)
                fetchOffsetExprClone = fetchOffsetExpr.CloneToNumerical(varArrClone);
            IBinaryExpression fetchOffsetKeyExprClone = null;
            if (fetchOffsetKeyExpr != null)
                fetchOffsetKeyExprClone = fetchOffsetKeyExpr.CloneToBinary(varArrClone);

            IValueExpression expressionClone = null;
            if (expression is INumericalExpression)
                expressionClone = (expression as INumericalExpression).CloneToNumerical(varArrClone);
            else
                expressionClone = (expression as IStringExpression).CloneToString(varArrClone);

            return new ObjectIdentityLookup(nodeId, rowTypeBindClone, extentNumber, expressionClone,
                condition.Clone(varArrClone), fetchNumberExprClone, fetchOffsetExprClone, fetchOffsetKeyExprClone, 
                varArrClone, query, TopNode);
        }

        public override void BuildString(MyStringBuilder stringBuilder, Int32 tabs) {
            stringBuilder.AppendLine(tabs, "ObjectIdentityLookup(");
            stringBuilder.AppendLine(tabs + 1, extentNumber.ToString());
            expression.BuildString(stringBuilder, tabs + 1);
            condition.BuildString(stringBuilder, tabs + 1);
            base.BuildFetchString(stringBuilder, tabs + 1);
            stringBuilder.AppendLine(tabs, ")");
        }
    
        public void GenerateCompilableCode(CodeGenStringGenerator stringGen) {
            //expression.GenerateCompilableCode(stringGen);
            throw ErrorCode.ToException(Error.SCERRNOTIMPLEMENTED);
        }
    
        /// <summary>
        /// Gets the unique name for this enumerator.
        /// </summary>
        public String GetUniqueName(UInt64 seqNumber) {
            if (uniqueGenName == null)
                uniqueGenName = "ObjectIdentityLookup" + extentNumber;
            return uniqueGenName;
        }
    }
}
