
using Starcounter.Binding;
using Starcounter.Query.Execution;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Starcounter {

    /// <summary>
    /// </summary>
    public class SqlEnumerator<T> : IRowEnumerator<T>, IEnumerator<T>, IEnumerator, IDisposable {


        internal IExecutionEnumerator subEnumerator;
        private XNode node;

        internal SqlEnumerator(IExecutionEnumerator subEnumerator) {
            if (subEnumerator == null)
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect subEnumerator.");

            this.subEnumerator = subEnumerator;

            node = new XNode(this, subEnumerator);
            ThreadData.Current.RegisterObject(node);
        }

        /// <summary>
        /// Moves to the next of the resulting objects of the query.
        /// </summary>
        /// <returns>True if there is a next object, otherwise false.</returns>
        public bool MoveNext() {
            if (subEnumerator != null)
                return subEnumerator.MoveNext();
            else
                throw new ObjectDisposedException("Enumerator");
        }

        /// <summary>
        /// Resets the result by setting the cursor at the position before the first object.
        /// </summary>
        public void Reset() {
            if (subEnumerator != null)
                subEnumerator.Reset();
            else
                throw new ObjectDisposedException("Enumerator");
        }

        /// <summary>
        /// Releases unmanaged resources.
        /// </summary>
        public void Dispose() {
            if (subEnumerator != null) {
                subEnumerator.Dispose();
                subEnumerator = null;
                node.MarkAsDead();
            }
        }

        /// <summary>
        /// The SQL query this SQL enumerator executes.
        /// </summary>
        public string Query {
            get {
                if (subEnumerator != null)
                    return subEnumerator.Query;
                else
                    throw new ObjectDisposedException("Enumerator");
            }
        }

        /// <summary>
        /// If the projection is an (Entity or Row) object, then the type binding of that object, otherwise null.
        /// </summary>
        public ITypeBinding TypeBinding {
            get {
                if (subEnumerator != null)
                    return subEnumerator.TypeBinding;
                else
                    throw new ObjectDisposedException("Enumerator");
            }
        }

        /// <summary>
        /// If the projection is a singleton, then the DbTypeCode of that singleton, otherwise null.
        /// </summary>
        public Nullable<DbTypeCode> ProjectionTypeCode {
            get {
                if (subEnumerator != null)
                    return subEnumerator.ProjectionTypeCode;
                else
                    throw new ObjectDisposedException("Enumerator");
            }
        }

        /// <summary>
        /// Counts the number of returned objects.
        /// </summary>
        public long Counter {
            get {
                if (subEnumerator != null)
                    return subEnumerator.Counter;
                else
                    throw new ObjectDisposedException("Enumerator");
            }
        }

        public bool IsBisonPrarserUsed {
            get { return subEnumerator.IsBisonPrarserUsed;}
        }

        /// <summary>
        /// Gets offset key of the SQL enumerator if it is possible.
        /// </summary>
        public byte[] GetOffsetKey() {
            if (subEnumerator != null)
                return subEnumerator.GetOffsetKey();
            else
                throw new ObjectDisposedException("Enumerator");
        }

        /// <summary>
        /// Returns a string presentation of the execution enumerator including
        /// a specification of the type of the returned objects and the execution plan.
        /// </summary>
        /// <returns>A string presentation of the execution enumerator.</returns>
        public override string ToString() {
            try {
                return subEnumerator.ToString();
            }
            catch (NullReferenceException) {
                if (subEnumerator == null)
                    throw new ObjectDisposedException("Enumerator");
                throw;
            }
        }

        object System.Collections.IEnumerator.Current {
            get {
                return Current;
            }
        }


//        internal SqlEnumerator(IExecutionEnumerator subEnumerator) : base(subEnumerator) { }

        // We hide the base Current property to return an instance of T instead
        // of a dynamic in case the property is accessed from generic
        // SqlEnumerator instance in order to not polute the calling code with
        // dynamic code when you explicitly specify the type.

        /// <summary>
        /// Gets the current item (row) in the result of the query.
        /// </summary>
        public T Current {
            get {
                if (subEnumerator != null)
                    return subEnumerator.Current;
                else
                    throw new ObjectDisposedException("Enumerator");
            }
        }

        T System.Collections.Generic.IEnumerator<T>.Current {
            get {
                return Current;
            }
        }
        
        //public void SetVariableToNull(Int32 index)
        //{
        //    if (subEnumerator != null)
        //        subEnumerator.SetVariableToNull(index);
        //    else
        //        throw new ObjectDisposedException("Enumerator");
        //}

        //public void SetVariable(Int32 index, Binary newValue)
        //{
        //    if (subEnumerator != null)
        //        subEnumerator.SetVariable(index, newValue);
        //    else
        //        throw new ObjectDisposedException("Enumerator");
        //}

        //public void SetVariable(Int32 index, Byte[] newValue)
        //{
        //    if (subEnumerator != null)
        //        subEnumerator.SetVariable(index, newValue);
        //    else
        //        throw new ObjectDisposedException("Enumerator");
        //}

        //public void SetVariable(Int32 index, Boolean newValue)
        //{
        //    if (subEnumerator != null)
        //        subEnumerator.SetVariable(index, newValue);
        //    else
        //        throw new ObjectDisposedException("Enumerator");
        //}

        //public void SetVariable(Int32 index, DateTime newValue)
        //{
        //    if (subEnumerator != null)
        //        subEnumerator.SetVariable(index, newValue);
        //    else
        //        throw new ObjectDisposedException("Enumerator");
        //}

        //public void SetVariable(Int32 index, Decimal newValue)
        //{
        //    if (subEnumerator != null)
        //        subEnumerator.SetVariable(index, newValue);
        //    else
        //        throw new ObjectDisposedException("Enumerator");
        //}

        //public void SetVariable(Int32 index, Double newValue)
        //{
        //    if (subEnumerator != null)
        //        subEnumerator.SetVariable(index, newValue);
        //    else
        //        throw new ObjectDisposedException("Enumerator");
        //}

        //public void SetVariable(Int32 index, Single newValue)
        //{
        //    if (subEnumerator != null)
        //        subEnumerator.SetVariable(index, newValue);
        //    else
        //        throw new ObjectDisposedException("Enumerator");
        //}

        //public void SetVariable(Int32 index, Int64 newValue)
        //{
        //    if (subEnumerator != null)
        //        subEnumerator.SetVariable(index, newValue);
        //    else
        //        throw new ObjectDisposedException("Enumerator");
        //}

        //public void SetVariable(Int32 index, Int32 newValue)
        //{
        //    if (subEnumerator != null)
        //        subEnumerator.SetVariable(index, newValue);
        //    else
        //        throw new ObjectDisposedException("Enumerator");
        //}

        //public void SetVariable(Int32 index, Int16 newValue)
        //{
        //    if (subEnumerator != null)
        //        subEnumerator.SetVariable(index, newValue);
        //    else
        //        throw new ObjectDisposedException("Enumerator");
        //}

        //public void SetVariable(Int32 index, SByte newValue)
        //{
        //    if (subEnumerator != null)
        //        subEnumerator.SetVariable(index, newValue);
        //    else
        //        throw new ObjectDisposedException("Enumerator");
        //}

        //public void SetVariable(Int32 index, IObjectView newValue)
        //{
        //    if (subEnumerator != null)
        //        subEnumerator.SetVariable(index, newValue);
        //    else
        //        throw new ObjectDisposedException("Enumerator");
        //}

        //public void SetVariable(Int32 index, String newValue)
        //{
        //    if (subEnumerator != null)
        //        subEnumerator.SetVariable(index, newValue);
        //    else
        //        throw new ObjectDisposedException("Enumerator");
        //}

        //public void SetVariable(Int32 index, UInt64 newValue)
        //{
        //    if (subEnumerator != null)
        //        subEnumerator.SetVariable(index, newValue);
        //    else
        //        throw new ObjectDisposedException("Enumerator");
        //}

        //public void SetVariable(Int32 index, UInt32 newValue)
        //{
        //    if (subEnumerator != null)
        //        subEnumerator.SetVariable(index, newValue);
        //    else
        //        throw new ObjectDisposedException("Enumerator");
        //}

        //public void SetVariable(Int32 index, UInt16 newValue)
        //{
        //    if (subEnumerator != null)
        //        subEnumerator.SetVariable(index, newValue);
        //    else
        //        throw new ObjectDisposedException("Enumerator");
        //}

        //public void SetVariable(Int32 index, Byte newValue)
        //{
        //    if (subEnumerator != null)
        //        subEnumerator.SetVariable(index, newValue);
        //    else
        //        throw new ObjectDisposedException("Enumerator");
        //}

        //public void SetVariable(Int32 index, Object newValue)
        //{
        //    if (subEnumerator != null)
        //        subEnumerator.SetVariable(index, newValue);
        //    else
        //        throw new ObjectDisposedException("Enumerator");
        //}

        //public void SetVariables(Object[] newValues)
        //{
        //    if (subEnumerator != null)
        //        subEnumerator.SetVariables(newValues);
        //    else
        //        throw new ObjectDisposedException("Enumerator");
        //}

        //public Int32 VariableCount
        //{
        //    get 
        //    {
        //        if (subEnumerator != null)
        //            return subEnumerator.VariableCount;
        //        else
        //            throw new ObjectDisposedException("Enumerator");
        //    }
        //}

        //public UInt64 UniqueQueryID
        //{
        //    get
        //    {
        //        if (subEnumerator != null)
        //            return subEnumerator.UniqueQueryID;
        //        else
        //            throw new ObjectDisposedException("Enumerator");
        //    }
        //    set
        //    {
        //        if (subEnumerator != null)
        //            subEnumerator.UniqueQueryID = value;
        //        else
        //            throw new ObjectDisposedException("Enumerator");
        //    }
        //}

        //public Boolean HasCodeGeneration
        //{
        //    get
        //    {
        //        if (subEnumerator != null)
        //            return subEnumerator.HasCodeGeneration;
        //        else
        //            throw new ObjectDisposedException("Enumerator");
        //    }
        //    set
        //    {
        //        if (subEnumerator != null)
        //            subEnumerator.HasCodeGeneration = value;
        //        else
        //            throw new ObjectDisposedException("Enumerator");
        //    }
        //}

        //public String GetUniqueName(UInt64 seqNumber)
        //{
        //    if (subEnumerator != null)
        //        return subEnumerator.GetUniqueName(seqNumber);
        //    else
        //        throw new ObjectDisposedException("Enumerator");
        //}

        //public Row CurrentRow
        //{
        //    get 
        //    {
        //        if (subEnumerator != null)
        //            return subEnumerator.CurrentRow;
        //        else
        //            throw new ObjectDisposedException("Enumerator");
        //    }
        //}

        //public VariableArray VarArray
        //{
        //    get 
        //    {
        //        if (subEnumerator != null)
        //            return subEnumerator.VarArray;
        //        else
        //            throw new ObjectDisposedException("Enumerator");
        //    }
        //}

        //public Boolean MoveNextSpecial(Boolean force)
        //{
        //    if (subEnumerator != null)
        //        return subEnumerator.MoveNextSpecial(force);
        //    else
        //        throw new ObjectDisposedException("Enumerator");
        //}

        //public void Reset(Row contextObj)
        //{
        //    if (subEnumerator != null)
        //        subEnumerator.Reset(contextObj);
        //    else
        //        throw new ObjectDisposedException("Enumerator");
        //}

        //public RowTypeBinding RowTypeBinding
        //{
        //    get 
        //    {
        //        if (subEnumerator != null)
        //            return subEnumerator.RowTypeBinding;
        //        else
        //            throw new ObjectDisposedException("Enumerator");
        //    }
        //}

        //public Int32 Depth
        //{
        //    get
        //    {
        //        if (subEnumerator != null)
        //            return subEnumerator.Depth;
        //        else
        //            throw new ObjectDisposedException("Enumerator");
        //    }
        //}

        //public IExecutionEnumerator Clone(RowTypeBinding rowTypeBindClone, VariableArray varArray)
        //{
        //    if (subEnumerator != null)
        //        return new GenericEnumerator<T>(subEnumerator.Clone(rowTypeBindClone, varArray));
        //    else
        //        throw new ObjectDisposedException("Enumerator");
        //}

        //public IExecutionEnumerator CloneCached()
        //{
        //    if (subEnumerator != null)
        //        return new GenericEnumerator<T>(subEnumerator.CloneCached());
        //    else
        //        throw new ObjectDisposedException("Enumerator");
        //}

        //public void AttachToCache(LinkedList<IExecutionEnumerator> fromCache)
        //{
        //    if (subEnumerator != null)
        //        subEnumerator.AttachToCache(fromCache);
        //    else
        //        throw new ObjectDisposedException("Enumerator");
        //}

        //public unsafe void InitVariablesFromBuffer(Byte* queryParamsBuf)
        //{
        //    if (subEnumerator != null)
        //        subEnumerator.InitVariablesFromBuffer(queryParamsBuf);
        //    else
        //        throw new ObjectDisposedException("Enumerator");
        //}

        //public unsafe UInt32 FillupFoundObjectIDs(Byte* results, UInt32 resultsMaxBytes, UInt32* resultsNum, UInt32* flags)
        //{
        //    if (subEnumerator != null)
        //        return subEnumerator.FillupFoundObjectIDs(results, resultsMaxBytes, resultsNum, flags);
        //    else
        //        throw new ObjectDisposedException("Enumerator");
        //}

        //public unsafe void PopulateQueryFlags(UInt32* flags)
        //{
        //    if (subEnumerator != null)
        //        subEnumerator.PopulateQueryFlags(flags);
        //    else
        //        throw new ObjectDisposedException("Enumerator");
        //}

        //public unsafe UInt32 GetInfo(Byte infoType, UInt64 param, Byte* results, UInt32 maxBytes, UInt32* outLenBytes)
        //{
        //    if (subEnumerator != null)
        //        return subEnumerator.GetInfo(infoType, param, results, maxBytes, outLenBytes);
        //    else
        //        throw new ObjectDisposedException("Enumerator");
        //}

        //public unsafe Int32 SaveEnumerator(Byte* keysData, Int32 globalOffset, Boolean saveDynamicDataOnly)
        //{
        //    if (subEnumerator != null)
        //        return subEnumerator.SaveEnumerator(keysData, globalOffset, saveDynamicDataOnly);
        //    else
        //        throw new ObjectDisposedException("Enumerator");
        //}

        //public QueryFlags QueryFlags
        //{
        //    get 
        //    {
        //        if (subEnumerator != null)
        //            return subEnumerator.QueryFlags;
        //        else
        //            throw new ObjectDisposedException("Enumerator");
        //    }
        //}

        //public String QueryString
        //{
        //    get 
        //    {
        //        if (subEnumerator != null)
        //            return subEnumerator.QueryString;
        //        else
        //            throw new ObjectDisposedException("Enumerator");
        //    }
        //}

        //public UInt64 TransactionId
        //{
        //    set 
        //    {
        //        if (subEnumerator != null)
        //            subEnumerator.TransactionId = value;
        //        else
        //            throw new ObjectDisposedException("Enumerator");
        //    }
        //}

        //public void SetFirstOnlyFlag()
        //{
        //    if (subEnumerator != null)
        //        subEnumerator.SetFirstOnlyFlag();
        //    else
        //        throw new ObjectDisposedException("Enumerator");
        //}

        //public void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
        //{
        //    if (subEnumerator != null)
        //        subEnumerator.BuildString(stringBuilder, tabs);
        //    else
        //        throw new ObjectDisposedException("Enumerator");
        //}

        //public void GenerateCompilableCode(CodeGenStringGenerator stringGen)
        //{
        //    if (subEnumerator != null)
        //        subEnumerator.GenerateCompilableCode(stringGen);
        //    else
        //        throw new ObjectDisposedException("Enumerator");
        //}
    }
}
