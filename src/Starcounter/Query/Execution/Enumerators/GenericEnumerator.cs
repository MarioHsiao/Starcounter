
using Starcounter.Binding;
using System;
using System.Collections.Generic;

namespace Starcounter.Query.Execution
{
    internal class GenericEnumerator<T> : IExecutionEnumerator
    {
        IExecutionEnumerator subEnumerator;

        internal GenericEnumerator(IExecutionEnumerator subEnumerator)
        {
            if (subEnumerator == null)
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect subEnumerator.");
            
            this.subEnumerator = subEnumerator;
        }

        public Boolean MoveNext()
        {
            if (subEnumerator != null)
                return subEnumerator.MoveNext();

            throw new ObjectDisposedException("Enumerator");
        }

        public dynamic Current
        {
            get 
            {
                if (subEnumerator != null)
                {
                    //if (subEnumerator.Current != null || !typeof(T).IsValueType || Nullable.GetUnderlyingType(typeof(T)) != null)
                    if (subEnumerator.Current != null || Nullable.GetUnderlyingType(typeof(T)) != null)
                        return (T)subEnumerator.Current;
                    else
                        return default(T);
                }

                throw new ObjectDisposedException("Enumerator");
            }
        }

        public void Reset()
        {
            if (subEnumerator != null)
                subEnumerator.Reset();

            throw new ObjectDisposedException("Enumerator");
        }

        public void Dispose()
        {
            if (subEnumerator != null)
            {
                subEnumerator.Dispose();
                subEnumerator = null;
            }
        }

        public String Query
        {
            get 
            {
                if (subEnumerator != null)
                    return subEnumerator.Query;

                throw new ObjectDisposedException("Enumerator");
            }
        }

        public ITypeBinding TypeBinding
        {
            get 
            {
                if (subEnumerator != null)
                    return subEnumerator.TypeBinding;

                throw new ObjectDisposedException("Enumerator");
            }
        }

        public Nullable<DbTypeCode> ProjectionTypeCode
        {
            get 
            {
                if (subEnumerator != null)
                    return subEnumerator.ProjectionTypeCode;

                throw new ObjectDisposedException("Enumerator");
            }
        }

        public Int64 Counter
        {
            get 
            {
                if (subEnumerator != null)
                    return subEnumerator.Counter;

                throw new ObjectDisposedException("Enumerator");
            }
        }

        public Byte[] GetOffsetKey()
        {
            if (subEnumerator != null)
                return subEnumerator.GetOffsetKey();

            throw new ObjectDisposedException("Enumerator");
        }

        Object System.Collections.IEnumerator.Current
        {
            get 
            { 
                return Current; 
            }
        }

        Boolean System.Collections.IEnumerator.MoveNext()
        {
            return MoveNext();
        }

        void System.Collections.IEnumerator.Reset()
        {
            Reset();
        }

        public void SetVariableToNull(Int32 index)
        {
            if (subEnumerator != null)
                subEnumerator.SetVariableToNull(index);

            throw new ObjectDisposedException("Enumerator");
        }

        public void SetVariable(Int32 index, Binary newValue)
        {
            if (subEnumerator != null)
                subEnumerator.SetVariable(index, newValue);

            throw new ObjectDisposedException("Enumerator");
        }

        public void SetVariable(Int32 index, Byte[] newValue)
        {
            if (subEnumerator != null)
                subEnumerator.SetVariable(index, newValue);

            throw new ObjectDisposedException("Enumerator");
        }

        public void SetVariable(Int32 index, Boolean newValue)
        {
            if (subEnumerator != null)
                subEnumerator.SetVariable(index, newValue);

            throw new ObjectDisposedException("Enumerator");
        }

        public void SetVariable(Int32 index, DateTime newValue)
        {
            if (subEnumerator != null)
                subEnumerator.SetVariable(index, newValue);

            throw new ObjectDisposedException("Enumerator");
        }

        public void SetVariable(Int32 index, Decimal newValue)
        {
            if (subEnumerator != null)
                subEnumerator.SetVariable(index, newValue);

            throw new ObjectDisposedException("Enumerator");
        }

        public void SetVariable(Int32 index, Double newValue)
        {
            if (subEnumerator != null)
                subEnumerator.SetVariable(index, newValue);

            throw new ObjectDisposedException("Enumerator");
        }

        public void SetVariable(Int32 index, Single newValue)
        {
            if (subEnumerator != null)
                subEnumerator.SetVariable(index, newValue);

            throw new ObjectDisposedException("Enumerator");
        }

        public void SetVariable(Int32 index, Int64 newValue)
        {
            if (subEnumerator != null)
                subEnumerator.SetVariable(index, newValue);

            throw new ObjectDisposedException("Enumerator");
        }

        public void SetVariable(Int32 index, Int32 newValue)
        {
            if (subEnumerator != null)
                subEnumerator.SetVariable(index, newValue);

            throw new ObjectDisposedException("Enumerator");
        }

        public void SetVariable(Int32 index, Int16 newValue)
        {
            if (subEnumerator != null)
                subEnumerator.SetVariable(index, newValue);

            throw new ObjectDisposedException("Enumerator");
        }

        public void SetVariable(Int32 index, SByte newValue)
        {
            if (subEnumerator != null)
                subEnumerator.SetVariable(index, newValue);

            throw new ObjectDisposedException("Enumerator");
        }

        public void SetVariable(Int32 index, IObjectView newValue)
        {
            if (subEnumerator != null)
                subEnumerator.SetVariable(index, newValue);

            throw new ObjectDisposedException("Enumerator");
        }

        public void SetVariable(Int32 index, String newValue)
        {
            if (subEnumerator != null)
                subEnumerator.SetVariable(index, newValue);

            throw new ObjectDisposedException("Enumerator");
        }

        public void SetVariable(Int32 index, UInt64 newValue)
        {
            if (subEnumerator != null)
                subEnumerator.SetVariable(index, newValue);

            throw new ObjectDisposedException("Enumerator");
        }

        public void SetVariable(Int32 index, UInt32 newValue)
        {
            if (subEnumerator != null)
                subEnumerator.SetVariable(index, newValue);

            throw new ObjectDisposedException("Enumerator");
        }

        public void SetVariable(Int32 index, UInt16 newValue)
        {
            if (subEnumerator != null)
                subEnumerator.SetVariable(index, newValue);

            throw new ObjectDisposedException("Enumerator");
        }

        public void SetVariable(Int32 index, Byte newValue)
        {
            if (subEnumerator != null)
                subEnumerator.SetVariable(index, newValue);

            throw new ObjectDisposedException("Enumerator");
        }

        public void SetVariable(Int32 index, Object newValue)
        {
            if (subEnumerator != null)
                subEnumerator.SetVariable(index, newValue);

            throw new ObjectDisposedException("Enumerator");
        }

        public void SetVariables(Object[] newValues)
        {
            if (subEnumerator != null)
                subEnumerator.SetVariables(newValues);

            throw new ObjectDisposedException("Enumerator");
        }

        public Int32 VariableCount
        {
            get 
            {
                if (subEnumerator != null)
                    return subEnumerator.VariableCount;

                throw new ObjectDisposedException("Enumerator");
            }
        }

        public UInt64 UniqueQueryID
        {
            get
            {
                if (subEnumerator != null)
                    return subEnumerator.UniqueQueryID;

                throw new ObjectDisposedException("Enumerator");
            }
            set
            {
                if (subEnumerator != null)
                    subEnumerator.UniqueQueryID = value;

                throw new ObjectDisposedException("Enumerator");
            }
        }

        public Boolean HasCodeGeneration
        {
            get
            {
                if (subEnumerator != null)
                    return subEnumerator.HasCodeGeneration;

                throw new ObjectDisposedException("Enumerator");
            }
            set
            {
                if (subEnumerator != null)
                    subEnumerator.HasCodeGeneration = value;

                throw new ObjectDisposedException("Enumerator");
            }
        }

        public String GetUniqueName(UInt64 seqNumber)
        {
            if (subEnumerator != null)
                return subEnumerator.GetUniqueName(seqNumber);

            throw new ObjectDisposedException("Enumerator");
        }

        public Row CurrentRow
        {
            get 
            {
                if (subEnumerator != null)
                    return subEnumerator.CurrentRow;

                throw new ObjectDisposedException("Enumerator");
            }
        }

        public VariableArray VarArray
        {
            get 
            {
                if (subEnumerator != null)
                    return subEnumerator.VarArray;

                throw new ObjectDisposedException("Enumerator");
            }
        }

        public Boolean MoveNextSpecial(Boolean force)
        {
            if (subEnumerator != null)
                return subEnumerator.MoveNextSpecial(force);

            throw new ObjectDisposedException("Enumerator");
        }

        public void Reset(Row contextObj)
        {
            if (subEnumerator != null)
                subEnumerator.Reset(contextObj);

            throw new ObjectDisposedException("Enumerator");
        }

        public RowTypeBinding RowTypeBinding
        {
            get 
            {
                if (subEnumerator != null)
                    return subEnumerator.RowTypeBinding;

                throw new ObjectDisposedException("Enumerator");
            }
        }

        public Int32 Depth
        {
            get
            {
                if (subEnumerator != null)
                    return subEnumerator.Depth;

                throw new ObjectDisposedException("Enumerator");
            }
        }

        public IExecutionEnumerator Clone(RowTypeBinding rowTypeBindClone, VariableArray varArray)
        {
            if (subEnumerator != null)
                return new GenericEnumerator<T>(subEnumerator.Clone(rowTypeBindClone, varArray));

            throw new ObjectDisposedException("Enumerator");
        }

        public IExecutionEnumerator CloneCached()
        {
            if (subEnumerator != null)
                return new GenericEnumerator<T>(subEnumerator.CloneCached());

            throw new ObjectDisposedException("Enumerator");
        }

        public void AttachToCache(LinkedList<IExecutionEnumerator> fromCache)
        {
            if (subEnumerator != null)
                subEnumerator.AttachToCache(fromCache);

            throw new ObjectDisposedException("Enumerator");
        }

        public unsafe void InitVariablesFromBuffer(Byte* queryParamsBuf)
        {
            if (subEnumerator != null)
                subEnumerator.InitVariablesFromBuffer(queryParamsBuf);

            throw new ObjectDisposedException("Enumerator");
        }

        public unsafe UInt32 FillupFoundObjectIDs(Byte* results, UInt32 resultsMaxBytes, UInt32* resultsNum, UInt32* flags)
        {
            if (subEnumerator != null)
                return subEnumerator.FillupFoundObjectIDs(results, resultsMaxBytes, resultsNum, flags);

            throw new ObjectDisposedException("Enumerator");
        }

        public unsafe void PopulateQueryFlags(UInt32* flags)
        {
            if (subEnumerator != null)
                subEnumerator.PopulateQueryFlags(flags);

            throw new ObjectDisposedException("Enumerator");
        }

        public unsafe UInt32 GetInfo(Byte infoType, UInt64 param, Byte* results, UInt32 maxBytes, UInt32* outLenBytes)
        {
            if (subEnumerator != null)
                return subEnumerator.GetInfo(infoType, param, results, maxBytes, outLenBytes);

            throw new ObjectDisposedException("Enumerator");
        }

        public unsafe Int32 SaveEnumerator(Byte* keysData, Int32 globalOffset, Boolean saveDynamicDataOnly)
        {
            if (subEnumerator != null)
                return subEnumerator.SaveEnumerator(keysData, globalOffset, saveDynamicDataOnly);

            throw new ObjectDisposedException("Enumerator");
        }

        public QueryFlags QueryFlags
        {
            get 
            {
                if (subEnumerator != null)
                    return subEnumerator.QueryFlags;

                throw new ObjectDisposedException("Enumerator");
            }
        }

        public String QueryString
        {
            get 
            {
                if (subEnumerator != null)
                    return subEnumerator.QueryString;

                throw new ObjectDisposedException("Enumerator");
            }
        }

        public UInt64 TransactionId
        {
            set 
            {
                if (subEnumerator != null)
                    subEnumerator.TransactionId = value;

                throw new ObjectDisposedException("Enumerator");
            }
        }

        public void SetFirstOnlyFlag()
        {
            if (subEnumerator != null)
                subEnumerator.SetFirstOnlyFlag();

            throw new ObjectDisposedException("Enumerator");
        }

        public void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
        {
            if (subEnumerator != null)
                subEnumerator.BuildString(stringBuilder, tabs);

            throw new ObjectDisposedException("Enumerator");
        }

        public void GenerateCompilableCode(CodeGenStringGenerator stringGen)
        {
            if (subEnumerator != null)
                subEnumerator.GenerateCompilableCode(stringGen);

            throw new ObjectDisposedException("Enumerator");
        }
    }
}
