// ***********************************************************************
// <copyright file="LikeExecEnumerator.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections;
using System.Diagnostics;
using Starcounter.Binding;
using Starcounter.Internal;

namespace Starcounter.Query.Execution
{
    internal class LikeExecEnumerator : ExecutionEnumerator, IExecutionEnumerator
    {
        IExecutionEnumerator[] subExecEnums = null; // Execution enumerators of converted queries.
        IExecutionEnumerator currentExecEnum = null; // Current underlying converted execution enumerator.
        Int32[] likeVarIndexes = null; // Indexes of query variables belonging to LIKE statements.
        Int32 bestEnumIndex = 0; // Indicates best possible execution enumerator variant.
        Boolean enumeratorCreated = false; // Indicates if enumerator was already created.

        internal LikeExecEnumerator(byte nodeId, String sqlQuery,
            IExecutionEnumerator[] subExecEnumsClone,
            Int32[] likeVarIndexRef) : base(nodeId, EnumeratorNodeType.LikeExec, null, null, true,0)
        {
            Debug.Assert(OffsetTuppleLength == 0);

            query = sqlQuery;
            subExecEnums = subExecEnumsClone;
            likeVarIndexes = likeVarIndexRef;

            if (subExecEnums != null)
            {
                bestEnumIndex = subExecEnums.Length - 1;

                // Setting variable array to reference the most efficient execution enumerator.
                variableArray = subExecEnums[bestEnumIndex].VarArray;

                // Selecting appropriate execution enumerator.
                currentExecEnum = subExecEnums[bestEnumIndex];
            }
            else
            {
                // Just creating an empty array.
                variableArray = new VariableArray(0);
            }
        }

        /// <summary>
        /// Checks if string conforms to STARTS WITH syntax.
        /// </summary>
        String IsStartWith(String str)
        {
            if (String.IsNullOrEmpty(str))
                return null;
            Int32 strLastCharIndex = str.Length - 1;
            if (str[strLastCharIndex] == '%')
            {
                String subStr = str.Substring(0, strLastCharIndex);
                foreach (Char c in subStr)
                {
                    if (c == '_' || c == '%')
                        return null;
                }
                return subStr;
            }
            return null;
        }

        /// <summary>
        /// Creates an array of converted START WITH strings.
        /// </summary>
        internal void CreateLikeCombinations<T>(String query, bool slowSql, params Object[] values)
        {
            String[] likeAndStartWith = { " like ", " starts with " };
            String lowerQuery = query.ToLower();
            String[] splittedQuery = lowerQuery.Split(new String[] { likeAndStartWith[0] }, StringSplitOptions.RemoveEmptyEntries);

            Int32 expectedLikesNum = splittedQuery.Length - 1;
            likeVarIndexes = new Int32[expectedLikesNum];
            Int32 likeVarNum = 0, curVarNum = 0;

            // Going through all question marks and identifying those that belong to LIKE.
            foreach (String s in splittedQuery)
            {
                Boolean varFound = false;
                foreach (Char c in s)
                {
                    if (c == '?')
                    {
                        if ((!varFound) && (s != splittedQuery[0]))
                        {
                            varFound = true;
                            likeVarIndexes[likeVarNum] = curVarNum;
                            likeVarNum++;
                        }
                        curVarNum++;
                    }
                }
            }

            // Testing if everything is correct with variable numbers.
            if (likeVarNum != expectedLikesNum)
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Unwanted behavior in LIKE enumerator");

            // Calculating number of LIKE/STARTWITH combinations.
            Int32 combNum = 1;
            for (Int32 i = 0; i < expectedLikesNum; i++)
                combNum = combNum << 1;

            // Allocating strings to cover all combinations.
            String[] convertedQueries = new String[combNum];
            var tempExecEnums = new IExecutionEnumerator[combNum];
            int nrTemps = 0;

            // Creating combined strings.
            for (Int32 i = 0; i < combNum; i++)
            {
                // Creating bit representation of combination.
                Int32[] combBits = new Int32[expectedLikesNum];
                for (Int32 k = 0; k < expectedLikesNum; k++)
                    combBits[k] = (i >> k) & 1;

                // Combining final converted string.
                convertedQueries[i] = " " + splittedQuery[0];
                for (Int32 k = 0; k < expectedLikesNum; k++)
                    convertedQueries[i] += likeAndStartWith[combBits[k]] + splittedQuery[k + 1];

                // Creating query in cache and obtaining enumerator.
                try {
                    tempExecEnums[i] = Scheduler.GetInstance().SqlEnumCache.GetCachedEnumerator<T>(convertedQueries[i], slowSql, values);
                    nrTemps++;
                } catch {}
            }
            combNum = nrTemps;
            subExecEnums = new IExecutionEnumerator[combNum];
            for (int i = 0; i < combNum; i++)
                subExecEnums[i] = tempExecEnums[i];
            // Setting variable array to reference most efficient execution enumerator.
            bestEnumIndex = combNum - 1;
            variableArray = subExecEnums[bestEnumIndex].VarArray;
            rowTypeBinding = subExecEnums[bestEnumIndex].RowTypeBinding;
            projectionTypeCode = subExecEnums[bestEnumIndex].ProjectionTypeCode;

            // Selecting appropriate execution enumerator.
            currentExecEnum = subExecEnums[bestEnumIndex];
        }

        /// <summary>
        /// Gets the type binding of the Row.
        /// </summary>
        override public RowTypeBinding RowTypeBinding
        {
            get
            {
                return currentExecEnum.RowTypeBinding;
            }
        }

        /// <summary>
        /// If the projection is a singleton, then the DbTypeCode of that singleton, otherwise null.
        /// </summary>
        public override Nullable<DbTypeCode> ProjectionTypeCode {
            get {
                return currentExecEnum.ProjectionTypeCode;
            }
        }

        /// <summary>
        /// The type binding of the resulting objects of the query.
        /// </summary>
        public ITypeBinding TypeBinding
        {
            get
            {
                return currentExecEnum.TypeBinding;
            }
        }

        public Int32 Depth
        {
            get
            {
                return currentExecEnum.Depth;
            }
        }

        Object IEnumerator.Current
        {
            get
            {
                return Current;
            }
        }

        // Returns current proxy object.
        public new Object Current
        {
            get
            {
                return currentExecEnum.Current;
            }
        }

        public Row CurrentRow
        {
            get
            {
                return currentExecEnum.CurrentRow;
            }
        }

        public Boolean MoveNext()
        {
            if (!enumeratorCreated)
            {
                Int32 convQueryNum = 0;

                // Check string variables for being LIKE parameters.
                for (Int32 i = 0; i < likeVarIndexes.Length; i++)
                {
                    StringVariable strVar = variableArray.GetElement(likeVarIndexes[i]) as StringVariable;
                    String startWith = IsStartWith(strVar.Value);

                    // Checking if variable conforms the 'STARTS WITH' rules.
                    if (startWith != null)
                    {
                        strVar.Value = startWith;
                        convQueryNum |= (1 << i);
                    }
                }

                // Selecting appropriate execution enumerator.
                currentExecEnum = subExecEnums[convQueryNum];

                // Checking if we jumped from the best case.
                if (convQueryNum != bestEnumIndex)
                {
                    // Prolonging query parameters.
                    variableArray.ProlongValues(currentExecEnum);
                }

                // Setting the recreation key and recreation flag.
                //unsafe { currentExecEnum.VarArray.RecreationKeyData = variableArray.RecreationKeyData; }
                currentExecEnum.VarArray.FailedToRecreateObject = variableArray.FailedToRecreateObject;

                // Attaching to current transaction.
                currentExecEnum.TransactionId = variableArray.TransactionId;

                // We created the enumerator.
                enumeratorCreated = true;
            }

            return currentExecEnum.MoveNext();
        }

        public Boolean MoveNextSpecial(Boolean force)
        {
            throw new NotImplementedException();
        }

        public unsafe short SaveEnumerator(ref SafeTupleWriterBase64 enumerators, short expectedNodeId) {
            return currentExecEnum.SaveEnumerator(ref enumerators, expectedNodeId);
        }

#if false
        /// <summary>
        /// Saves the underlying enumerator state.
        /// </summary>
        public unsafe UInt16 SaveEnumerator(Byte* keysData, UInt16 globalOffset, Boolean saveDynamicDataOnly)
        {
            return currentExecEnum.SaveEnumerator(keysData, globalOffset, saveDynamicDataOnly);
        }
#endif

        /// <summary>
        /// Depending on query flags, populates the flags value.
        /// </summary>
        public unsafe override void PopulateQueryFlags(UInt32* flags)
        {
            currentExecEnum.PopulateQueryFlags(flags);

            // Like enumerator can always have a post filter.
            (*flags) |= SqlConnectivityInterface.FLAG_POST_MANAGED_FILTER;
        }

        /// <summary>
        /// Resets the enumerator with a context object.
        /// </summary>
        /// <param name="obj">Context object from another enumerator.</param>
        public override void Reset(Row obj)
        {
            // Resetting but not disposing (so its not returned back to cache).
            currentExecEnum.Reset(null);
            counter = 0;

            // Setting variable array to reference the most efficient execution enumerator.
            variableArray = subExecEnums[bestEnumIndex].VarArray;

            enumeratorCreated = false;
        }

        public override IExecutionEnumerator Clone(RowTypeBinding typeBindingClone, VariableArray varArrClone)
        {
            IExecutionEnumerator[] subExecEnumsClone = null;
            if (subExecEnums != null)
            {
                Int32 combNum = subExecEnums.Length;
                subExecEnumsClone = new IExecutionEnumerator[combNum];
                for (Int32 i = 0; i < combNum; i++)
                    subExecEnumsClone[i] = subExecEnums[i].CloneCached();
            }

            return new LikeExecEnumerator(nodeId, query, subExecEnumsClone, likeVarIndexes);
        }

        public override void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
        {
            currentExecEnum.BuildString(stringBuilder, tabs);
        }

        /// <summary>
        /// Generates compilable code representation of this data structure.
        /// </summary>
        public void GenerateCompilableCode(CodeGenStringGenerator stringGen)
        {
            currentExecEnum.GenerateCompilableCode(stringGen);
        }

        /// <summary>
        /// Gets the unique name for this enumerator.
        /// </summary>
        public String GetUniqueName(UInt64 seqNumber)
        {
            return currentExecEnum.GetUniqueName(seqNumber);
        }
    
        public Boolean IsAtRecreatedKey { get { throw ErrorCode.ToException(Error.SCERRNOTIMPLEMENTED); } }
        public Boolean StayAtOffsetkey { get { throw ErrorCode.ToException(Error.SCERRNOTIMPLEMENTED); } set { throw ErrorCode.ToException(Error.SCERRNOTIMPLEMENTED); } }
        public Boolean UseOffsetkey { get { throw ErrorCode.ToException(Error.SCERRNOTIMPLEMENTED); } set { throw ErrorCode.ToException(Error.SCERRNOTIMPLEMENTED); } }
    }
}
