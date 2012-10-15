
using se.sics.prologbeans;
using Starcounter;
using Starcounter.Binding;
using Starcounter.Logging;
using Starcounter.Query.Execution;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Permissions;
using System.Text;
using System.Threading;

namespace Starcounter.Query.Sql
{
    internal static class PrologManager
    {
        static LogSource logSource;
        static Object startProcessLock;
        static List<String> schemaFilePathList = new List<String>();
        static Process process;
        //static String processFolder;
        //static String processFileName;
        //static String processVersion;
        //static Int32 processPort;
        static String schemaFolderExternal;
        //static Int32 maxQueryLength;
        //static Int32 maxQueryRetries;
        //static Int32 maxVerifyRetries;
        //static Int32 timeBetweenVerifyRetries;

        // Is called during start-up.
        internal static void Initiate()
        {
            logSource = LogSources.Sql;
            startProcessLock = new Object();

            //processFolder = procFolder;
            //processFileName = procFileName;
            //processVersion = procVersion;
            //processPort = procPort;

            //schemaFolderExternal = schemaFolder.Replace("\\", "/").Replace(' ', '?'); // TODO: Use some appropriate standard encoding?
            schemaFolderExternal = QueryModule.SchemaFolder.Replace("\\", "/");
            
            //maxQueryLength = queryLength;
            //maxQueryRetries = queryRetries;
            //maxVerifyRetries = verifyRetries;
            //timeBetweenVerifyRetries = betweenVerifyRetries;

            // Establish an SQL process without any schema information.
            EstablishSqlProcess();
        }

        /// <summary>
        /// Export schema information to external SQL process by one call to the SQL process per TypeBinding.
        /// Note that this method is significantly slower than ExportSchemaInfo.
        /// </summary>
        /// <param name="scheduler">Representation of the current virtual processor.</param>
        /// <param name="typeEnumerator">Enumerator of TypeBindings (type information).</param>
        private static void ExportSchemaInfo2(Scheduler scheduler, IEnumerator<TypeDef> typeEnumerator)
        {
            // Since the scheduler.PrologSession is shared between all the threads
            // managed by the same scheduler, this method must be called within
            // the scope of a yield block.

            String schemaInfo = null;
            TypeDef typeDef = null;
            try
            {
                while (typeEnumerator.MoveNext())
                {
                    typeDef = typeEnumerator.Current;
                    schemaInfo = GetTableSchemaInfo(typeDef);
                    CallSqlProcessToAddSchemaInfo(scheduler, schemaInfo);
                    logSource.Debug("Exported schema info about table (class): " + typeDef.Name);
                }
            }
            catch (Exception exception)
            {
                String errMessage = "Failed to export SQL schema info.";
                // TODO: New error code.
                throw ErrorCode.ToException(Error.SCERRSQLEXPORTSCHEMAFAILED, exception, errMessage);
            }
        }

        /// <summary>
        /// Export schema information to external SQL process by file and a single call to the SQL process.
        /// Note that this method is significantly faster than ExportSchemaInfo2.
        /// </summary>
        /// <param name="scheduler">Representation of the current virtual processor.</param>
        /// <param name="typeDefArray">Enumerator of TypeDefs (type information).</param>
        internal static void ExportSchemaInfo(Scheduler scheduler, TypeDef[] typeDefArray)
        {
            // Since the scheduler.PrologSession is shared between all the threads
            // managed by the same scheduler, this method must be called within
            // the scope of a yield block. 

            String schemaFilePath = schemaFolderExternal + "schema" + DateTime.Now.ToString("yyMMddHHmmssfff") + ".pl";
            try
            {
                WriteSchemaInfoToFile(schemaFilePath, typeDefArray);
            }
            catch (Exception exception)
            {
                String errMessage = "Failed to export SQL schema info to file: " + schemaFilePath;
                throw ErrorCode.ToException(Error.SCERRSQLEXPORTSCHEMAFAILED, exception, errMessage);
            }
            try
            {
                CallSqlProcessToLoadSchemaInfo(scheduler, schemaFilePath);
            }
            catch (Exception exception)
            {
                String errMessage = "Failed to load SQL schema info from file: " + schemaFilePath;
                // TODO: New error code SCERRSQLLOADSCHEMAFAILED
                throw ErrorCode.ToException(Error.SCERRSQLEXPORTSCHEMAFAILED, exception, errMessage);
            }
            schemaFilePathList.Add(schemaFilePath);
            logSource.Debug("Exported schema info: " + schemaFilePath);
        }

        /// <summary>
        /// Deletes all schema information of external SQL process and reexports schema information by using previously generated files.
        /// This method is primarily used for debugging purposes.
        /// </summary>
        /// <param name="scheduler">Representation of the current virtual processor.</param>
        private static void DeleteAndReExportAllSchemaInfo(Scheduler scheduler)
        {
            // Since the scheduler.PrologSession is shared between all the threads
            // managed by the same scheduler, this method must be called within
            // the scope of a yield block.

            List<String> tmpSchemaFileList = schemaFilePathList; // Temporary store the schemaFilePathList.
            DeleteAllSchemaInfo(scheduler); // schemaFilePathList becomes empty.
            ReExportAllSchemaInfo(scheduler, tmpSchemaFileList); // schemaFilePathList becomes reinstantiated.
        }

        /// <summary>
        /// Reexports schema information to external SQL process by using previously generated files.
        /// This method is primarily used for debugging purposes.
        /// </summary>
        /// <param name="scheduler">Representation of the current virtual processor.</param>
        /// <param name="tmpSchemaFileList">List of previously generated schema files.</param>
        private static void ReExportAllSchemaInfo(Scheduler scheduler, List<String> tmpSchemaFileList)
        {
            // Since the scheduler.PrologSession is shared between all the threads
            // managed by the same scheduler, this method must be called within
            // the scope of a yield block.

            String schemaFilePath = null;
            for (Int32 i = 0; i < tmpSchemaFileList.Count; i++)
            {
                try
                {
                    CallSqlProcessToLoadSchemaInfo(scheduler, tmpSchemaFileList[i]);
                }
                catch (Exception exception)
                {
                    String errMessage = "Failed to reload SQL schema info from file: " + schemaFilePath;
                    // TODO: New error code SCERRSQLRELOADSCHEMAFAILED
                    throw ErrorCode.ToException(Error.SCERRSQLEXPORTSCHEMAFAILED, exception, errMessage);
                }
                schemaFilePathList.Add(tmpSchemaFileList[i]);
                logSource.Debug("Reexported schema info: " + schemaFilePath);
            }
        }

        /// <summary>
        /// Deletes all schema information of external SQL process.
        /// </summary>
        /// <param name="scheduler">Representation of the current virtual processor.</param>
        internal static void DeleteAllSchemaInfo(Scheduler scheduler)
        {
            // Since the scheduler.PrologSession is shared between all the threads
            // managed by the same scheduler, this method must be called within
            // the scope of a yield block. 

            PrologSession session = null;
            QueryAnswer answer = null;
            Int32 loopCount = 0;

            while (loopCount < QueryModule.MaxQueryRetries)
            {
                try
                {
                    EstablishConnectedSession(ref session, scheduler);
                    answer = session.executeQuery("delete_schemainfo_prolog");
                    CheckQueryAnswerForError(answer);
                    loopCount = QueryModule.MaxQueryRetries;
                }
                catch (Exception exception)
                {
                    loopCount++;
                    if (loopCount < QueryModule.MaxQueryRetries)
                    {
                        logSource.LogWarning("Failed once to delete schema info.", exception);
                    }
                    else
                    {
                        LeaveConnectedSession(session, scheduler);
                        // TODO: New error code SCERRSQLDELETESCHEMAFAILED
                        throw ErrorCode.ToException(Error.SCERRSQLEXPORTSCHEMAFAILED, exception);
                    }
                }
            }
            LeaveConnectedSession(session, scheduler);

            schemaFilePathList = new List<String>();
        }

        /// <summary>
        /// Checks that the schema files loaded into the external SQL process equals the here generated schema files.
        /// Primarily used for debugging purposes.
        /// </summary>
        /// <param name="scheduler">Representation of the current virtual processor.</param>
        /// <returns>True, if the schema information is correct, otherwise false.</returns>
        private static Boolean VerifySchemaInfo(Scheduler scheduler)
        {
            // Since the scheduler.PrologSession is shared between all the threads
            // managed by the same scheduler, this method must be called within
            // the scope of a yield block. 

            List<String> externalSchemaFilePathList = null;
            try
            {
                externalSchemaFilePathList = GetCurrentSqlSchemaFiles(scheduler);
                if (StringListEqual(externalSchemaFilePathList, schemaFilePathList))
                    return true;
                return false;
            }
            catch (Exception exception)
            {
                // TODO: New error code SCERRSQLVERIFYSCHEMAFAILED
                throw ErrorCode.ToException(Error.SCERRSQLEXPORTSCHEMAFAILED, exception);
            }
        }

        private static void CheckQueryAnswerForError(QueryAnswer answer)
        {
            if (answer == null)
            {
                throw new SqlExecutableException("Incorrect answer.");
            }
            if (answer.IsError)
            {
                throw new SqlExecutableException("SQL process error: " + answer.Error);
            }
            if (answer.queryFailed())
            {
                throw new SqlExecutableException("SQL process query failure.");
            }
        }

        private static void EstablishSqlProcess()
        {
            DisconnectPrologSessions();

            String existingProcessVersion = GetExistingSqlProcessVersion();

            // Correct version of process is running.
            if (existingProcessVersion == QueryModule.ProcessVersion)
            {
                ConnectPrologSessions();
                try
                {
                    //Starcounter.ThreadHelper.SetYieldBlock();
                    //Scheduler scheduler = Scheduler.GetInstance(true);
                    //DeleteAllSchemaInfo(scheduler);
                    DeleteAllSchemaInfo(null);
                }
                finally
                {
                    //Starcounter.ThreadHelper.ReleaseYieldBlock();
                }
                return;
            }

            // No process is running.
            if (existingProcessVersion == null)
            {
                StartSqlProcess();
                ConnectPrologSessions();
                return;
            }

            // Incorrect version of process is running.
            KillExistingSqlProcess();
            StartSqlProcess();
            ConnectPrologSessions();
        }

        private static void ConnectPrologSessions()
        {
            Scheduler scheduler = null;
            PrologSession prologSession = null;
            for (Byte cpuNumber = 0; cpuNumber < Scheduler.SchedulerCount; cpuNumber++)
            {
                scheduler = Scheduler.GetInstance(cpuNumber);
                prologSession = scheduler.PrologSession;
                if (prologSession == null)
                {
                    prologSession = new PrologSession();
                    prologSession.Port = QueryModule.ProcessPort;
                    scheduler.PrologSession = prologSession;
                }

                if (prologSession.Connected == false)
                {
                    prologSession.connect();
                }
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

        /// <summary>
        /// Gets the version of existing SQL process, if there is one.
        /// </summary>
        /// <returns>The version number, or null (if there is no such process).</returns>
        private static String GetExistingSqlProcessVersion()
        {
            PrologSession session = null;

            try
            {
                EstablishConnectedSession(ref session, null);
                QueryAnswer answer = session.executeQuery("process_version_prolog(Version)");
                CheckQueryAnswerForError(answer);
                String existingProcessVersion = answer.getValue("Version").ToString();
                return existingProcessVersion;
            }
            catch (SocketException)
            {
                return null;
            }
            finally
            {
                LeaveConnectedSession(session, null);
            }
        }

        private static void EstablishConnectedSession(ref PrologSession session, Scheduler scheduler)
        {
            // Check current session.
            if (session != null)
            {
                if (session.Connected)
                {
                    return;
                }
                session.connect();
                return;
            }
            // Get session from scheduler.
            if (scheduler != null)
            {
                session = scheduler.PrologSession;
            }
            // Create new session.
            if (session == null)
            {
                session = new PrologSession();
                if (scheduler != null)
                {
                    scheduler.PrologSession = session;
                }
                session.Port = QueryModule.ProcessPort;
            }
            if (!session.Connected)
            {
                session.connect();
            }
        }

        private static void LeaveConnectedSession(PrologSession session, Scheduler scheduler)
        {
            if (scheduler == null && session != null)
            {
                session.disconnect();
            }
        }

        private static void KillExistingSqlProcess()
        {
            PrologSession session = null;

            // An exception will always be thrown, either (if there is an existing SQL process)
            // System.IO.IOException: Unable to read data from the transport connection: An existing connection was forcibly closed by the remote host.
            // or (otherwise)
            // System.Net.Sockets.SocketException: No connection could be made because the target machine actively refused it.
            try
            {
                EstablishConnectedSession(ref session, null);
                session.executeQuery("kill_prolog");
                logSource.LogWarning("A conflicting SQL process was found and was killed.");
            }
            catch (IOException)
            {
                logSource.LogWarning("A conflicting SQL process was found and was killed.");
            }
            catch (SocketException)
            {
                logSource.LogWarning("A conflicting SQL process was found but was killed by someone else.");
            }
            finally
            {
                LeaveConnectedSession(session, null);
            }
        }

        /// <summary>
        /// Starts and verifies external SQL process (StarcounterSQL.exe).
        /// </summary>
        private static void StartSqlProcess()
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
                        process.StartInfo.FileName = QueryModule.ProcessFolder + QueryModule.ProcessFileName;
                        process.StartInfo.CreateNoWindow = true;
                        process.StartInfo.UseShellExecute = true;
                        process.StartInfo.Arguments = QueryModule.ProcessPort.ToString();
                        process.Start();
                    }
                    catch (Exception exception)
                    {
                        String errMessage = "Failed to start process: " + QueryModule.ProcessFolder + QueryModule.ProcessFileName + " " + QueryModule.ProcessPort.ToString();
                        throw ErrorCode.ToException(Error.SCERRSQLSTARTPROCESSFAILED, exception, errMessage);
                    }
                    logSource.Debug("Started process: " + QueryModule.ProcessFolder + QueryModule.ProcessFileName + " " + QueryModule.ProcessPort.ToString());

                    // Verify process.
                    Boolean verified = false;
                    Int32 retries = 0;
                    String existingProcessVersion = null;

                    while (verified == false && retries < QueryModule.MaxVerifyRetries)
                    {
                        retries++;

                        existingProcessVersion = GetExistingSqlProcessVersion();
                        verified = (existingProcessVersion == QueryModule.ProcessVersion);

                        if (!verified && retries < QueryModule.MaxVerifyRetries)
                        {
                            Thread.Sleep(QueryModule.TimeBetweenVerifyRetries);
                        }
                    }

                    if (verified)
                        logSource.Debug("Verified process: " + QueryModule.ProcessFolder + QueryModule.ProcessFileName + " " + QueryModule.ProcessVersion);
                    else
                    {
                        String errMessage = "Failed to verify process: " + QueryModule.ProcessFolder + QueryModule.ProcessFileName + " " + QueryModule.ProcessVersion;
                        throw ErrorCode.ToException(Error.SCERRSQLVERIFYPROCESSFAILED, errMessage);
                    }
                }
            }
        }

        /// <summary>
        /// Get schema info regarding one table (class).
        /// </summary>
        /// <param name="typeDef">The table (class).</param>
        /// <returns>A concatenated string of schema info items.</returns>
        private static String GetTableSchemaInfo(TypeDef typeDef)
        {
            StringBuilder strBuilder = new StringBuilder();
            //IEnumerator<ExtensionBinding> extEnumerator = null;

            try
            {
                // Info about table (class).
                String fullClassNameUpper = typeDef.Name.ToUpperInvariant();
                String shortClassNameUpper = GetShortName(typeDef.Name).ToUpperInvariant();
                if (typeDef.BaseName != null)
                {
                    strBuilder.Append("class(\'" + fullClassNameUpper + "\',\'" + typeDef.Name + "\',\'" + typeDef.BaseName + "\');");
                    if (shortClassNameUpper != fullClassNameUpper)
                    {
                        strBuilder.Append("class(\'" + shortClassNameUpper + "\',\'" + typeDef.Name + "\',\'" + typeDef.BaseName + "\');");
                    }
                }
                else
                {
                    strBuilder.Append("class(\'" + fullClassNameUpper + "\',\'" + typeDef.Name + "\',\'none\');");
                    if (shortClassNameUpper != fullClassNameUpper)
                    {
                        strBuilder.Append("class(\'" + shortClassNameUpper + "\',\'" + typeDef.Name + "\',\'none\');");
                    }
                }

                // Info about columns (properties).
                PropertyDef propDef = null;
                for (Int32 i = 0; i < typeDef.PropertyDefs.Length; i++)
                {
                    propDef = typeDef.PropertyDefs[i];
                    if (propDef.Type == DbTypeCode.Object)
                    {
                        if (propDef.TargetTypeName != null)
                        {
                            strBuilder.Append("property(\'" + typeDef.Name + "\',\'" + propDef.Name.ToUpperInvariant() + "\',\'" + propDef.Name + "\',\'" +
                                propDef.TargetTypeName + "\');");
                        }
                        else
                        {
                            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Object reference type without type binding.");
                        }
                    }
                    else
                    {
                        strBuilder.Append("property(\'" + typeDef.Name + "\',\'" + propDef.Name.ToUpperInvariant() + "\',\'" + propDef.Name + "\',\'" +
                            propDef.Type.ToString() + "\');");
                    }
                }

                //// Info about extensions.
                //extEnumerator = typeBind.GetAllExtensionBindings();
                //ExtensionBinding extBind = null;
                //String fullExtensionNameUpper = null;
                //String shortExtensionNameUpper = null;
                //if (extEnumerator != null)
                //{
                //    while (extEnumerator.MoveNext())
                //    {
                //        extBind = extEnumerator.Current;
                //        fullExtensionNameUpper = extBind.Name.ToUpperInvariant();
                //        shortExtensionNameUpper = GetShortName(extBind.Name).ToUpperInvariant();
                //        strBuilder.Append("extension(\'" + typeBind.Name + "\',\'" + fullExtensionNameUpper + "\',\'" + extBind.Name + "\');");
                //        if (shortExtensionNameUpper != fullExtensionNameUpper)
                //        {
                //            strBuilder.Append("extension(\'" + typeBind.Name + "\',\'" + shortExtensionNameUpper + "\',\'" + extBind.Name + "\');");
                //        }
                //    }
                //}

                //// Info about extension columns (properties).
                //extEnumerator.Reset();
                //if (extEnumerator != null)
                //{
                //    while (extEnumerator.MoveNext())
                //    {
                //        extBind = extEnumerator.Current;
                //        for (Int32 i = 0; i < extBind.PropertyCount; i++)
                //        {
                //            propBind = extBind.GetPropertyBinding(i);
                //            if (propBind.TypeCode == DbTypeCode.Object)
                //            {
                //                if (propBind.TypeBinding != null)
                //                {
                //                    strBuilder.Append("property(\'" + extBind.Name + "\',\'" + propBind.Name.ToUpperInvariant() + "\',\'" + propBind.Name +
                //                        "\',\'" + propBind.TypeBinding.Name + "\');");
                //                }
                //                else
                //                {
                //                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Object reference type without type binding.");
                //                }
                //            }
                //            else
                //            {
                //                strBuilder.Append("property(\'" + extBind.Name + "\',\'" + propBind.Name.ToUpperInvariant() + "\',\'" + propBind.Name + "\',\'" +
                //                    propBind.TypeCode.ToString() + "\');");
                //            }
                //        }
                //    }
                //}

                //// Info about method EqualsOrIsDerivedFrom(Object).
                //strBuilder.Append("method(\'" + typeBind.Name + "\',\'EQUALSORISDERIVEDFROM\',\'EqualsOrIsDerivedFrom\',[\'Starcounter.IObjectView\'],\'Boolean\');");

                //// Info about generic method GetExtension<Type>().
                //extEnumerator.Reset();
                //while (extEnumerator.MoveNext())
                //{
                //    extBind = extEnumerator.Current;
                //    strBuilder.Append("gmethod(\'" + typeBind.Name + "\',\'GETEXTENSION\',\'GetExtension\',[\'" + extBind.Name + "\'],[],\'" + extBind.Name + "\');");
                //}
            }
            finally
            {
                //    if (extEnumerator != null)
                //        extEnumerator.Dispose();
            }

            return strBuilder.ToString();
        }

        private static void CallSqlProcessToAddSchemaInfo(Scheduler scheduler, String schemaInfo)
        {
            PrologSession session = null;
            se.sics.prologbeans.Bindings bindings = null;
            QueryAnswer answer = null;
            Int32 loopCount = 0;

            while (loopCount < QueryModule.MaxQueryRetries)
            {
                try
                {
                    EstablishConnectedSession(ref session, scheduler);
                    bindings = new se.sics.prologbeans.Bindings();
                    bindings.bind("SchemaInfo", schemaInfo);
                    answer = session.executeQuery("add_schemainfo_prolog(SchemaInfo)", bindings);
                    CheckQueryAnswerForError(answer);
                    loopCount = QueryModule.MaxQueryRetries;
                }
                catch (Exception exception)
                {
                    loopCount++;
                    if (loopCount < QueryModule.MaxQueryRetries)
                    {
                        logSource.LogWarning("Failed once to add schema info: " + schemaInfo, exception);
                    }
                    else
                    {
                        LeaveConnectedSession(session, scheduler);
                        throw exception;
                    }
                }
            }
            LeaveConnectedSession(session, scheduler);
        }

        private static void WriteSchemaInfoToFile(String schemaFilePath, TypeDef[] typeDefArray)
        {
            StreamWriter streamWriter = null;
            //IEnumerator<ExtensionBinding> extEnumerator = null;
            TypeDef typeDef = null;
            //ExtensionBinding extBind = null;
            PropertyDef propDef = null;

            Int32 tickCount = Environment.TickCount;
            try
            {
                streamWriter = new StreamWriter(schemaFilePath);

                // Set meta-info of the current schema export.
                streamWriter.WriteLine("/* THIS FILE WAS AUTO-GENERATED. DO NOT EDIT! */");
                streamWriter.WriteLine(":- multifile schemafile/1, class/3, extension/3, property/4, method/5, gmethod/6.");
                streamWriter.WriteLine(":- dynamic schemafile/1, class/3, extension/3, property/4, method/5, gmethod/6.");
                streamWriter.WriteLine(":- assert(schemafile('" + schemaFilePath + "')).");

                // Export information about classes (tables).
                streamWriter.WriteLine("/* class(fullClassNameUpper,fullClassName,baseClassName). */");
                streamWriter.WriteLine("/* class(shortClassNameUpper,fullClassName,baseClassName). */");
                String fullClassNameUpper = null;
                String shortClassNameUpper = null;
                for (Int32 i = 0; i < typeDefArray.Length; i++)
                {
                    typeDef = typeDefArray[i];
                    fullClassNameUpper = typeDef.Name.ToUpperInvariant();
                    shortClassNameUpper = GetShortName(typeDef.Name).ToUpperInvariant();
                    if (typeDef.BaseName != null)
                    {
                        streamWriter.WriteLine(":- assert(class('" + fullClassNameUpper + "','" + typeDef.Name + "','" + typeDef.BaseName + "')).");
                        if (shortClassNameUpper != fullClassNameUpper)
                            streamWriter.WriteLine(":- assert(class('" + shortClassNameUpper + "','" + typeDef.Name + "','" + typeDef.BaseName + "')).");
                    }
                    else
                    {
                        streamWriter.WriteLine(":- assert(class('" + fullClassNameUpper + "','" + typeDef.Name + "','none')).");
                        if (shortClassNameUpper != fullClassNameUpper)
                            streamWriter.WriteLine(":- assert(class('" + shortClassNameUpper + "','" + typeDef.Name + "','none')).");
                    }
                }

                //// Export information about extensions.
                //streamWriter.WriteLine("/* extension(fullClassName,fullExtensionNameUpper,fullExtensionName). */");
                //streamWriter.WriteLine("/* extension(fullClassName,shortExtensionNameUpper,fullExtensionName). */");
                //String fullExtensionNameUpper = null;
                //String shortExtensionNameUpper = null;
                //typeEnumerator.Reset();
                //while (typeEnumerator.MoveNext())
                //{
                //    typeDef = typeEnumerator.Current;
                //    extEnumerator = typeDef.GetAllExtensionBindings();
                //    if (extEnumerator != null)
                //    {
                //        while (extEnumerator.MoveNext())
                //        {
                //            extBind = extEnumerator.Current;
                //            fullExtensionNameUpper = extBind.Name.ToUpperInvariant();
                //            shortExtensionNameUpper = GetShortName(extBind.Name).ToUpperInvariant();
                //            streamWriter.WriteLine(":- assert(extension('" + typeDef.Name + "','" + fullExtensionNameUpper + "','" + extBind.Name + "')).");
                //            if (shortExtensionNameUpper != fullExtensionNameUpper)
                //                streamWriter.WriteLine(":- assert(extension('" + typeDef.Name + "','" + shortExtensionNameUpper + "','" + extBind.Name + "')).");
                //        }
                //    }
                //}

                // Export information about properties (columns).
                streamWriter.WriteLine("/* property(fullClassName,propertyNameUpper,propertyName,propertyType). */");
                for (Int32 i = 0; i < typeDefArray.Length; i++)
                {
                    typeDef = typeDefArray[i];
                    for (Int32 j = 0; j < typeDef.PropertyDefs.Length; j++)
                    {
                        propDef = typeDef.PropertyDefs[j];
                        if (propDef.Type == DbTypeCode.Object)
                        {
                            if (propDef.TargetTypeName != null)
                            {
                                streamWriter.WriteLine(":- assert(property('" + typeDef.Name + "','" + propDef.Name.ToUpperInvariant() + "','" +
                                    propDef.Name + "','" + propDef.TargetTypeName + "')).");
                            }
                            else
                            {
                                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Object reference type without type binding.");
                            }
                        }
                        else
                        {
                            streamWriter.WriteLine(":- assert(property('" + typeDef.Name + "','" + propDef.Name.ToUpperInvariant() + "','" +
                                propDef.Name + "','" + propDef.Type.ToString() + "')).");
                        }
                    }
                }

                //// Export information about extension properties (columns).
                //typeEnumerator.Reset();
                //while (typeEnumerator.MoveNext())
                //{
                //    typeDef = typeEnumerator.Current;
                //    extEnumerator = typeDef.GetAllExtensionBindings();
                //    if (extEnumerator != null)
                //    {
                //        while (extEnumerator.MoveNext())
                //        {
                //            extBind = extEnumerator.Current;
                //            for (Int32 i = 0; i < extBind.PropertyCount; i++)
                //            {
                //                propDef = extBind.GetPropertyBinding(i);
                //                if (propDef.TypeCode == DbTypeCode.Object)
                //                {
                //                    if (propDef.TypeBinding != null)
                //                    {
                //                        streamWriter.WriteLine(":- assert(property('" + extBind.Name + "','" + propDef.Name.ToUpperInvariant() + "','" +
                //                            propDef.Name + "','" + propDef.TypeBinding.Name + "')).");
                //                    }
                //                    else
                //                    {
                //                        throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Object reference type without type binding.");
                //                    }
                //                }
                //                else
                //                {
                //                    streamWriter.WriteLine(":- assert(property('" + extBind.Name + "','" + propDef.Name.ToUpperInvariant() + "','" +
                //                        propDef.Name + "','" + propDef.TypeCode.ToString() + "')).");
                //                }
                //            }
                //        }
                //    }
                //}

                //// Export information about method EqualsOrIsDerivedFrom(Object).
                //streamWriter.WriteLine("/* method(fullClassName,methodNameUpper,methodName,argumentTypes,returnType). */");
                //typeEnumerator.Reset();
                //while (typeEnumerator.MoveNext())
                //{
                //    typeDef = typeEnumerator.Current;
                //    streamWriter.WriteLine(":- assert(method('" + typeDef.Name + "','EQUALSORISDERIVEDFROM','EqualsOrIsDerivedFrom',['Starcounter.IObjectView'],'Boolean')).");
                //}

                //// Export information about generic method GetExtension<Type>().
                //streamWriter.WriteLine("/* gmethod(fullClassName,methodNameUpper,methodName,typeParameters,argumentTypes,returnType)). */");
                //typeEnumerator.Reset();
                //while (typeEnumerator.MoveNext())
                //{
                //    typeDef = typeEnumerator.Current;
                //    extEnumerator = typeDef.GetAllExtensionBindings();
                //    while (extEnumerator.MoveNext())
                //    {
                //        extBind = extEnumerator.Current;
                //        streamWriter.WriteLine(":- assert(gmethod('" + typeDef.Name + "','GETEXTENSION','GetExtension',['" +
                //            extBind.Name + "'],[],'" + extBind.Name + "')).");
                //    }
                //}

                tickCount = Environment.TickCount - tickCount;
                logSource.Debug("Exported SQL schema to " + schemaFilePath + " in " + tickCount.ToString() + " ms.");
                //logSource.LogNotice("Exported SQL schema info to " + schemaFilePath + " in " + tickCount.ToString() + " ms.");
            }
            finally
            {
                if (streamWriter != null)
                {
                    streamWriter.Close();
                }
            }
        }

        private static void CallSqlProcessToLoadSchemaInfo(Scheduler scheduler, String schemaFilePath)
        {
            PrologSession session = null;
            se.sics.prologbeans.Bindings bindings = null;
            QueryAnswer answer = null;
            Int32 loopCount = 0;

            while (loopCount < QueryModule.MaxQueryRetries)
            {
                try
                {
                    EstablishConnectedSession(ref session, scheduler);
                    bindings = new se.sics.prologbeans.Bindings();
                    bindings.bind("SchemaFile", schemaFilePath);
                    answer = session.executeQuery("load_schemainfo_prolog(SchemaFile)", bindings);
                    CheckQueryAnswerForError(answer);
                    loopCount = QueryModule.MaxQueryRetries;
                }
                catch (Exception exception)
                {
                    loopCount++;
                    if (loopCount < QueryModule.MaxQueryRetries)
                    {
                        logSource.LogWarning("Failed once to load schema file: " + schemaFilePath, exception);
                    }
                    else
                    {
                        LeaveConnectedSession(session, scheduler);
                        throw exception;
                    }
                }
            }
            LeaveConnectedSession(session, scheduler);
        }

        private static List<String> GetCurrentSqlSchemaFiles(Scheduler scheduler)
        {
            PrologSession session = null;
            QueryAnswer answer = null;
            Int32 loopCount = 0;

            while (loopCount < QueryModule.MaxQueryRetries)
            {
                try
                {
                    EstablishConnectedSession(ref session, scheduler);
                    answer = session.executeQuery("current_schemafiles_prolog(SchemaFiles)");
                    CheckQueryAnswerForError(answer);
                    loopCount = QueryModule.MaxQueryRetries;
                }
                catch (Exception exception)
                {
                    loopCount++;
                    if (loopCount < QueryModule.MaxQueryRetries)
                    {
                        logSource.LogWarning("Failed once to verify schema.", exception);
                    }
                    else
                    {
                        LeaveConnectedSession(session, scheduler);
                        throw exception;
                    }
                }
            }
            LeaveConnectedSession(session, scheduler);

            List<String> schemaFileList = new List<String>();
            Term cursor = answer.getValue("SchemaFiles");
            while (cursor.List && cursor.Name != "[]")
            {
                schemaFileList.Add(cursor.getArgument(1).Name);
                cursor = cursor.getArgument(2);
            }
            return schemaFileList;
        }

        private static Boolean StringListEqual(List<String> list1, List<String> list2)
        {
            if (list1.Count != list2.Count)
                return false;

            for (Int32 i = 0; i < list1.Count; i++)
            {
                if (list1[i] != list2[i])
                    return false;
            }

            return true;
        }

        [PermissionSet(SecurityAction.Assert, Unrestricted = true)]
        internal static IExecutionEnumerator ProcessSqlQuery(Scheduler scheduler, String query)
        {
            // Since the scheduler.PrologSession is shared between all the threads
            // managed by the same scheduler, this method must be called within
            // the scope of a yield block. 

            if (query == null)
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect query.");
            }
            if (query.Length > QueryModule.MaxQueryLength)
            {
                throw ErrorCode.ToException(Error.SCERRQUERYSTRINGTOOLONG, "Query string longer than maximal length of " + QueryModule.MaxQueryLength + " characters.");
            }
            if (query == "")
            {
                query = " ";
            }

            QueryAnswer answer = null;
            Int32 loopCount = 0;
            PrologSession session = null;
            se.sics.prologbeans.Bindings bindings = null;

            // Try maximum maxQueryRetries times to process the query.
            while (loopCount < QueryModule.MaxQueryRetries)
            {
                try
                {
                    EstablishConnectedSession(ref session, scheduler);
                    bindings = new se.sics.prologbeans.Bindings();
                    bindings.bind("Query", query);
                    answer = session.executeQuery("sql_prolog(Query,TypeDef,ExecInfo,VarNum,ErrList)", bindings);
                    CheckQueryAnswerForError(answer);
                    loopCount = QueryModule.MaxQueryRetries;
                }
                catch (Exception exception)
                {
                    loopCount++;
                    if (loopCount < QueryModule.MaxQueryRetries)
                    {
                        logSource.LogWarning("Failed to process query: " + query, exception);
                        EstablishSqlProcess();
                        logSource.LogWarning("Restarted process: " + QueryModule.ProcessFolder + QueryModule.ProcessFileName + " " + 
                            QueryModule.ProcessPort + " " + schemaFolderExternal);
                        ReExportAllSchemaInfo(scheduler, schemaFilePathList);
                    }
                    else
                    {
                        LeaveConnectedSession(session, scheduler);
                        throw;
                    }
                }
            }
            LeaveConnectedSession(session, scheduler);

            // Check for errors.
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
