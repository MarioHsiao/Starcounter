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

namespace Starcounter.Internal.JsonPatch {
    /// <summary>
    /// Class InternalHandlers
    /// </summary>
    public class InternalHandlers {
        /// <summary>
        /// Registers this instance.
        /// </summary>
        public static void Register(UInt16 defaultUserHttpPort, UInt16 defaultSystemHttpPort) {
            string dbName = Db.Environment.DatabaseName.ToLower();

            Debug.Assert(Db.Environment != null, "Db.Environment is not initialized");
            Debug.Assert(string.IsNullOrEmpty(Db.Environment.DatabaseName) == false, "Db.Environment.DatabaseName is empty or null");

            Handle.GET(defaultUserHttpPort, "/__" + dbName + "/{?}", (Session session) => {
                Obj json = Session.Data;
                if (json == null) {
                    return HttpStatusCode.NotFound;
                }

                return new HttpResponse() {
                    Uncompressed = HttpResponseBuilder.FromJsonUTF8Content(json.ToJsonUtf8())
                };
            });

            Handle.PATCH(defaultUserHttpPort, "/__" + dbName + "/{?}", (Session session, Request request) => {
                Obj root;

                try {
                    root = Session.Data;
                    JsonPatch.EvaluatePatches(root, request.GetContentByteArray_Slow());
                    return root;
                }
                catch (NotSupportedException nex) {
                    return new HttpResponse() { Uncompressed = HttpPatchBuilder.Create415Response(nex.Message) };
                }
                catch (Exception ex) {
                    return new HttpResponse() { Uncompressed = HttpPatchBuilder.Create400Response(ex.Message) };
                }
            });

            if (Db.Environment.HasDatabase) {
                Console.WriteLine("Database {0} is listening for SQL commands.", Db.Environment.DatabaseName);
                Handle.POST(defaultSystemHttpPort, "/__" + dbName + "/sql", (Request r) => {
                    string bodyData = r.GetContentStringUtf8_Slow();   // Retrieve the sql command in the body
                    return ExecuteQuery(bodyData);
                });
            }
        }


        /// <summary>
        /// Executes the query and returns a json string of the result
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        private static string ExecuteQuery(string query) {

            Starcounter.SqlEnumerator<object> sqle = null;
            ITypeBinding resultBinding;
            IPropertyBinding propertyBinding;
            IPropertyBinding[] props;

            dynamic resultJson = new DynamicJson();
            resultJson.columns = new object[] { };
            resultJson.rows = new object[] { };
            resultJson.exception = null; // new object { };
            resultJson.sqlexception = null; // new object { };

            try {
                sqle = (Starcounter.SqlEnumerator<object>)Db.SQL(query).GetEnumerator();

                #region Retrive Columns
                //resultJson.columns = new object[] { };

                if (sqle.ProjectionTypeCode != null && false) {
                    props = new IPropertyBinding[1];
                    propertyBinding = new SingleProjectionBinding() { TypeCode = (DbTypeCode)sqle.ProjectionTypeCode };
                    resultJson.columns[0] = new { title = propertyBinding.Name, value = propertyBinding.Name, type = propertyBinding.TypeCode.ToString() };
                }
                else {
                    resultBinding = sqle.TypeBinding;
                    props = new IPropertyBinding[resultBinding.PropertyCount];
                    for (int i = 0; i < resultBinding.PropertyCount; i++) {
                        props[i] = resultBinding.GetPropertyBinding(i);
                        resultJson.columns[i] = new { title = props[i].Name, value = props[i].Name, type = props[i].TypeCode.ToString() };
                    }
                }
                #endregion

                #region Retrive Rows
                // resultJson.rows = new object[] { };
                int index = 0;
                while (sqle.MoveNext()) {

                    if (sqle.ProjectionTypeCode != null) {
                        //this.QueryResult.Result.Add(new SqlRowApp(sqle.Current, props[0]));

                        IObjectView row = (IObjectView)sqle.Current;
                        resultJson.rows[index] = new object { };

                        foreach (IPropertyBinding prop in props) {
                            object value = null;
                            switch (prop.TypeCode) {
                                case DbTypeCode.Binary:
                                    value = row.GetBinary(prop.Index);
                                    break;
                                case DbTypeCode.Boolean:
                                    value = row.GetBoolean(prop.Index);
                                    break;
                                case DbTypeCode.Byte:
                                    value = row.GetByte(prop.Index);
                                    break;
                                case DbTypeCode.DateTime:
                                    value = row.GetDateTime(prop.Index);
                                    break;
                                case DbTypeCode.Decimal:
                                    value = row.GetDecimal(prop.Index);
                                    break;
                                case DbTypeCode.Double:
                                    value = row.GetDouble(prop.Index);
                                    break;
                                case DbTypeCode.Int16:
                                    value = row.GetInt16(prop.Index);
                                    break;
                                case DbTypeCode.Int32:
                                    value = row.GetInt32(prop.Index);
                                    break;
                                case DbTypeCode.Int64:
                                    value = row.GetInt64(prop.Index);
                                    break;
                                case DbTypeCode.LargeBinary:
                                    value = row.GetBinary(prop.Index);
                                    break;
                                case DbTypeCode.Object:
                                    value = row.GetObject(prop.Index);
                                    break;
                                case DbTypeCode.SByte:
                                    value = row.GetSByte(prop.Index);
                                    break;
                                case DbTypeCode.Single:
                                    value = row.GetSingle(prop.Index);
                                    break;
                                case DbTypeCode.String:
                                    value = row.GetString(prop.Index);
                                    break;
                                case DbTypeCode.UInt16:
                                    value = row.GetUInt16(prop.Index);
                                    break;
                                case DbTypeCode.UInt32:
                                    value = row.GetUInt32(prop.Index);
                                    break;
                                case DbTypeCode.UInt64:
                                    value = row.GetUInt64(prop.Index);
                                    break;
                                default:
                                    // ERROR
                                    throw new Exception("Unknown column type");

                            }
                            resultJson.rows[index][prop.Name] = value;
                        }
                    }
                    else {
                        // RODO:
                        //  this.QueryResult.Result.Add(new SqlRowApp(sqle.Current, props));
                    }

                    index++;
                }

                #endregion

            }
            catch (Starcounter.SqlException ee) {
                resultJson.sqlexception = new {
                    BeginPosition = ee.BeginPosition,
                    EndPosition = ee.EndPosition,
                    ErrorMessage = ee.ErrorMessage,
                    HelpLink = ee.HelpLink,
                    Message = ee.Message,
                    Query = ee.Query,
                    ScErrorCode = ee.ScErrorCode,
                    Token = ee.Token
                };
            }
            catch (Exception e) {
                resultJson.exception = new { message = e.Message, helplink = e.HelpLink };
                //resultJson.error["Message"] = e.Message;
                //resultJson.error["HelpLink"] = e.HelpLink;

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
                get { return "0"; }
            }

            public ITypeBinding TypeBinding {
                get { return null; }
            }

            public DbTypeCode TypeCode {
                get { return typeCode; }
                set { typeCode = value; }
            }
        }
    }
}
