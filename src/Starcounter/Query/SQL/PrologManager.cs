
using se.sics.prologbeans;
using Starcounter;
using Sc.Server.Binding;
using Sc.Server.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Permissions;
using System.Threading;
//using System.Xml;
using Starcounter.Query.Execution;

namespace Starcounter.Query.Sql
{
    internal static class PrologManager
    {
        static LogSource logSource;
        static Object startProcessLock;
        static Process process;
        static Boolean newSchema;
        static String processFolder;
        static String processFileName;
        static String processVersion;
        static Int32 processPort;
        static String schemaFilePath;
        static String schemaFileEncoded;
        static DateTime schemaTime;
        static Int32 maxQueryLength;
        static Int32 maxQueryRetries;
        static Int32 maxVerifyRetries;
        static Int32 timeBetweenVerifyRetries;

        // Is called during start-up.
        internal static void Initiate(Boolean newschema, String processfolder, String processfilename, String processversion, Int32 processport, 
            String schemafilepath, Int32 maxquerylength, Int32 maxqueryretries, Int32 maxverifyretries, Int32 timebetweenverifyretries)
        {
            logSource = LogSources.Sql;
            startProcessLock = new Object();

            newSchema = newschema;
            processFolder = processfolder;
            processFileName = processfilename;
            processVersion = processversion;
            processPort = processport;
            schemaFilePath = schemafilepath;
            maxQueryLength = maxquerylength;
            maxQueryRetries = maxqueryretries;
            maxVerifyRetries = maxverifyretries;
            timeBetweenVerifyRetries = timebetweenverifyretries;

            schemaFileEncoded = schemaFilePath.Replace("\\", "/").Replace(' ', '?'); // TODO: Use some appropriate standard encoding?

            if (newSchema)
                InitiateNewSchema();
            else
                InitiateSameSchema();
        }

        private static void InitiateNewSchema()
        {
            DisconnectPrologSessions();

            Int32 tickCount = Environment.TickCount;

            // Export schema to file.
            ExportSchemaFile(schemaFilePath);

            KillConflictingProcess();

            StartProcess();

            // Temporary output the upstart time of Starcounter SQL executable.
            tickCount = Environment.TickCount - tickCount;
            // logSource.Debug("Upstart of StarcounterSQL in " + tickCount.ToString() + " ms.");
            logSource.LogNotice("Upstart of StarcounterSQL in " + tickCount.ToString() + " ms.");

            ConnectPrologSessions();
        }

        private static void InitiateSameSchema()
        {
            try
            {
                DisconnectPrologSessions();

                Int32 tickCount = Environment.TickCount;

                VerifyProcess(false);

                // Temporary output the reconnect time to Starcounter SQL executable.
                tickCount = Environment.TickCount - tickCount;
                //logSource.Debug("Reconnect to StarcounterSQL in " + tickCount.ToString() + " ms.");
                logSource.LogNotice("Reconnect to StarcounterSQL in " + tickCount.ToString() + " ms.");

                ConnectPrologSessions();
            }
            catch (SqlExecutableException)
            {
                InitiateNewSchema();
            }
        }

        /// <summary>
        /// If there is an old conflicting PrologProcess answering on the port processPort then it is killed.
        /// </summary>
        private static void KillConflictingProcess()
        {
            PrologSession session;
            session = null;
            // An exception will always be thrown, either (if there is an old PrologProcess)
            // System.IO.IOException: Unable to read data from the transport connection: An existing connection was forcibly closed by the remote host.
            // or (otherwise)
            // System.Net.Sockets.SocketException: No connection could be made because the target machine actively refused it.
            try
            {
                session = new PrologSession();
                session.Port = processPort;
                if (!session.Connected)
                {
                    session.connect();
                }
                session.executeQuery("kill_prolog");
                logSource.LogWarning("A conflicting SQL process was found and was killed.");
            }
            catch (IOException)
            {
                logSource.LogWarning("A conflicting SQL process was found and was killed.");
            }
            catch (SocketException)
            {
                logSource.Debug("No conflicting SQL process.");
            }
            finally
            {
                if (session != null)
                {
                    session.disconnect();
                }
            }
        }

        /// <summary>
        /// Starts and verifies external SQL process (StarcounterSQL.exe).
        /// </summary>
        private static void StartProcess()
        {
            lock (startProcessLock)
            {
                if (process != null && process.HasExited)
                {
                    process.Dispose();
                    process = null;
                }
                if (process == null)
                {
                    // Start process.
                    try
                    {
                        process = new Process();
                        process.StartInfo.FileName = Environment.CurrentDirectory + processFolder + processFileName;
                        process.StartInfo.CreateNoWindow = true;
                        process.StartInfo.UseShellExecute = true;
                        process.StartInfo.Arguments = processPort.ToString() + " " + schemaFileEncoded;
                        process.Start();
                    }
                    catch (Exception exception)
                    {
                        String errMessage = "Failed to start process: " + processFolder + processFileName + " " + process.StartInfo.Arguments;
                        throw ErrorCode.ToException(Error.SCERRSQLSTARTPROCESSFAILED, exception, errMessage);
                    }
                    logSource.Debug("Started process: " + processFolder + processFileName + " " + processPort + " " + schemaFileEncoded);

                    // Verify process.
                    Boolean verified = false;
                    Int32 retries = 0;
                    while (verified == false && retries < maxVerifyRetries)
                    {
                        retries++;
                        try
                        {
                            VerifyProcess(true);
                            verified = true;
                            logSource.Debug("Verified process: " + processFileName + " " + processVersion + " " + SchemaTimeString);
                        }
                        catch (DbException exception)
                        {
                            if (retries < maxVerifyRetries)
                            {
                                Thread.Sleep(timeBetweenVerifyRetries);
                            }
                            else
                            {
                                throw exception;
                            }
                        }
                    }
                }
            }
        }

        private static void VerifyProcess(Boolean controlSchemaTime)
        {
            Scheduler vpContext = Scheduler.GetInstance(true);
            PrologSession session = null;

            try
            {
                if (vpContext != null)
                {
                    session = vpContext.PrologSession;
                }
                if (session == null)
                {
                    session = new PrologSession();
                    if (vpContext != null)
                    {
                        vpContext.PrologSession = session;
                    }
                    session.Port = processPort;
                }
                if (!session.Connected)
                {
                    session.connect();
                }
                QueryAnswer answer = session.executeQuery("verify_prolog(Version,SchemaTime)");
                if (answer.IsError)
                {
                    throw new SqlExecutableException("SQL process error: " + answer.Error);
                }
                if (answer.queryFailed())
                {
                    throw new SqlExecutableException("SQL process query failure.");
                }
                String sqlVersion = answer.getValue("Version").ToString();
                if (sqlVersion != processVersion)
                {
                    throw new SqlExecutableException("Incorrect version of SQL executable: " + sqlVersion);
                }
                String externalSchemaTime = answer.getValue("SchemaTime").ToString();
                if (controlSchemaTime)
                {
                    if (externalSchemaTime != SchemaTimeString)
                    {
                        throw new SqlExecutableException("Incorrect schema time: " + externalSchemaTime);
                    }
                }
                else
                {
                    schemaTime = DateTime.Parse(externalSchemaTime, DateTimeFormatInfo.InvariantInfo);
                }
                if (vpContext == null)
                {
                    session.disconnect();
                }
            }
            catch (Exception exception)
            {
                String errMessage = "Failed to verify process: " + processFileName + " " + processVersion + " " + SchemaTimeString;
                throw ErrorCode.ToException(Error.SCERRSQLVERIFYPROCESSFAILED, exception, errMessage);
            }
        }

        [PermissionSet(SecurityAction.Assert, Unrestricted = true)]
        internal static IExecutionEnumerator ProcessSqlQuery(Scheduler vproc, String query)
        {
            //  Since the Prolog session is shared between all the threads
            //  managed by the same scheduler, this method must be called within
            //  the scope of a yield block. Currently it's only called from
            //  method SQL.GetPreparedCache() which sets a yield block. But if
            //  called from another place in the future this needs to be kept in
            //  mind.
            if (query == null)
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect query.");
            }
            if (query.Length > maxQueryLength)
            {
                throw ErrorCode.ToException(Error.SCERRQUERYSTRINGTOOLONG, "Query string longer than maximal length of " + maxQueryLength + " characters.");
            }
            if (query == "")
            {
                query = " ";
            }

            try
            {
                QueryAnswer answer = null;
                const Int32 loopMax = 5;
                Int32 loopCount = 0;
                PrologSession session = vproc.PrologSession;

                // Try maximum loopMax times to process the query.
                while (loopCount < loopMax)
                {
                    try
                    {
                        if (session == null)
                        {
                            session = new PrologSession();
                            vproc.PrologSession = session;
                            session.Port = processPort;
                        }
                        try
                        {
                            if (!session.Connected)
                            {
                                session.connect();
                            }
                            se.sics.prologbeans.Bindings bindings = new se.sics.prologbeans.Bindings();
                            bindings.bind("Query", query);
                            answer = session.executeQuery("sql_prolog(Query,TypeDef,ExecInfo,VarNum,ErrList)", bindings);
                        }
                        catch (IOException exception)
                        {
                            throw new SqlExecutableException(null, exception);
                        }
                        catch (SocketException exception)
                        {
                            throw new SqlExecutableException(null, exception);
                        }
                        if (answer.IsError)
                        {
                            throw new SqlExecutableException("SQL process error: " + answer.Error);
                        }
                        if (answer.queryFailed())
                        {
                            throw new SqlExecutableException("SQL process query failure.");
                        }
                        loopCount = loopMax;
                    }
                    catch (SqlExecutableException exception)
                    {
                        loopCount++;
                        if (loopCount < loopMax)
                        // Try to restart Prolog process.
                        {
                            logSource.LogException(exception);
                            InitiateNewSchema();
                            logSource.LogWarning("Restarted process: " + processFolder + processFileName + " " + processPort + " " + schemaFileEncoded);
                        }
                        else
                        {
                            throw;
                        }
                    }
                }

                if (answer == null)
                {
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect answer.");
                }

                Term errListTerm = answer.getValue("ErrList");
                if (errListTerm.Name != "[]")
                {
                    throw CreateSqlException(query, errListTerm);
                }

                // Get the number of variables in the query.
                Term varNumTerm = answer.getValue("VarNum");
                if (varNumTerm.Integer == false)
                {
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect varNumTerm: " + varNumTerm);
                }
                Int32 varNumber = varNumTerm.intValue();

                // Creating variable array with specified number of entries (if any).
                VariableArray variableArray = new VariableArray(varNumber);

                // Calling core enumerator creation function.
                IExecutionEnumerator createdEnumerator = Creator.CreateEnumerator(Creator.CreateResultTypeBinding(answer.getValue("TypeDef"), variableArray),
                                                                                  answer.getValue("ExecInfo"), variableArray, query);

                // The special case where query includes "LIKE ?" is handled by special class LikeExecEnumerator.
                if (((variableArray.QueryFlags & QueryFlags.IncludesLIKEvariable) != QueryFlags.None) && (query[0] != ' '))
                    createdEnumerator = new LikeExecEnumerator(query, null, null);

                // Return the created execution enumerator.
                return createdEnumerator;
            }
            catch (Exception exception)
            {
                if (exception is SqlException)
                {
                    throw exception;
                }
                else
                {
                    throw ErrorCode.ToException(Error.SCERRSQLPROCESSQUERYFAILED, exception, "Failed to process query: " + query);
                }
            }
        }

        private static SqlException CreateSqlException(String query, Term msgListTerm)
        {
            String message = "Failed to process query: " + query + ": ";
            Term cursor = msgListTerm;
            while (cursor.List && cursor.Name != "[]")
            {
                message += cursor.getArgument(1).Name + " ";
                cursor = cursor.getArgument(2);
            }
            return new SqlException(message);
        }

        // Is called during shut-down.
        internal static void Terminate()
        {
            DisconnectPrologSessions();

            if (process != null)
            {
                process.Dispose();
            }
        }

        private static void DisconnectPrologSessions()
        {
            PrologSession prologSession = null;
            for (Byte cpuNumber = 0; cpuNumber < Scheduler.SchedulerCount; cpuNumber++)
            {
                prologSession = Scheduler.GetInstance(cpuNumber).PrologSession;
                if (prologSession != null)
                {
                    prologSession.disconnect();
                }
            }
        }

        private static void ConnectPrologSessions()
        {
            Scheduler vpContext = null;
            PrologSession prologSession = null;
            for (Byte cpuNumber = 0; cpuNumber < Scheduler.SchedulerCount; cpuNumber++)
            {
                vpContext = Scheduler.GetInstance(cpuNumber);
                prologSession = vpContext.PrologSession;
                if (prologSession == null)
                {
                    prologSession = new PrologSession();
                    prologSession.Port = processPort;
                    vpContext.PrologSession = prologSession;
                }

                if (prologSession.Connected == false)
                {
                    prologSession.connect();
                }
            }
        }

        private static String SchemaTimeString
        {
            get
            {
                //return schemaTime.ToString("yyMMddHHmmss");
                return schemaTime.ToString(DateTimeFormatInfo.InvariantInfo);
            }
        }

        private static void ExportSchemaFile(String schemaFile)
        {
            StreamWriter streamWriter = null;
            IEnumerator<TypeBinding> typeEnumerator = null;
            IEnumerator<ExtensionBinding> extEnumerator = null;
            TypeBinding typeBind = null;
            ExtensionBinding extBind = null;
            IPropertyBinding propBind = null;

            schemaFilePath = schemaFile;

            // Set timestamp of schema creation.
            schemaTime = DateTime.Now;

            Int32 tickCount = Environment.TickCount;
            try
            {
                streamWriter = new StreamWriter(schemaFilePath);

                // Set meta-info of the current schema export.
                streamWriter.WriteLine("/* THIS FILE WAS AUTO-GENERATED. DO NOT EDIT! */");
                streamWriter.WriteLine(":- module(schema,[]).");
                streamWriter.WriteLine("schema_time('" + SchemaTimeString + "').");

                // Export information about classes (tables).
                streamWriter.WriteLine("class(fullClassNameUpper,fullClassName,baseClassName).");
                streamWriter.WriteLine("class(shortClassNameUpper,fullClassName,baseClassName).");
                String fullClassNameUpper = null;
                String shortClassNameUpper = null;
                typeEnumerator = TypeRepository.GetAllTypeBindings();
                while (typeEnumerator.MoveNext())
                {
                    typeBind = typeEnumerator.Current;
                    fullClassNameUpper = typeBind.Name.ToUpperInvariant();
                    shortClassNameUpper = GetShortName(typeBind.Name).ToUpperInvariant();
                    if (typeBind.Base != null)
                    {
                        streamWriter.WriteLine("class('" + fullClassNameUpper + "','" + typeBind.Name + "','" + typeBind.Base.Name + "').");
                        if (shortClassNameUpper != fullClassNameUpper)
                            streamWriter.WriteLine("class('" + shortClassNameUpper + "','" + typeBind.Name + "','" + typeBind.Base.Name + "').");
                    }
                    else
                    {
                        streamWriter.WriteLine("class('" + fullClassNameUpper + "','" + typeBind.Name + "','none').");
                        if (shortClassNameUpper != fullClassNameUpper)
                            streamWriter.WriteLine("class('" + shortClassNameUpper + "','" + typeBind.Name + "','none').");
                    }
                }

                // Export information about extensions.
                streamWriter.WriteLine("extension(fullClassName,fullExtensionNameUpper,fullExtensionName).");
                streamWriter.WriteLine("extension(fullClassName,shortExtensionNameUpper,fullExtensionName).");
                String fullExtensionNameUpper = null;
                String shortExtensionNameUpper = null;
                typeEnumerator.Reset();
                while (typeEnumerator.MoveNext())
                {
                    typeBind = typeEnumerator.Current;
                    extEnumerator = typeBind.GetAllExtensionBindings();
                    if (extEnumerator != null)
                    {
                        while (extEnumerator.MoveNext())
                        {
                            extBind = extEnumerator.Current;
                            fullExtensionNameUpper = extBind.Name.ToUpperInvariant();
                            shortExtensionNameUpper = GetShortName(extBind.Name).ToUpperInvariant();
                            streamWriter.WriteLine("extension('" + typeBind.Name + "','" + fullExtensionNameUpper + "','" + extBind.Name + "').");
                            if (shortExtensionNameUpper != fullExtensionNameUpper)
                                streamWriter.WriteLine("extension('" + typeBind.Name + "','" + shortExtensionNameUpper + "','" + extBind.Name + "').");
                        }
                    }
                }

                // Export information about properties (columns).
                streamWriter.WriteLine("property(fullClassName,propertyNameUpper,propertyName,propertyType).");
                typeEnumerator.Reset();
                while (typeEnumerator.MoveNext())
                {
                    typeBind = typeEnumerator.Current;
                    for (Int32 i = 0; i < typeBind.PropertyCount; i++)
                    {
                        propBind = typeBind.GetPropertyBinding(i);
                        if (propBind.TypeCode == DbTypeCode.Object)
                        {
                            if (propBind.TypeBinding != null)
                            {
                                streamWriter.WriteLine("property('" + typeBind.Name + "','" + propBind.Name.ToUpperInvariant() + "','" +
                                    propBind.Name + "','" + propBind.TypeBinding.Name + "').");
                            }
                            else
                            {
                                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Object reference type without type binding.");
                            }
                        }
                        else
                        {
                            streamWriter.WriteLine("property('" + typeBind.Name + "','" + propBind.Name.ToUpperInvariant() + "','" +
                                propBind.Name + "','" + propBind.TypeCode.ToString() + "').");
                        }
                    }
                }

                // Export information about extension properties (columns).
                typeEnumerator.Reset();
                while (typeEnumerator.MoveNext())
                {
                    typeBind = typeEnumerator.Current;
                    extEnumerator = typeBind.GetAllExtensionBindings();
                    if (extEnumerator != null)
                    {
                        while (extEnumerator.MoveNext())
                        {
                            extBind = extEnumerator.Current;
                            for (Int32 i = 0; i < extBind.PropertyCount; i++)
                            {
                                propBind = extBind.GetPropertyBinding(i);
                                if (propBind.TypeCode == DbTypeCode.Object)
                                {
                                    if (propBind.TypeBinding != null)
                                    {
                                        streamWriter.WriteLine("property('" + extBind.Name + "','" + propBind.Name.ToUpperInvariant() + "','" +
                                            propBind.Name + "','" + propBind.TypeBinding.Name + "').");
                                    }
                                    else
                                    {
                                        throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Object reference type without type binding.");
                                    }
                                }
                                else
                                {
                                    streamWriter.WriteLine("property('" + extBind.Name + "','" + propBind.Name.ToUpperInvariant() + "','" +
                                        propBind.Name + "','" + propBind.TypeCode.ToString() + "').");
                                }
                            }
                        }
                    }
                }

                // Export information about method EqualsOrIsDerivedFrom(Object).
                streamWriter.WriteLine("method(fullClassName,methodNameUpper,methodName,argumentTypes,returnType).");
                typeEnumerator.Reset();
                while (typeEnumerator.MoveNext())
                {
                    typeBind = typeEnumerator.Current;
                    streamWriter.WriteLine("method('" + typeBind.Name + "','EQUALSORISDERIVEDFROM','EqualsOrIsDerivedFrom',['Starcounter.IObjectView'],'Boolean').");
                }

                // Export information about generic method GetExtension<Type>().
                streamWriter.WriteLine("gmethod(fullClassName,methodNameUpper,methodName,typeParameters,argumentTypes,returnType).");
                typeEnumerator.Reset();
                while (typeEnumerator.MoveNext())
                {
                    typeBind = typeEnumerator.Current;
                    extEnumerator = typeBind.GetAllExtensionBindings();
                    while (extEnumerator.MoveNext())
                    {
                        extBind = extEnumerator.Current;
                        streamWriter.WriteLine("gmethod('" + typeBind.Name + "','GETEXTENSION','GetExtension',['" +
                            extBind.Name + "'],[],'" + extBind.Name + "').");
                    }
                }

                tickCount = Environment.TickCount - tickCount;
                // logSource.Debug("Exported SQL schema to " + schemaFilePath + " in " + tickCount.ToString() + " ms.");
                logSource.LogNotice("Exported SQL schema to " + schemaFilePath + " in " + tickCount.ToString() + " ms.");
            }
            catch (Exception exception)
            {
                String errMessage = "Failed to export SQL schema to " + schemaFilePath + ".";
                // logSource.LogException(exception, errMessage);
                throw ErrorCode.ToException(Error.SCERRSQLEXPORTSCHEMAFAILED, exception, errMessage);
            }
            finally
            {
                if (streamWriter != null)
                {
                    streamWriter.Close();
                }
            }
        }

        private static String GetShortName(String fullName)
        {
            if (fullName == null)
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect fullName.");
            }
            Int32 index = fullName.LastIndexOf('.'); // If no occurrence then index is -1.
            return fullName.Substring(index + 1);
        }
    }
}
