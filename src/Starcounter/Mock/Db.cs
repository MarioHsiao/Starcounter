
using System;
//using System.Data;
using System.Reflection;
using Sc.Server.Internal;
//using Sc.Server.Weaver;
using Starcounter.LucentObjects;
using Starcounter.Query.Execution;
using Starcounter.Query.Sql;
using Sc.Server.Binding;

namespace Starcounter
{
    public static partial class Db
    {
//        static readonly LogSource logSource = LogSources.Sql;

        /// <summary>
        /// Used to represent the "null" value as a string in a uniform way.
        /// </summary>
        public const String NullString = "<NULL>";

        public const String NoIdString = "<NoId>";
        public const String FieldSeparator = " | ";

#if false
        /// <summary>
        /// Gets the current database.
        /// </summary>
        private static volatile Db _current;
        public static Db Current
        {
            get { return _current; }
            private set { _current = value; }
        }
#endif

#if false
        /// <summary>
        /// Gets the Starcounter URI of the database represented by the current
        /// client application instance.
        /// </summary>
        public string DatabaseUri { get; private set; }

        private Db(ScUri uri)
        {
            this.DatabaseUri = uri;
        }

        /// <summary>
        /// Initializes the <c>Db.Current</c> reference when executing in the
        /// database (and in the database domain).
        /// </summary>
        /// <param name="uri">The URI of the executing database.</param>
        internal static void InstantiateInDatabaseDomain(string uri)
        {
            Db.Current = new Db(uri);
        }

        /// <summary>
        /// Initializes the <c>Db.Current</c> reference when executing in an
        /// external process.
        /// </summary>
        /// <param name="uri">The URI of the executing database.</param>
        internal static void InstantiateInExternalProcess(ScUri uri)
        {
            Db.Current = new Db(uri);
        }
#endif

#if true
        public static String BinaryToHex(Binary binValue)
        {
            if (binValue == null)
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect binValue.");

            String hexString = BitConverter.ToString(binValue.ToArray());
            return hexString.Replace("-", "");
        }

        public static Binary HexToBinary(String hexString)
        {
            if (hexString == null)
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect hexString.");

            Byte[] byteArr = new Byte[hexString.Length / 2];
            for (Int32 i = 0; i < byteArr.Length; i++)
            {
                byteArr[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            }
            return new Binary(byteArr);
        }
#endif

#if true
        /// <summary>
        /// Returns the result of an SQL query as an SqlResult which implements IEnumerable.
        /// </summary>
        /// <param name="query">An SQL query.</param>
        /// <param name="values">The values to be used for variables in the query.</param>
        /// <returns>The result of the SQL query.</returns>
        public static SqlResult SQL(String query, params Object[] values)
        {
            if (query == null)
                throw new ArgumentNullException("query");

#if true
            return new SqlResult(0, query, false, values);
#else
            if (Starcounter.Transaction.Current != null)
                return new SqlResult(Starcounter.Transaction.Current.TransactionId, query, false, values); 
            else
                return new SqlResult(0, query, false, values);
#endif
        }
#endif

#if true
        /// <summary>
        /// Returns the result of an SQL query as an SqlResult which implements IEnumerable.
        /// Especially queries with expected slow execution are supported, as for example queries including literals.
        /// </summary>
        /// <param name="query">An SQL query.</param>
        /// <param name="values">The values to be used for variables in the query.</param>
        /// <returns>The result of the SQL query.</returns>
        public static SqlResult SlowSQL(String query, params Object[] values)
        {
            if (query == null)
                throw new ArgumentNullException("query");

            UInt64 transactionId = 0;
#if false
            if (Starcounter.Transaction.Current != null)
				transactionId = Starcounter.Transaction.Current.TransactionId;
#endif

            if (query == "")
				return new SqlResult(transactionId, query, true, values);

            switch (query[0])
            {
                case 'S':
                case 's':
					return new SqlResult(transactionId, query, true, values);

                case 'D':
                case 'd':
                    SqlProcessor.ProcessDelete(query, values);
                    return null;

                case ' ':
                case '\t':
                    query = query.TrimStart(' ', '\t');
                    switch (query[0])
                    {
                        case 'S':
                        case 's':
							return new SqlResult(transactionId, query, true, values);

                        case 'D':
                        case 'd':
                            SqlProcessor.ProcessDelete(query, values);
                            return null;

                        default:
							return new SqlResult(transactionId, query, true, values);
                    }

                default:
					return new SqlResult(transactionId, query, true, values);
            }
        }
#endif

#if false
        /// <summary>
        /// Executes a specified delegate in a transaction scheduled with
        /// the job manager.
        /// </summary>
        /// <param name="action">
        /// Delegate to be executed.
        /// </param>
        public static void ScheduledTransaction(Action action)
        {
            Sc.Server.Internal.ThreadPool.QueueUserWorkItem(
                new System.Threading.WaitCallback((ignored) => Transaction(action)), null);
        }

        /// <summary>
        /// Executes a specified delegate in a transaction. If there is already
        /// a current transaction, that one is used.
        /// </summary>
        /// <param name="action">
        /// Delegate to be executed.
        /// </param>
        public static void Transaction(Action action)
        {
            Transaction(action, TransactionScopeOptions.Default, TransactionOptions.None);
        }

        /// <summary>
        /// Executes a specified delegate in a new transaction.
        /// </summary>
        /// <param name="action">
        /// Delegate to be executed.
        /// </param>
        /// <remarks>
        /// <para>
        /// Is not allowed to be call within a transaction scope.
        /// </para>
        /// <para>
        /// The new transaction will be committed after successful execution
        /// of that delegate, or rolled back if the execution results in an
        /// exception.
        /// </para>
        /// </remarks>
		public static void ParallelTransaction(Action action)
        {
            Transaction(action, TransactionScopeOptions.RequiresNew, TransactionOptions.None);
        }

        /// <summary>
        /// Executes a specified delegate in a new transaction.
        /// </summary>
        /// <param name="action">
        /// Delegate to be executed.
        /// </param>
        /// <param name="options">
        /// Options for the new Transaction.
        /// </param>
        /// <remarks>
        /// <para>
        /// Is not allowed to be call within a transaction scope.
        /// </para>
        /// <para>
        /// The new transaction will be committed after successful execution
        /// of that delegate, or rolled back if the execution results in an
        /// exception.
        /// </para>
        /// </remarks>
		public static void ParallelTransaction(Action action, TransactionOptions options)
        {
            Transaction(action, TransactionScopeOptions.RequiresNew, options);
        }

        /// <summary>
        /// Connects an external application to the database identified by
        /// <paramref name="connectionString"/>.
        /// </summary>
        /// <param name="connectionString">Connection string identifying a
        /// database.</param>
        public static void Connect(string connectionString)
        {   
            // Check if a backend for the current process (or rather the current domain)
            // has been determined previously and if it has, check we are not exeucting
            // inside the database. The database will always instantly assign the backend
            // so we don't run the risk of user code invoking Connect before it has been
            // assigned if we are in the database process.

            if (ApplicationBackend.Current.BackendKind == ApplicationBackend.Kind.Database)
            {
                throw ErrorCode.ToException(Error.SCERRCONNECTINSIDEDATABASE);
            }

            ClientContext.Current.Connect(connectionString);
        }
#endif

#if false
        public void Disconnect()
        {
            if (ApplicationBackend.Current.BackendKind == ApplicationBackend.Kind.Database)
            {
                throw new InvalidOperationException("Disconnect denied: the calling thread is not allowed to disconnect the database.");
            }

            ClientContext.Current.Disconnect();
        }
#endif

#if false
        /// <summary>
        /// Allows a client assembly to be explicitly added to the set of client
        /// assemblies the Lucent Objects runtime knows about.
        /// </summary>
        /// <param name="userAssembly">The assembly to enable</param>
        public void EnableClientAssembly(Assembly userAssembly)
        {
            Type initializerType;

            if (ApplicationBackend.Current.BackendKind == ApplicationBackend.Kind.Database)
            {
                throw new InvalidOperationException();
            }

            if (userAssembly == null)
                throw new ArgumentNullException("userAssembly");

            // To proceed, we require that the assembly is tagged with the custom
            // attribute the compile-time weaver adds and that we can get ahold of
            // the known "implementation details type".

            initializerType = null;
            if (userAssembly.IsDefined(typeof(AssemblyWeavedForIPCAttribute), false))
            {
                initializerType = userAssembly.GetType(WeaverNamingConventions.ImplementationDetailsTypeName);
            }

            if (initializerType == null)
                throw LucentObjectsRuntime.CreateAssemblyNotWeavedForClientException(userAssembly);

            LucentObjectsRuntime.ExplicitInitializeClientAssembly(initializerType);
        }
#endif

#if false
		/// <summary>
		/// Creates and attaches a session in the database.
		/// </summary>
		/// <returns></returns>
		public ScSessionId CreateSession()
		{
			UInt32 ec;
			ScSessionId sessionId;

			if (ApplicationBackend.Current.BackendKind == ApplicationBackend.Kind.Database)
			{
				throw ErrorCode.ToException(Error.SCERRSESSIONINSIDEDATABASE);
			}

			ec = UnmanagedFunctions.sc_client_create_and_attach_session(out sessionId);
			if (ec != 0) throw ErrorCode.ToException(ec);

			return sessionId;
		}
#endif

#if false
		/// <summary>
		/// Closes and disposes the currently attached session.
		/// </summary>
		/// <remarks>
		/// If the session is not closed manually, 
		/// it will be timed out eventually if it's not used.
		/// </remarks>
		public void CloseSession()
		{
			UInt32 ec;

			if (ApplicationBackend.Current.BackendKind == ApplicationBackend.Kind.Database)
			{
				throw ErrorCode.ToException(Error.SCERRSESSIONINSIDEDATABASE);
			}

			ec = UnmanagedFunctions.sc_client_close_session();
			if (ec != 0) throw ErrorCode.ToException(ec);
		}
#endif

#if false
		/// <summary>
		/// Attaches the specified session to the current thread. 
		/// </summary>
		/// <param name="sessionId"></param>
		public void AttachSession(ScSessionId sessionId)
		{
			UInt32 ec;

			if (ApplicationBackend.Current.BackendKind == ApplicationBackend.Kind.Database)
			{
				throw ErrorCode.ToException(Error.SCERRSESSIONINSIDEDATABASE);
			}

			ec = UnmanagedFunctions.sc_client_attach_session(sessionId);
			if (ec != 0) throw ErrorCode.ToException(ec);
		}
#endif

#if false
		/// <summary>
		/// Detaches the current attached session from the thread.
		/// </summary>
		public void DetachSession()
		{
			UInt32 ec;

			if (ApplicationBackend.Current.BackendKind == ApplicationBackend.Kind.Database)
			{
				throw ErrorCode.ToException(Error.SCERRSESSIONINSIDEDATABASE);
			}

			ec = UnmanagedFunctions.sc_client_detach_session();
			if (ec != 0) throw ErrorCode.ToException(ec);
		}
#endif

#if false
        /// <summary>
        /// Executes a specified delegate in a transaction. Depending on the 
        /// specified options either an existing Transaction is used or a new 
        /// is created.
        /// </summary>
        /// <param name="options">
        /// Specifies if a current Transaction should be used or a new one 
        /// always created.
        /// </param>
        /// <param name="transOptions">
        /// Specific options for 
        /// </param>
        /// <param name="action">
        /// Delegate to be executed.
        /// </param>
        private static void Transaction(Action action,
                                        TransactionScopeOptions options,
                                        TransactionOptions transOptions
                                       )
        {
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }
            TransactionScope transactionScope = new TransactionScope(options, transOptions);
            for (int i = 1; ; i++)
            {
                try
                {
                    action();
                    transactionScope.SetComplete();
                    return;
                }
                catch (Exception e)
                {
                    ITransactionConflictException transactionConflictException = e as ITransactionConflictException;
                    if (transactionConflictException != null)
                    {
                        if (transactionScope.PrepareRetry(transactionConflictException, i))
                        {
                            continue;
                        }
                        else
                        {
                            if (transactionScope.IsTransactionBoundary)
                            {
                                transactionScope.SetAbort();
                                throw ErrorCode.ToException(Error.SCERRUNHANDLEDTRANSACTCONFLICT, e);
                            }
                        }
                    }
                    transactionScope.SetAbort();
                    throw;
                }
            }
        }
#endif

#if false
        /// <summary>
        /// Executes a Starcounter SQL query and returns the result as a DataTable.
        /// An object is not returned as an object reference but as the object-id as a String.
        /// </summary>
        /// <param name="query">A Starcounter SQL query.</param>
        /// <returns>The result of the query.</returns>
        internal static DataTable GetDataTable(String query)
        {
            Boolean hasMoreRows;
            return GetDataTable(query, 0, Int64.MaxValue, out hasMoreRows);
        }
#endif

#if false
        /// <summary>
        /// Executes a Starcounter SQL query and returns the result as a DataTable.
        /// Only the rows between the specified parameters first and last are returned (first row is 0).
        /// An object is not returned as an object reference but as the object-id as a String.
        /// </summary>
        /// <param name="query">A Starcounter SQL query.</param>
        /// <param name="firstRow">The number of the first row to be returned.</param>
        /// <param name="lastRow">The number of the last row to be returned.</param>
        /// <param name="hasMoreRows">True, if there are more rows to return, otherwise false.</param>
        /// <returns>The result of the query.</returns>
        internal static DataTable GetDataTable(String query, Int64 firstRow, Int64 lastRow, out Boolean hasMoreRows)
        {
            try
            {
                if (query == null)
                {
                    throw new ArgumentNullException("query");
                }
                IExecutionEnumerator sqlResult = (IExecutionEnumerator) SQL(query).GetEnumerator();
                return ConvertToDataTable(sqlResult, firstRow, lastRow, out hasMoreRows);
            }
            catch (DbException exception)
            {
                logSource.LogException(exception, "Internal error.");
                throw exception;
            }
        }
#endif

#if false
        internal static DataTable ConvertToDataTable(IExecutionEnumerator sqlResult, Int64 firstRow, Int64 lastRow, out Boolean hasMoreRows)
        {
            CompositeTypeBinding typeBind = sqlResult.CompositeTypeBinding;
            DataTable dataTable = new DataTable();
            dataTable.TableName = sqlResult.ToString();
            for (Int32 i = 0; i < typeBind.PropertyCount; i++)
            {
                PropertyMapping propBind = typeBind.GetPropertyBinding(i) as PropertyMapping;
                DataColumn dataColumn = new DataColumn();
                DbTypeCode typeCode = propBind.TypeCode;
                Type type = GetType(typeCode);
                dataColumn.DataType = type;
                dataColumn.ColumnName = propBind.DisplayName;
                dataTable.Columns.Add(dataColumn);
            }
            sqlResult.Reset();
            // Move to the first resulting object (row) of interest.
            while (sqlResult.Counter < firstRow && sqlResult.MoveNext())
            {
            }
            // Collect the resulting objects (rows) of interest.
            while (sqlResult.Counter <= lastRow && sqlResult.MoveNext())
            {
                IObjectView currentObj = sqlResult.Current;
                DataRow dataRow = dataTable.NewRow();
                IPropertyBinding propBind = null;
                for (Int32 i = 0; i < typeBind.PropertyCount; i++)
                {
                    propBind = typeBind.GetPropertyBinding(i);
                    switch (propBind.TypeCode)
                    {
                        case DbTypeCode.Binary:
                            Nullable<Binary> binValue = currentObj.GetBinary(i);
                            if (binValue == null)
                            {
                                dataRow[i] = DBNull.Value;
                            }
                            else
                            {
                                dataRow[i] = BinaryToHex(binValue.Value);
                            }
                            break;
                        case DbTypeCode.Boolean:
                            Nullable<Boolean> blnValue = currentObj.GetBoolean(i);
                            if (blnValue == null)
                            {
                                dataRow[i] = DBNull.Value;
                            }
                            else
                            {
                                dataRow[i] = blnValue.Value;
                            }
                            break;
                        case DbTypeCode.Byte:
                            Nullable<Byte> byteValue = currentObj.GetByte(i);
                            if (byteValue == null)
                            {
                                dataRow[i] = DBNull.Value;
                            }
                            else
                            {
                                dataRow[i] = byteValue.Value;
                            }
                            break;
                        case DbTypeCode.DateTime:
                            Nullable<DateTime> dtmValue = currentObj.GetDateTime(i);
                            if (dtmValue == null)
                            {
                                dataRow[i] = DBNull.Value;
                            }
                            else
                            {
                                dataRow[i] = dtmValue.Value;
                            }
                            break;
                        case DbTypeCode.Decimal:
                            Nullable<Decimal> decValue = currentObj.GetDecimal(i);
                            if (decValue == null)
                            {
                                dataRow[i] = DBNull.Value;
                            }
                            else
                            {
                                dataRow[i] = decValue.Value;
                            }
                            break;
                        case DbTypeCode.Double:
                            Nullable<Double> dblValue = currentObj.GetDouble(i);
                            if (dblValue == null)
                            {
                                dataRow[i] = DBNull.Value;
                            }
                            else
                            {
                                dataRow[i] = dblValue.Value;
                            }
                            break;
                        case DbTypeCode.Int16:
                            Nullable<Int16> int16Value = currentObj.GetInt16(i);
                            if (int16Value == null)
                            {
                                dataRow[i] = DBNull.Value;
                            }
                            else
                            {
                                dataRow[i] = int16Value.Value;
                            }
                            break;
                        case DbTypeCode.Int32:
                            Nullable<Int32> int32Value = currentObj.GetInt32(i);
                            if (int32Value == null)
                            {
                                dataRow[i] = DBNull.Value;
                            }
                            else
                            {
                                dataRow[i] = int32Value.Value;
                            }
                            break;
                        case DbTypeCode.Int64:
                            Nullable<Int64> int64Value = currentObj.GetInt64(i);
                            if (int64Value == null)
                            {
                                dataRow[i] = DBNull.Value;
                            }
                            else
                            {
                                dataRow[i] = int64Value.Value;
                            }
                            break;
                        case DbTypeCode.Object:
                            IObjectView objValue = currentObj.GetObject(i);
                            if (objValue == null)
                            {
                                dataRow[i] = DBNull.Value;
                            }
                            else
                            {
                                if (objValue is Entity)
                                {
                                    dataRow[i] = DbHelper.GetObjectID(objValue as Entity).ToString() + " " + objValue.ToString();
                                }
                                else
                                {
                                    dataRow[i] = objValue.ToString();
                                }
                            }
                            break;
                        case DbTypeCode.Objects:
                            // TODO: A method GetObjects(Int32) does not exist.
                            dataRow[i] = "NoInfo";
                            break;
                        case DbTypeCode.SByte:
                            Nullable<SByte> sbyteValue = currentObj.GetSByte(i);
                            if (sbyteValue == null)
                            {
                                dataRow[i] = DBNull.Value;
                            }
                            else
                            {
                                dataRow[i] = sbyteValue.Value;
                            }
                            break;
                        case DbTypeCode.Single:
                            Nullable<Single> sngValue = currentObj.GetSingle(i);
                            if (sngValue == null)
                            {
                                dataRow[i] = DBNull.Value;
                            }
                            else
                            {
                                dataRow[i] = sngValue.Value;
                            }
                            break;
                        case DbTypeCode.String:
                            String strValue = currentObj.GetString(i);
                            if (strValue == null)
                            {
                                dataRow[i] = DBNull.Value;
                            }
                            else
                            {
                                dataRow[i] = strValue;
                            }
                            break;
                        case DbTypeCode.UInt16:
                            Nullable<UInt16> uint16Value = currentObj.GetUInt16(i);
                            if (uint16Value == null)
                            {
                                dataRow[i] = DBNull.Value;
                            }
                            else
                            {
                                dataRow[i] = uint16Value.Value;
                            }
                            break;
                        case DbTypeCode.UInt32:
                            Nullable<UInt32> uint32Value = currentObj.GetUInt32(i);
                            if (uint32Value == null)
                            {
                                dataRow[i] = DBNull.Value;
                            }
                            else
                            {
                                dataRow[i] = uint32Value.Value;
                            }
                            break;
                        case DbTypeCode.UInt64:
                            Nullable<UInt64> uint64Value = currentObj.GetUInt64(i);
                            if (uint64Value == null)
                            {
                                dataRow[i] = DBNull.Value;
                            }
                            else
                            {
                                dataRow[i] = uint64Value.Value;
                            }
                            break;
                        default:
                            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect TypeCode: " + propBind.TypeCode);
                    }
                }
                dataTable.Rows.Add(dataRow);
            }
            hasMoreRows = sqlResult.MoveNext();
            return dataTable;
        }
#endif

#if false
        internal static Type GetType(DbTypeCode typeCode)
        {
            switch (typeCode)
            {
                // A Binary is returned as a hexadecimal string.
                case DbTypeCode.Binary:
                    return Type.GetType("System.String");
                case (DbTypeCode.Boolean):
                    return Type.GetType("System.Boolean");
                case (DbTypeCode.Byte):
                    return Type.GetType("System.Byte");
                case (DbTypeCode.DateTime):
                    return Type.GetType("System.DateTime");
                case (DbTypeCode.Decimal):
                    return Type.GetType("System.Decimal");
                case (DbTypeCode.Double):
                    return Type.GetType("System.Double");
                case (DbTypeCode.Int16):
                    return Type.GetType("System.Int16");
                case (DbTypeCode.Int32):
                    return Type.GetType("System.Int32");
                case (DbTypeCode.Int64):
                    return Type.GetType("System.Int64");
                // An object is returned as the object-id in string format.
                case (DbTypeCode.Object):
                    return Type.GetType("System.String");
                // TODO: Handle DbTypeCode.Objects.
                case (DbTypeCode.Objects):
                    return Type.GetType("System.String");
                case (DbTypeCode.SByte):
                    return Type.GetType("System.SByte");
                case (DbTypeCode.Single):
                    return Type.GetType("System.Single");
                case (DbTypeCode.String):
                    return Type.GetType("System.String");
                case (DbTypeCode.UInt16):
                    return Type.GetType("System.UInt16");
                case (DbTypeCode.UInt32):
                    return Type.GetType("System.UInt32");
                case (DbTypeCode.UInt64):
                    return Type.GetType("System.UInt64");
                default:
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect typeCode: " + typeCode);
            }
        }
#endif

        internal static String CreateObjectString(ITypeBinding typeBind, IObjectView currentObj)
        {
            IPropertyBinding propBind = null;
            String result = FieldSeparator;
            for (Int32 i = 0; i < typeBind.PropertyCount; i++)
            {
                propBind = typeBind.GetPropertyBinding(i);
                switch (propBind.TypeCode)
                {
                    case DbTypeCode.Binary:
                        Nullable<Binary> binValue = currentObj.GetBinary(i);
                        if (binValue == null)
                            result += NullString;
                        else
                            result += BinaryToHex(binValue.Value);
                        break;

                    case DbTypeCode.Boolean:
                        Nullable<Boolean> blnValue = currentObj.GetBoolean(i);
                        if (blnValue == null)
                            result += NullString;
                        else
                            result += blnValue.Value;
                        break;

                    case DbTypeCode.Byte:
                        Nullable<Byte> byteValue = currentObj.GetByte(i);
                        if (byteValue == null)
                            result += NullString;
                        else
                            result += byteValue.Value;
                        break;

                    case DbTypeCode.DateTime:
                        Nullable<DateTime> dtmValue = currentObj.GetDateTime(i);
                        if (dtmValue == null)
                            result += NullString;
                        else
                            result += dtmValue.Value;
                        break;

                    case DbTypeCode.Decimal:
                        Nullable<Decimal> decValue = currentObj.GetDecimal(i);
                        if (decValue == null)
                            result += NullString;
                        else
                            result += decValue.Value;
                        break;

                    case DbTypeCode.Double:
                        Nullable<Double> dblValue = currentObj.GetDouble(i);
                        if (dblValue == null)
                            result += NullString;
                        else
                            result += dblValue.Value;
                        break;

                    case DbTypeCode.Int16:
                        Nullable<Int16> int16Value = currentObj.GetInt16(i);
                        if (int16Value == null)
                            result += NullString;
                        else
                            result += int16Value.Value;
                        break;

                    case DbTypeCode.Int32:
                        Nullable<Int32> int32Value = currentObj.GetInt32(i);
                        if (int32Value == null)
                            result += NullString;
                        else
                            result += int32Value.Value;
                        break;

                    case DbTypeCode.Int64:
                        Nullable<Int64> int64Value = currentObj.GetInt64(i);
                        if (int64Value == null)
                            result += NullString;
                        else
                            result += int64Value.Value;
                        break;

                    case DbTypeCode.Object:
                        IObjectView objValue = currentObj.GetObject(i);
                        if (objValue == null)
                            result += NullString;
                        else
                        {
                            if (objValue is Entity)
                                result += (objValue as Entity).ThisRef.ObjectID.ToString();
                            else
                                result += objValue.ToString();
                        }
                        break;

                    case DbTypeCode.SByte:
                        Nullable<SByte> sbyteValue = currentObj.GetSByte(i);
                        if (sbyteValue == null)
                            result += NullString;
                        else
                            result += sbyteValue.Value;
                        break;

                    case DbTypeCode.Single:
                        Nullable<Single> sngValue = currentObj.GetSingle(i);
                        if (sngValue == null)
                            result += NullString;
                        else
                            result += sngValue.Value;
                        break;

                    case DbTypeCode.String:
                        String strValue = currentObj.GetString(i);
                        if (strValue == null)
                            result += NullString;
                        else
                            result += strValue;
                        break;

                    case DbTypeCode.UInt16:
                        Nullable<UInt16> uint16Value = currentObj.GetUInt16(i);
                        if (uint16Value == null)
                            result += NullString;
                        else
                            result += uint16Value.Value;
                        break;

                    case DbTypeCode.UInt32:
                        Nullable<UInt32> uint32Value = currentObj.GetUInt32(i);
                        if (uint32Value == null)
                            result += NullString;
                        else
                            result += uint32Value.Value;
                        break;

                    case DbTypeCode.UInt64:
                        Nullable<UInt64> uint64Value = currentObj.GetUInt64(i);
                        if (uint64Value == null)
                            result += NullString;
                        else
                            result += uint64Value.Value;
                        break;

                    default:
                        throw new Exception("Incorrect TypeCode: " + propBind.TypeCode);
                }
                result += FieldSeparator;
            }
            return result;
        }
    }
}
