// ***********************************************************************
// <copyright file="InternalHandlers.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using Starcounter.Apps;
using Starcounter.Internal.Web;
using Starcounter.Templates;
using Starcounter.Advanced;
using Newtonsoft.Json;
using System.Collections.Specialized;
using System.Diagnostics;
using Starcounter.Binding;
using Codeplex.Data;
using System.Threading;

namespace Starcounter.Internal.JsonPatch {
    /// <summary>
    /// Class InternalHandlers
    /// </summary>
    public class InternalHandlers : Puppet {
        /// <summary>
        /// Registers this instance.
        /// </summary>
        public static void Register() {
            GET("/__vm/{?}", (int viewModelId) => {
                Puppet rootApp;
                Byte[] json;
                HttpResponse response = null;

                rootApp = Session.Current.GetRootApp(viewModelId);
                json = rootApp.ToJsonUtf8();
                response = new HttpResponse() { Uncompressed = HttpResponseBuilder.CreateMinimalOk200WithContent(json, 0, (uint)json.Length) };

                return response;
            });

            Debug.Assert(Db.Environment != null, "Db.Environment is not initlized");
            Debug.Assert(string.IsNullOrEmpty(Db.Environment.DatabaseName) == false, "Db.Environment.DatabaseName is empty or null");

            if (Db.Environment.HasDatabase) {

                Console.WriteLine("Database {0} is listening for SQL commands.", Db.Environment.DatabaseName);

                // SQL command
                POST( "/__sql/" + Db.Environment.DatabaseName.ToLower(), (HttpRequest r) => {
                    try {
                        string bodyData = r.GetBodyStringUtf8_Slow();   // Retrice the sql command in the body
                        string resultJson = ExecuteQuery(bodyData);
                        return resultJson;
                    }
                    catch (Starcounter.SqlException sqle) {
                        return sqle.Message;
                    }
                    catch (Exception e) {
                        return e.ToString();
                    }


                });
            }

            PATCH("/__vm/{?}", (int viewModelId, HttpRequest request) => {
                Puppet rootApp;
                Session session;
                HttpResponse response = null;

                response = new HttpResponse();
                try {
                    session = Session.Current;
                    rootApp = session.GetRootApp(viewModelId);

                    JsonPatch.EvaluatePatches(rootApp, request.GetBodyByteArray_Slow());

                    // TODO:
                    // Quick and dirty hack to autorefresh dependent properties that might have been 
                    // updated. This implementation should be removed after the demo.
                    RefreshAllValues(rootApp, session.changeLog);

                    response.Uncompressed = HttpPatchBuilder.CreateHttpPatchResponse(session.changeLog);
                }
                catch (NotSupportedException nex) {
                    response.Uncompressed = HttpPatchBuilder.Create415Response(nex.Message);
                }
                catch (Exception ex) {
                    response.Uncompressed = HttpPatchBuilder.Create400Response(ex.Message);
                }
                return response;
            });
        }

        private static void RefreshAllValues(Puppet app, ChangeLog log) {
            foreach (Template template in app.Template.Children) {
                TValue tv = template as TValue;
                if (tv != null && !tv.Bound)
                    continue;

                if (template is TObjArr) {
                    Arr l = app.Get((TObjArr)template);
                    foreach (Puppet childApp in l) {
                        RefreshAllValues(childApp, log);
                    }
                    continue;
                }

                if (template is TPuppet) {
                    RefreshAllValues((Puppet)app.Get((TPuppet)template), log);
                    continue;
                }

                if (template is TTrigger)
                    continue;

                ChangeLog.UpdateValue(app, (TValue)template);
            }
        }


        private static string ExecuteQuery(string query) {

            Starcounter.SqlEnumerator<object> sqle = null;
            ITypeBinding resultBinding;
            IPropertyBinding propertyBinding;
            IPropertyBinding[] props;

            try {

                dynamic resultJson = new DynamicJson();

                sqle = (Starcounter.SqlEnumerator<object>)Db.SQL(query).GetEnumerator();

                // Retrive Columns
                resultJson.columns = new object[] { };

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

                #region Retrive Rows
                resultJson.rows = new object[] { };
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

                // empty error message
                resultJson.error = new object { };

                return resultJson.ToString();

            }
            catch (Exception e) {

                dynamic resultJson = new DynamicJson();
                resultJson.columns = new object[] { };
                resultJson.rows = new object[] { };

                resultJson.error = new { message = e.Message, helplink = e.HelpLink };
                //resultJson.error["Message"] = e.Message;
                //resultJson.error["HelpLink"] = e.HelpLink;
                return resultJson.ToString();

            }
            finally {
                if (sqle != null)
                    sqle.Dispose();
            }

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
