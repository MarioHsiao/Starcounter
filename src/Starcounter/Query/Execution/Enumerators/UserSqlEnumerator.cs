using Starcounter;
using Starcounter.Binding;
using Starcounter.Query.Sql;
using Sc.Server.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using Starcounter.LucentObjects;
using System.Runtime.InteropServices;
using Starcounter.Internal;

namespace Starcounter.Query.Execution
{
    internal class UserSqlEnumProperties
    {
        // Indicates if query has sorting.
        public Boolean HasSorting = false;

        // Indicates if query has managed post filtering.
        public Boolean HasPostFiltering = false;

        // Fetch statement information.
        public Int32 FetchVarIndex = -1;

        // Recreation key information.
        public Int32 RecreationKeyVarIndex = -1;

        // Reference to cache this enumerator is coming from.
        public ClientQueryCache QueryCacheRef = null;

        // Query flags, such as if it includes sorting, fetch statement, projection, etc.
        public UInt32 SpecQueryFlags = 0;

        // Maximum number of hits to fetch per page.
        public UInt32 MaxHitsPerPage = 0;

        // Maximum size of results in bytes.
        public UInt32 ResultsMaxBytes = 0;

        // Shared empty buffer reference.
        public Byte[] EmptyBuffer = new Byte[] { 4, 0, 0, 0 };
    }

    internal class UserSqlEnumerator : ExecutionEnumerator, IExecutionEnumerator
    {
        // Minimum number of hits per page.
        const Int32 MIN_HITS_PER_PAGE = 128;

        // Indicates if underlying iterator was already created.
        Boolean iteratorCreated = false;

        // Represents current result object.
        Entity currentProxyObject = null;

        // Array containing query parameters data.
        ByteArrayBuilder queryParams = null;

        // Contains object ETIs, IDs, class indexes.
        UInt64[] results = null;

        // Buffer containing recreation key.
        Byte[] recreationKey = null;

        // How many hits should be fetched per page.
        UInt32 fetchPerPage = 0;

        // Hits found during the last fetch.
        UInt32 lastNumberOfHits = 0;

        // How many hits to fetch specified by user.
        Int32 userFetchNum = -1;

        // Counter of hits within page.
        Int32 counterWithinPage = 0;

        // Indicates if more results should be fetched.
        Boolean moreToFetch = false;

        // Holds shared SQL properties among same queries.
        UserSqlEnumProperties sharedProps = null;

        // Byte offset for object data.
        Int32 objectDataOffset = 0;

        // Indicates if last MoveNext was successful and object was fetched.
        Boolean objectWasFetched = false;

        // Some enumerator specific flags, e.g. if only the first result should be fetched.
        UInt32 enumSpecificFlags = 0;

        internal UserSqlEnumerator(
            String sqlQuery,
            UInt64 uniqueSqlQueryID,
            VariableArray varArr,
            ClientQueryCache queryCache,
            UInt32 queryFlags,
            UserSqlEnumProperties props)
            : base(varArr)
        {
            if (sqlQuery == null)
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect query.");

            query = sqlQuery;
            uniqueQueryID = uniqueSqlQueryID;
            variableArray = varArr;
            sharedProps = props;

            // Checking if its newly created(not cached) enumerator
            // that is just used as an SQL cache cloning copy.
            if (sharedProps == null)
            {
                sharedProps = new UserSqlEnumProperties();
                sharedProps.QueryCacheRef = queryCache;
                sharedProps.SpecQueryFlags = queryFlags;

                // Checking if query has sorting.
                if ((sharedProps.SpecQueryFlags & SqlConnectivityInterface.FLAG_HAS_SORTING) != 0)
                {
                    sharedProps.HasSorting = true;
                }

                // Checking if query has a post managed filter.
                if ((sharedProps.SpecQueryFlags & SqlConnectivityInterface.FLAG_POST_MANAGED_FILTER) != 0)
                {
                    sharedProps.HasPostFiltering = true;
                }

                if ((sharedProps.SpecQueryFlags & SqlConnectivityInterface.FLAG_FETCH_VARIABLE) != 0)
                {
                    Int32 varIndex;
                    UInt32 lenBytes;
                    unsafe
                    {
                        UInt32 err = SqlConnectivityInterface.SqlConn_GetInfo(
                            SqlConnectivityInterface.GET_FETCH_VARIABLE,
                            uniqueQueryID,
                            (Byte*)&varIndex,
                            4,
                            &lenBytes);

                        if (err != 0)
                            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Error fetching query fetch variable index.");
                    }

                    sharedProps.FetchVarIndex = varIndex;
                }

                if ((sharedProps.SpecQueryFlags & SqlConnectivityInterface.FLAG_RECREATION_KEY_VARIABLE) != 0)
                {
                    Int32 varIndex;
                    UInt32 lenBytes;
                    unsafe
                    {
                        UInt32 err = SqlConnectivityInterface.SqlConn_GetInfo(
                            SqlConnectivityInterface.GET_RECREATION_KEY_VARIABLE,
                            uniqueQueryID,
                            (Byte*)&varIndex,
                            4,
                            &lenBytes);

                        if (err != 0)
                            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Error fetching query offset key variable index.");
                    }

                    sharedProps.RecreationKeyVarIndex = varIndex;
                }

                // Setting max hits number and corresponding byte size.
                if (sharedProps.HasSorting)
                {
                    sharedProps.MaxHitsPerPage = SqlConnectivityInterface.MAX_HITS_PER_PAGE_SORTING;
                    sharedProps.ResultsMaxBytes = 8 * 3 * sharedProps.MaxHitsPerPage + 8;
                }
                else
                {
                    sharedProps.MaxHitsPerPage = StarcounterEnvironment.SpecialVariables.ScConnMaxHitsPerPage();
                    sharedProps.ResultsMaxBytes = 8 * 3 * sharedProps.MaxHitsPerPage + 8;
                }
            }
            else // Indicates that cloning used enumerator.
            {
                // Creating query params.
                queryParams = new ByteArrayBuilder();

                // Allocating data for recreation key.
                recreationKey = new Byte[SqlConnectivityInterface.RECREATION_KEY_MAX_BYTES];

                // Voiding the recreation key.
                recreationKey[0] = 4;
                recreationKey[1] = 0;
                recreationKey[2] = 0;
                recreationKey[3] = 0;

                // Allocating buffer for results.
                results = new UInt64[sharedProps.ResultsMaxBytes / 8];
            }

            // Checking if we have sorting.
            if (sharedProps.HasSorting)
            {
                fetchPerPage = sharedProps.MaxHitsPerPage;
            }
            else
            {
                fetchPerPage = MIN_HITS_PER_PAGE;
            }
        }

        /// <summary>
        /// The type binding of the resulting objects of the query.
        /// </summary>
        public ITypeBinding TypeBinding
        {
            get
            {
                throw new NotImplementedException();
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

        // Returns current proxy object.
        public dynamic Current
        {
            get
            {
                // Checking if we fetched any objects.
                if (objectWasFetched)
                    return currentProxyObject;

                throw new InvalidOperationException("Enumerator has not started or has already finished.");
            }
        }

        public CompositeObject CurrentCompositeObject
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Gets offset key if its possible.
        /// </summary>
        public override Byte[] GetOffsetKey()
        {
            // Checking if any object was fetched.
            if (!objectWasFetched)
                return null;

            // Checking if we have a FETCH statement within query.
            if (sharedProps.FetchVarIndex < 0)
                throw ErrorCode.ToException(Error.SCERROFFSETKEYOUTOFPROCESSFETCH);

            // Getting the length of current recreation key.
            Int32 recrKeyLenBytes = BitConverter.ToInt32(recreationKey, 0);

            // Checking if recreation key contains any data.
            if (recrKeyLenBytes > 4)
            {
                // Allocating space for offset key.
                Byte[] offsetKey = new Byte[recrKeyLenBytes];

                // Copying key into created buffer.
                Buffer.BlockCopy(recreationKey, 0, offsetKey, 0, recrKeyLenBytes);

                // Returning the key.
                return offsetKey;
            }

            // Was not able to fetch the key.
            return null;
        }

        /// <summary>
        /// Fetches new results for this SQL query.
        /// </summary>
        void GetResults(Byte[] queryParamsBuffer)
        {
            UInt32 errCode = 0, _resultsNum = fetchPerPage, _flags = enumSpecificFlags;

            // Checking if we want to fetch certain amount.
            if ((userFetchNum > 0) && (userFetchNum < sharedProps.MaxHitsPerPage))
                _resultsNum = (UInt32)userFetchNum;

            // Calling native interface function to fill up objects buffer.
            unsafe
            {
                fixed (Byte* pQueryParams = queryParamsBuffer, pRecreationKey = recreationKey)
                {
                    fixed (UInt64* pResults = results)
                    {
                        //Application.Profiler.Start("Client-GetResults", 1);

                        errCode = SqlConnectivityInterface.SqlConn_GetResults(
                            uniqueQueryID,
                            pQueryParams,
                            (Byte*)pResults,
                            sharedProps.ResultsMaxBytes,
                            &_resultsNum,
                            pRecreationKey,
                            SqlConnectivityInterface.RECREATION_KEY_MAX_BYTES,
                            &_flags);

                        //Application.Profiler.Stop(1);
                    }
                }
            }

            // Checking for error code and translating it.
            if (errCode != 0)
                SqlConnectivity.ThrowConvertedServerError(errCode);

            lastNumberOfHits = _resultsNum;
            moreToFetch = ((_flags & SqlConnectivityInterface.FLAG_MORE_RESULTS) != 0);

            // Increasing the page size.
            if (fetchPerPage < sharedProps.MaxHitsPerPage)
                fetchPerPage += fetchPerPage;

            // Reseting counters.
            counterWithinPage = 0;
            objectDataOffset = 0;
        }

        /// <summary>
        /// Creates iterator which than used in MoveNext.
        /// </summary>
        private void InitIterator()
        {
            VerifyOwnedTransaction();

            // Finalizing variables buffer.
            queryParams.GetBufferCached();

            // Getting first results page.
            GetResults(queryParams.DataBuffer);

            // Indicating that enumerator has been created.
            iteratorCreated = true;
        }

        public Boolean MoveNext()
        {
            // Checking if we have already fetched needed amount.
            if (userFetchNum == 0)
                return false;

            // Fetching either all results or the first page.
            if (!iteratorCreated)
                InitIterator();

            // Checking if new result page needed.
            if ((moreToFetch) && (counterWithinPage >= lastNumberOfHits) && (!sharedProps.HasSorting))
            {
                Byte[] _queryParams = null;

                // Checking if we need to pass query parameters buffer.
                if (!sharedProps.HasPostFiltering)
                    _queryParams = sharedProps.EmptyBuffer;
                else
                    _queryParams = queryParams.DataBuffer;

                // Fetching next results page.
                GetResults(_queryParams);
            }

            // Checking if more results exist.
            if (counterWithinPage < lastNumberOfHits)
            {
                // Creating proxy object based on retrieved data.
                currentProxyObject = CreateProxyEntityObject(
                    results[++objectDataOffset],
                    results[++objectDataOffset],
                    (UInt16)results[++objectDataOffset]);

                // Manipulating with counters.
                userFetchNum--;
                counterWithinPage++;
                objectWasFetched = true;

                return true;
            }

            objectWasFetched = false;
            return false;
        }

        /// <summary>
        /// Verifies that the enumerator is accessed in the correct transaction.
        /// </summary>
        private void VerifyOwnedTransaction()
        {
#if false // TODO EOH2: Transaction id
            UInt64 transactionId = variableArray.TransactionId;
            if (transactionId == 0)
            {
                // Implicit transaction -> explicit transaction
                if (Transaction.Current != null)
                {
                    throw ErrorCode.ToException(Error.SCERRITERATORNOTOWNED);
                }
            }
            else
            {
                // Explicit transaction -> another explicit or implicit transaction
                Transaction ct = Transaction.Current;
                if (ct == null || ct.TransactionId != transactionId)
                {
                    throw ErrorCode.ToException(Error.SCERRITERATORNOTOWNED);
                }
            }
#endif
        }

        // Creates proxy entity object from ETI, OID and CurrentCCI.
        Entity CreateProxyEntityObject(UInt64 eti, UInt64 oid, UInt16 currentCCI)
        {
            // Checking if we have a null object.
            if ((eti == 0) && (oid == 0))
                return null;

            TypeBinding binding = Sc.Server.Binding.TypeRepository.GetTypeBinding(currentCCI);
            try
            {
                return binding.NewInstance(eti, oid) as Entity;
            }
            catch (DbException dbException)
            {
                if (dbException.ErrorCode != Error.SCERRINSTANTIATEBINDINGNOTYPE)
                    throw;

                // The exception indicates the binding was never attached to a client
                // type and we are executing in a client/external process. Lets give the
                // user a really nice exception message, instructing what to do to
                // resolve this.

                throw ErrorCode.ToException(
                    Error.SCERRCLIENTENTITYTYPEUNKNOWN,
                    dbException,
                    string.Empty,
                    binding.Name,
                    Environment.NewLine,
                    this.Query
                    );
            }
        }

        /// <summary>
        /// Sets the fetch first only flag.
        /// </summary>
        public override void SetFirstOnlyFlag()
        {
            enumSpecificFlags = SqlConnectivityInterface.FLAG_LAST_FETCH;
        }

        /// <summary>
        /// Resets the enumerator with a context object.
        /// </summary>
        /// <param name="obj">Context object from another enumerator.</param>
        public override void Reset(CompositeObject obj)
        {
            iteratorCreated = false;
            currentProxyObject = null;
            queryParams.ResetCached();

            // Voiding the recreation key.
            recreationKey[0] = 4;
            recreationKey[1] = 0;
            recreationKey[2] = 0;
            recreationKey[3] = 0;

            if (!sharedProps.HasSorting)
                fetchPerPage = MIN_HITS_PER_PAGE;

            lastNumberOfHits = 0;

            // Reseting the fetch number.
            userFetchNum = -1;

            // Reseting counters.
            counterWithinPage = 0;
            objectDataOffset = 0;

            moreToFetch = false;
            objectWasFetched = false;
            enumSpecificFlags = 0;
        }

        public override IExecutionEnumerator Clone(CompositeTypeBinding typeBindingClone, VariableArray varArrClone)
        {
            // Basically creating new client enumerator.
            return new UserSqlEnumerator(query, uniqueQueryID, varArrClone, null, 0, sharedProps);
        }

        public override void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
        {
            stringBuilder.Append(tabs, SqlConnectivity.GetServerStatusString(SqlConnectivityInterface.GET_ENUMERATOR_EXEC_PLAN, uniqueQueryID));
        }

        /// <summary>
        /// Does the continuous object ETIs and IDs fill up into the dedicated buffer.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="bufLenBytes"></param>
        /// <returns></returns>
        public override unsafe UInt32 FillupFoundObjectIDs(Byte* results, UInt32 resultsMaxBytes, UInt32* resultsNum, UInt32* flags)
        {
            // Client side enumerator does not need to implement this function.
            throw new NotImplementedException();
        }

        /// <summary>
        /// Generates compilable code representation of this data structure.
        /// </summary>
        public void GenerateCompilableCode(CodeGenStringGenerator stringGen)
        {
            throw new NotImplementedException();
        }

        public Boolean MoveNextSpecial(Boolean force)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Saves the underlying enumerator state.
        /// </summary>
        public unsafe Int32 SaveEnumerator(Byte* keysData, Int32 globalOffset, Boolean saveDynamicDataOnly)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the unique name for this enumerator.
        /// </summary>
        public String GetUniqueName(UInt64 seqNumber)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sets new query fetch size.
        /// </summary>
        void SetFetchSize(Int32 index, Int32 fetchNum)
        {
            // Checking if its a fetch variable.
            if (sharedProps.FetchVarIndex == index)
            {
                // Checking input correctness.
                if (fetchNum <= 0)
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Fetch size should be a positive number.");

                userFetchNum = fetchNum;
            }
        }

        /// <summary>
        /// Sets a value to an SQL variable.
        /// </summary>
        /// <param name="index">The order number of the variable starting at 0.</param>
        /// <param name="value">The new value of the variable.</param>
        public override void SetVariable(Int32 index, SByte value)
        {
            queryParams.AppendNonNullValue((Int64)value, SqlConnectivityInterface.QUERY_VARTYPE_INT);
            SetFetchSize(index, value);
        }

        public override void SetVariable(Int32 index, Int16 value)
        {
            queryParams.AppendNonNullValue((Int64)value, SqlConnectivityInterface.QUERY_VARTYPE_INT);
            SetFetchSize(index, value);
        }

        public override void SetVariable(Int32 index, Int32 value)
        {
            queryParams.AppendNonNullValue((Int64)value, SqlConnectivityInterface.QUERY_VARTYPE_INT);
            SetFetchSize(index, value);
        }

        public override void SetVariable(Int32 index, Int64 value)
        {
            queryParams.AppendNonNullValue(value, SqlConnectivityInterface.QUERY_VARTYPE_INT);
            SetFetchSize(index, (Int32)value);
        }

        public override void SetVariable(Int32 index, Byte value)
        {
            queryParams.AppendNonNullValue((UInt64)value, SqlConnectivityInterface.QUERY_VARTYPE_UINT);
            SetFetchSize(index, (Int32)value);
        }

        public override void SetVariable(Int32 index, UInt16 value)
        {
            queryParams.AppendNonNullValue((UInt64)value, SqlConnectivityInterface.QUERY_VARTYPE_UINT);
            SetFetchSize(index, (Int32)value);
        }

        public override void SetVariable(Int32 index, UInt32 value)
        {
            queryParams.AppendNonNullValue((UInt64)value, SqlConnectivityInterface.QUERY_VARTYPE_UINT);
            SetFetchSize(index, (Int32)value);
        }

        public override void SetVariable(Int32 index, UInt64 value)
        {
            queryParams.AppendNonNullValue(value, SqlConnectivityInterface.QUERY_VARTYPE_UINT);
            SetFetchSize(index, (Int32)value);
        }

        public override void SetVariable(Int32 index, String value)
        {
            queryParams.Append(value, false, SqlConnectivityInterface.QUERY_VARTYPE_STRING);
        }

        public override void SetVariable(Int32 index, Decimal value)
        {
            queryParams.AppendNonNullValue(value, SqlConnectivityInterface.QUERY_VARTYPE_DECIMAL);
        }

        public override void SetVariable(Int32 index, Double value)
        {
            queryParams.AppendNonNullValue(value, SqlConnectivityInterface.QUERY_VARTYPE_DOUBLE);
        }

        public override void SetVariable(Int32 index, Single value)
        {
            queryParams.AppendNonNullValue(value, SqlConnectivityInterface.QUERY_VARTYPE_DOUBLE);
        }

        public override void SetVariable(Int32 index, IObjectView value)
        {
            queryParams.Append(value, SqlConnectivityInterface.QUERY_VARTYPE_OBJECT);
        }

        public override void SetVariable(Int32 index, Boolean value)
        {
            queryParams.AppendNonNullValue(value, SqlConnectivityInterface.QUERY_VARTYPE_BOOLEAN);
        }

        public override void SetVariable(Int32 index, DateTime value)
        {
            queryParams.AppendNonNullValue(value.Ticks, SqlConnectivityInterface.QUERY_VARTYPE_DATETIME);
        }

        public override void SetVariable(Int32 index, Binary value)
        {
            queryParams.AppendNonNullValue(value, SqlConnectivityInterface.QUERY_VARTYPE_BINARY);
        }

        public override void SetVariable(Int32 index, Byte[] value)
        {
            // Checking if its an offset key variable.
            if (sharedProps.RecreationKeyVarIndex == index)
            {
                // Getting total length of recreation key.
                Int32 existingKeyBytes = GetTotalLength(value);

                // Checking the length of supplied recreation key.
                if (existingKeyBytes > SqlConnectivityInterface.RECREATION_KEY_MAX_BYTES)
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Recreation key size is too big.");

                // Copying the key.
                Buffer.BlockCopy(value, 0, recreationKey, 0, existingKeyBytes);

                // Resetting iterator time stamps.
                ResetIteratorsLocatTime(recreationKey);

                // Adding null value to binary variable.
                queryParams.AppendNullValue();
            }
            else
            {
                queryParams.AppendNonNullValue(new Binary(value), SqlConnectivityInterface.QUERY_VARTYPE_BINARY);
            }
        }

        public override void SetVariable(Int32 index, Object value)
        {
            // Need to check type of each variable.
            TypeCode typeCode = Type.GetTypeCode(value.GetType());

            switch (typeCode)
            {
                case TypeCode.SByte:
                {
                    SetVariable(index, (SByte) value);
                    return;
                }

                case TypeCode.Int16:
                {
                    SetVariable(index, (Int16) value);
                    return;
                }

                case TypeCode.Int32:
                {
                    SetVariable(index, (Int32) value);
                    return;
                }

                case TypeCode.Int64:
                {
                    SetVariable(index, (Int64) value);
                    return;
                }

                case TypeCode.Byte:
                {
                    SetVariable(index, (Byte) value);
                    return;
                }

                case TypeCode.UInt16:
                {
                    SetVariable(index, (UInt16) value);
                    return;
                }

                case TypeCode.UInt32:
                {
                    SetVariable(index, (UInt32) value);
                    return;
                }

                case TypeCode.UInt64:
                {
                    SetVariable(index, (UInt64) value);
                    return;
                }

                case TypeCode.String:
                {
                    SetVariable(index, (String) value);
                    return;
                }

                case TypeCode.DateTime:
                {
                    SetVariable(index, (DateTime) value);
                    return;
                }

                case TypeCode.Decimal:
                {
                    SetVariable(index, (Decimal) value);
                    return;
                }

                case TypeCode.Double:
                {
                    SetVariable(index, (Double) value);
                    return;
                }

                case TypeCode.Single:
                {
                    SetVariable(index, (Single) value);
                    return;
                }

                case TypeCode.Boolean:
                {
                    SetVariable(index, (Boolean) value);
                    return;
                }
            }

            // Now checking specific Starcounter data types.
            if (value is IObjectView)
            {
                SetVariable(index, (IObjectView) value);
                return;
            }

            if (value is Binary)
            {
                SetVariable(index, (Binary) value);
                return;
            }

            if (value is Byte[])
            {
                SetVariable(index, (Byte[]) value);
                return;
            }

            throw new ArgumentException("SQL parameter " + index + " with type \"" + typeCode.ToString() + "\" is not supported.");
        }

        public override void SetVariableToNull(Int32 index)
        {
            queryParams.AppendNullValue();
        }

        // Length of recreation key header in bytes.
        const Int32 RECREATION_KEY_HEADER_BYTES = 12;

        /// <summary>
        /// Resets the iterator time stamp for all iterators in the recreation key.
        /// </summary>
        static void ResetIteratorsLocatTime(Byte[] recreationKey)
        {
            unsafe
            {
                fixed (Byte* keyData = recreationKey)
                {
                    // Maximum 255 enumerators.
                    Int32 numEnumerators = (*(Int32*)(keyData + 4));

                    // Iterating through each extent.
                    for (Int32 enumNum = 0; enumNum < numEnumerators; enumNum++)
                    {
                        // Position of enumerator static data.
                        Byte* staticDataOffset = keyData + (enumNum << 3) + RECREATION_KEY_HEADER_BYTES;

                        // Recreation key header (12 bytes):
                        // (total length, number of enumerators, first dynamic data position).

                        // Checking if its possible to recreate the key.
                        Int32 dynKeyOffset = (*(Int32*)(staticDataOffset + 4));
                        if (dynKeyOffset != 0)
                        {
                            // Pointing to dynamic recreation key data.
                            Byte* dynRecrKey = keyData + dynKeyOffset;

                            // Recreation key length.
                            Int32 keyLen = (*(Int32*)dynRecrKey);

                            // Resetting recreation key time value.
                            (*(Int32*)(dynRecrKey + keyLen - 4)) = 0;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets total length of recreation key.
        /// </summary>
        static Int32 GetTotalLength(Byte[] recreationKey)
        {
            // Total length of recreation key in bytes.
            Int32 totalLenBytes = 0;

            unsafe
            {
                fixed (Byte* keyData = recreationKey)
                {
                    // Getting total length in bytes.
                    totalLenBytes = (*(Int32*)keyData);
                }
            }

            return totalLenBytes;
        }

        /// <summary>
        /// Sets values to all SQL variables in the SQL query.
        /// </summary>
        /// <param name="values">The new values of the variables in order of appearance.</param>
        public override void SetVariables(Object[] sqlParams)
        {
            // Running throw all variables in the array.
            for (Int32 i = 0; i < sqlParams.Length; i++)
            {
                if (sqlParams[i] != null)
                {
                    SetVariable(i, sqlParams[i]);
                }
                else
                {
                    SetVariableToNull(i);
                }
            }
        }
    }
}
