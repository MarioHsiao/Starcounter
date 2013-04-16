// ***********************************************************************
// <copyright file="InternalHandlers.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using Starcounter.Internal.Web;
using Starcounter.Advanced;
using System.Diagnostics;
using Starcounter.Binding;
using Codeplex.Data;
using System.Net;
using HttpStructs;
using System.Collections.Generic;
using System.IO;
using Starcounter.Query.Execution;

namespace Starcounter.Internal.JsonPatch {
    /// <summary>
    /// Class InternalHandlers
    /// </summary>
    public class InternalHandlers {
        private static List<UInt16> registeredPorts = new List<UInt16>();

        private static StreamWriter consoleWriter;
        /// <summary>
        /// Registers this instance.
        /// </summary>
        public static void Register(UInt16 defaultUserHttpPort, UInt16 defaultSystemHttpPort) {
            string dbName = Db.Environment.DatabaseName.ToLower();

            Debug.Assert(Db.Environment != null, "Db.Environment is not initialized");
            Debug.Assert(string.IsNullOrEmpty(Db.Environment.DatabaseName) == false, "Db.Environment.DatabaseName is empty or null");

            HandlersManagement.SetHandlerRegisteredCallback(HandlerRegistered);

            Handle.GET(defaultSystemHttpPort, "/__" + dbName + "/sessions", () => {
                // Collecting number of sessions on all schedulers.
                return "Active sessions for '" + dbName + "':" + Environment.NewLine +
                    GlobalSessions.AllGlobalSessions.GetActiveSessionsStats();
            });

            if (Db.Environment.HasDatabase) {
                Console.WriteLine("Database {0} is listening for SQL commands.", Db.Environment.DatabaseName);
                Handle.POST(defaultSystemHttpPort, "/__" + dbName + "/sql", (Request req) => {
                    string bodyData = req.GetBodyStringUtf8_Slow();   // Retrieve the sql command in the body
                    return ExecuteQuery(bodyData);
                });
            }

            // Do not redirect the administrator console output
            if (!NewConfig.IsAdministratorApp) {

                Handle.GET(defaultSystemHttpPort, "/__" + dbName + "/console", (Request req) => {
                    //if (StringExistInList("application/json", req["Accept"])) {
                    return GetConsoleOutput();
                    //}
                    //else {
                    //    return HttpStatusCode.NotAcceptable;
                    //}
                });

                // Redirect console output to circular memory buffer
                InternalHandlers.consoleWriter = new StreamWriter(new CircularStream(1024)); // Console buffer size (TODO: Maybe a configuration option)
                InternalHandlers.consoleWriter.AutoFlush = true;
                Console.SetOut(InternalHandlers.consoleWriter);
            }

        }

        public static bool StringExistInList(string str, string list) {
            if (string.IsNullOrEmpty(list) || string.IsNullOrEmpty(str)) return false;
            string[] items = list.Split(',');
            foreach (string type in items) {
                if (string.Equals(type, str, StringComparison.CurrentCultureIgnoreCase)) {
                    return true;
                }
            }
            return false;
        }

        private static void HandlerRegistered(string uri, ushort port) {
            if (registeredPorts.Contains(port))
                return;

            // We add the internal handlers for stateful access to json-objects
            // for each new port that is used.
            registeredPorts.Add(port);
            string dbName = Db.Environment.DatabaseName.ToLower();

            Handle.GET(port, "/__" + dbName + "/{?}", (Session session) => {
                Obj json = Session.Data;
                if (json == null) {
                    return HttpStatusCode.NotFound;
                }

                return new Response() {
                    Uncompressed = HttpResponseBuilder.FromJsonUTF8Content(json.ToJsonUtf8())
                };
            });

            Handle.PATCH(port, "/__" + dbName + "/{?}", (Session session, Request request) => {
                Obj root;

                try {
                    root = Session.Data;
                    JsonPatch.EvaluatePatches(root, request.GetBodyByteArray_Slow());
                    return root;
                }
                catch (NotSupportedException nex) {
                    return new Response() { Uncompressed = HttpPatchBuilder.Create415Response(nex.Message) };
                }
                catch (Exception ex) {
                    return new Response() { Uncompressed = HttpPatchBuilder.Create400Response(ex.Message) };
                }
            });
        }

        private static string GetConsoleOutput() {

            dynamic resultJson = new DynamicJson();
            resultJson.console = null;
            resultJson.exception = null;

            try {
                CircularStream circularMemoryStream = (CircularStream)InternalHandlers.consoleWriter.BaseStream;
                byte[] buffer = new byte[circularMemoryStream.Length];
                int count = circularMemoryStream.Read(buffer, 0, (int)circularMemoryStream.Length);
                if (count > 0) {
                    resultJson.console = System.Text.Encoding.UTF8.GetString(buffer);
                }
            }
            catch (Exception e) {
                resultJson.exception = new { message = e.Message, helpLink = e.HelpLink, stackTrace = e.StackTrace };
            }

            return resultJson.ToString();

        }

        /// <summary>
        /// Executes the query and returns a json string of the result
        /// </summary>
        /// <param name="query"></param>
        /// <returns>Always a Json string</returns>
        private static string ExecuteQuery(string query) {

            Starcounter.SqlEnumerator<object> sqle = null;
            ITypeBinding resultBinding;
            IPropertyBinding[] props;

            dynamic resultJson = new DynamicJson();
            resultJson.columns = new object[] { };
            resultJson.rows = new object[] { };
            resultJson.exception = null;
            resultJson.sqlException = null;

            try {
                sqle = (Starcounter.SqlEnumerator<object>)Db.SlowSQL(query).GetEnumerator();

                #region Retrive Columns

                if (sqle.ProjectionTypeCode != null) {

                    SingleProjectionBinding singleProjectionBinding = new SingleProjectionBinding() { TypeCode = (DbTypeCode)sqle.ProjectionTypeCode };

                    props = new IPropertyBinding[1];
                    props[0] = singleProjectionBinding;

                    singleProjectionBinding.Name = sqle.ProjectionTypeCode.ToString();
                    resultJson.columns[0] = new { title = props[0].Name, value = props[0].Name, type = props[0].TypeCode.ToString() };
                }
                else {
                    resultBinding = sqle.TypeBinding;
                    props = new IPropertyBinding[resultBinding.PropertyCount];
                    for (int i = 0; i < resultBinding.PropertyCount; i++) {
                        PropertyMapping pm = (PropertyMapping)resultBinding.GetPropertyBinding(i);
                        props[i] = pm;
                        resultJson.columns[i] = new { title = pm.DisplayName, value = "_" + props[i].Name, type = props[i].TypeCode.ToString() };
                    }
                }
                #endregion

                #region Retrive Rows
                int index = 0;
                while (sqle.MoveNext()) {

                    object row = sqle.Current;
                    resultJson.rows[index] = new object { };

                    if (sqle.ProjectionTypeCode != null) {

                        if (sqle.ProjectionTypeCode == DbTypeCode.Object && row != null) {
                            resultJson.rows[index][props[0].Name] = DbHelper.GetObjectID(row);
                        }
                        else {
                            resultJson.rows[index][props[0].Name] = row;
                        }
                    }
                    else {

                        IObjectView obj = (IObjectView)row;

                        foreach (IPropertyBinding prop in props) {
                            object value = null;
                            switch (prop.TypeCode) {
                                case DbTypeCode.Binary:
                                    value = obj.GetBinary(prop.Index);
                                    break;
                                case DbTypeCode.Boolean:
                                    value = obj.GetBoolean(prop.Index);
                                    break;
                                case DbTypeCode.Byte:
                                    value = obj.GetByte(prop.Index);
                                    break;
                                case DbTypeCode.DateTime:
                                    value = obj.GetDateTime(prop.Index);
                                    break;
                                case DbTypeCode.Decimal:
                                    value = obj.GetDecimal(prop.Index);
                                    break;
                                case DbTypeCode.Double:
                                    value = obj.GetDouble(prop.Index);
                                    break;
                                case DbTypeCode.Int16:
                                    value = obj.GetInt16(prop.Index);
                                    break;
                                case DbTypeCode.Int32:
                                    value = obj.GetInt32(prop.Index);
                                    break;
                                case DbTypeCode.Int64:
                                    value = obj.GetInt64(prop.Index);
                                    break;
                                case DbTypeCode.LargeBinary:
                                    value = obj.GetBinary(prop.Index);
                                    break;
                                case DbTypeCode.Object:

                                    value = obj.GetObject(prop.Index);
                                    if (value != null) {
                                        value = DbHelper.GetObjectID(value);
                                    }
                                    break;
                                case DbTypeCode.SByte:
                                    value = obj.GetSByte(prop.Index);
                                    break;
                                case DbTypeCode.Single:
                                    value = obj.GetSingle(prop.Index);
                                    break;
                                case DbTypeCode.String:
                                    value = obj.GetString(prop.Index);
                                    break;
                                case DbTypeCode.UInt16:
                                    value = obj.GetUInt16(prop.Index);
                                    break;
                                case DbTypeCode.UInt32:
                                    value = obj.GetUInt32(prop.Index);
                                    break;
                                case DbTypeCode.UInt64:
                                    value = obj.GetUInt64(prop.Index);
                                    break;
                                default:
                                    // ERROR
                                    throw new Exception("Unknown column type");
                            }
                            resultJson.rows[index]["_" + prop.Name] = value;
                        }
                    }
                    index++;

                }

                #endregion

            }
            catch (Starcounter.SqlException ee) {

                resultJson.sqlException = new {
                    beginPosition = ee.BeginPosition,
                    endPosition = ee.EndPosition,
                    helpLink = ee.HelpLink,
                    message = ee.Message,
                    query = ee.Query,
                    scErrorCode = ee.ScErrorCode,
                    token = ee.Token,
                    stackTrace = ee.StackTrace
                };
            }
            catch (Exception e) {
                resultJson.exception = new { message = e.Message, helpLink = e.HelpLink, stackTrace = e.StackTrace };
            }
            finally {
                if (sqle != null)
                    sqle.Dispose();
            }

            return resultJson.ToString();
        }

        internal class SingleProjectionBinding : IPropertyBinding {
            private DbTypeCode typeCode;

            public bool AssertEquals(IPropertyBinding other) {
                throw new NotImplementedException();
            }

            public int Index {
                get { return 0; }
            }

            public string Name {
                get;
                set;
            }

            public string DisplayName { get; set; }

            public ITypeBinding TypeBinding {
                get { return null; }
            }

            public DbTypeCode TypeCode {
                get { return typeCode; }
                set { typeCode = value; }
            }
        }


    }


    internal class CircularStream : Stream {

        private long _Position = 0;
        private bool _IsBufferFull = false;
        private byte[] _Buffer;

        #region properties

        public override bool CanRead {
            get { return true; }
        }

        public override bool CanSeek {
            get { return false; }
        }

        public override bool CanWrite {
            get { return true; }
        }

        #endregion

        public CircularStream(long size) {
            if (size <= 0) throw new ArgumentException("size");
            this._Buffer = new byte[size];
        }

        public override void Flush() {

        }

        public override long Length {
            get {
                if (this._IsBufferFull) return this._Buffer.Length;
                return this._Position;
            }
        }

        public override long Position {
            get {
                throw new NotImplementedException();
            }
            set {
                throw new NotImplementedException();
            }
        }

        public override long Seek(long offset, SeekOrigin origin) {
            throw new NotImplementedException();
        }

        public override void SetLength(long value) {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count) {

            lock (this) {
                long pos = 0;
                if (this._IsBufferFull) {
                    pos = this._Position;
                }

                int i = 0;
                for (; i < count && i <= (this.Length - 1); i++) {
                    buffer[offset + i] = this._Buffer[pos++];
                    if (pos > (this._Buffer.Length - 1)) {
                        pos = 0;
                    }
                }
                return i;
            }
        }


        public override void Write(byte[] buffer, int offset, int count) {

            lock (this) {

                for (int i = 0; i < count; i++) {

                    _Buffer[this._Position++] = buffer[i];

                    if (this._Position == this._Buffer.Length) {
                        this._Position = 0;
                        this._IsBufferFull = true;
                    }
                }
            }

        }
    }

}
