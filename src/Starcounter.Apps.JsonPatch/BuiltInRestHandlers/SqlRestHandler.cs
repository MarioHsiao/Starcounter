﻿// ***********************************************************************
// <copyright file="InternalHandlers.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using Codeplex.Data;
using Starcounter.Binding;
using Starcounter.Query.Execution;
using Starcounter.Templates;

namespace Starcounter.Internal {

    /// <summary>
    /// Every Starcounter application comes with built in REST handlers to allow communication
    /// with public Session.Data (aka. view-models or puppets) objects and/or with a potentially exposed SQL engine.
    /// </summary>
    public static class SqlRestHandler {

        /// <summary>
        /// Registers the built in REST handlers.
        /// </summary>
        /// <param name="defaultUserHttpPort">The public session data objects (view-models) are accessed using the same port as the user code REST handlers</param>
        /// <param name="defaultSystemHttpPort">The SQL access uses the system port</param>
        public static void Register(UInt16 defaultUserHttpPort, UInt16 defaultSystemHttpPort) {
            string dbName = Db.Environment.DatabaseNameLower;

            Debug.Assert(Db.Environment != null, "Db.Environment is not initialized");
            Debug.Assert(string.IsNullOrEmpty(Db.Environment.DatabaseNameLower) == false, "Db.Environment.DatabaseName is empty or null");

            Handle.GET(defaultSystemHttpPort, ScSessionClass.DataLocationUriPrefix + "sessions", () => {
                // Collecting number of sessions on all schedulers.
                return "Active sessions for '" + dbName + "':" + Environment.NewLine +
                    GlobalSessions.AllGlobalSessions.GetActiveSessionsStats();
            });

            if (Db.Environment.HasDatabase) {
                Console.WriteLine("Database {0} is listening for SQL commands.", Db.Environment.DatabaseNameLower);
                Handle.POST(defaultSystemHttpPort, ScSessionClass.DataLocationUriPrefix + "sql", (Request req) => {
                    SqlQueryResult result = null;
                    Db.Transaction(() => {
                        result = ExecuteQuery(req.Body);
                    }, false, 0);
                    return result;
                });
            }


        }

        /// <summary>
        /// Executes the query and returns a json string of the result
        /// </summary>
        /// <param name="query"></param>
        /// <returns>SqlQueryResult</returns>
        private static SqlQueryResult ExecuteQuery(string query) {

            Starcounter.SqlEnumerator<object> sqle = null;
            ITypeBinding resultBinding;
            IPropertyBinding[] props;

            SqlQueryResult results = new SqlQueryResult();

            TObject rowObjectTemplate = new TObject();
            TObjArr rowsTemplate = rowObjectTemplate.Add<TObjArr>("rows");
            TObject rowItemTemplate = new TObject();
            rowsTemplate.ElementType = rowItemTemplate;

            results.rows = (Json)rowObjectTemplate.CreateInstance();

			Arr<Json> rowArr = (Arr<Json>)rowsTemplate.Getter(results.rows);

            try {

                QueryResultRows<dynamic> sqlResult = Db.SlowSQL(query);

                if (sqlResult != null) {
                    sqle = (Starcounter.SqlEnumerator<object>)sqlResult.GetEnumerator();

                    results.queryPlan = sqle.ToString();

                    #region Retrive Columns

                    if (sqle.ProjectionTypeCode != null) {

                        SingleProjectionBinding singleProjectionBinding = new SingleProjectionBinding() { TypeCode = (DbTypeCode)sqle.ProjectionTypeCode };

                        props = new IPropertyBinding[1];
                        props[0] = singleProjectionBinding;

                        singleProjectionBinding.Name = sqle.ProjectionTypeCode.ToString();
                        //                      resultJson.columns[0] = new { title = props[0].Name, value = props[0].Name, type = props[0].TypeCode.ToString() };

                        var col = results.columns.Add();
                        col.title = props[0].Name;
                        col.value = props[0].Name;
                        col.type = props[0].TypeCode.ToString();

                        AddProperty(rowItemTemplate, col, props[0].TypeCode);
                    }
                    else {
                        resultBinding = sqle.TypeBinding;
                        props = new IPropertyBinding[resultBinding.PropertyCount];
                        for (int i = 0; i < resultBinding.PropertyCount; i++) {
                            PropertyMapping pm = (PropertyMapping)resultBinding.GetPropertyBinding(i);
                            props[i] = pm;
                            //                            resultJson.columns[i] = new { title = pm.DisplayName, value = "_" + props[i].Name, type = props[i].TypeCode.ToString() };

                            var col = results.columns.Add();
                            col.title = pm.DisplayName;
                            col.value = "_" + props[i].Name;
                            col.type = props[i].TypeCode.ToString();

                            AddProperty(rowItemTemplate, col, props[i].TypeCode);
                        }
                    }
                    #endregion

                    #region Retrive Rows
                    int index = 0;
                    while (sqle.MoveNext()) {

                        object row = sqle.Current;
                        var jsonRow = rowArr.Add();

                        if (sqle.ProjectionTypeCode != null) {
                            #region GetValue
                            switch (sqle.ProjectionTypeCode) {
                                //case DbTypeCode.Binary:
                                //    value = obj.GetBinary(prop.Index);
                                //    break;
                                case DbTypeCode.Boolean:
                                    ((TBool)rowItemTemplate.Properties[0]).Setter(jsonRow, (bool)row);
                                    break;
                                case DbTypeCode.Byte:
                                    ((TLong)rowItemTemplate.Properties[0]).Setter(jsonRow, (long)row);
                                    break;
                                case DbTypeCode.DateTime:
									((TString)rowItemTemplate.Properties[0]).Setter(jsonRow, ((DateTime)row).ToString());
                                    break;
                                case DbTypeCode.Decimal:
                                    ((TDecimal)rowItemTemplate.Properties[0]).Setter(jsonRow, (Decimal)row);
                                    break;
                                case DbTypeCode.Double:
                                    ((TDouble)rowItemTemplate.Properties[0]).Setter(jsonRow, (Double)row);
                                    break;
                                case DbTypeCode.Int16:
									((TLong)rowItemTemplate.Properties[0]).Setter(jsonRow, (long)row);
                                    break;
                                case DbTypeCode.Int32:
									((TLong)rowItemTemplate.Properties[0]).Setter(jsonRow, (long)row);
                                    break;
                                case DbTypeCode.Int64:
									((TLong)rowItemTemplate.Properties[0]).Setter(jsonRow, (long)row);
                                    break;
                                case DbTypeCode.Object:
                                    IObjectView value = (IObjectView)row;
                                    if (value != null) {
                                        ((TString)rowItemTemplate.Properties[0]).Setter(jsonRow, DbHelper.GetObjectNo(value).ToString());
                                    }
                                    break;
                                case DbTypeCode.SByte:
									((TLong)rowItemTemplate.Properties[0]).Setter(jsonRow, (long)row);
                                    break;
                                case DbTypeCode.Single:
									((TDouble)rowItemTemplate.Properties[0]).Setter(jsonRow, (Double)(Single)row);
                                    break;
                                case DbTypeCode.String:
									((TString)rowItemTemplate.Properties[0]).Setter(jsonRow, (String)row);
                                    break;
                                case DbTypeCode.UInt16:
									((TLong)rowItemTemplate.Properties[0]).Setter(jsonRow, (long)row);
                                    break;
                                case DbTypeCode.UInt32:
									((TLong)rowItemTemplate.Properties[0]).Setter(jsonRow, (long)row);
                                    break;
                                case DbTypeCode.UInt64:
									var col = rowItemTemplate.Properties[0];
									if (col is TString) {
										((TString)col).Setter(jsonRow, row.ToString());
									} else {
										((TLong)col).Setter(jsonRow, (long)row);
									}
                                    break;
                                default:
                                    throw new NotImplementedException(string.Format("The handling of the TypeCode {0} has not yet been implemented", sqle.ProjectionTypeCode.ToString()));
                            }
                            #endregion
                        }
                        else {

                            IObjectView obj = (IObjectView)row;

                            IPropertyBinding prop;
                            for (int pi = 0; pi < props.Length; pi++) {
                                prop = props[pi];

                                switch (prop.TypeCode) {
                                    #region GetValue
                                    //case DbTypeCode.Binary:
                                    //    value = obj.GetBinary(prop.Index);
                                    //    break;
                                    case DbTypeCode.Boolean:
                                        ((TBool)rowItemTemplate.Properties[pi]).Setter(jsonRow, (bool)obj.GetBoolean(prop.Index));
                                        break;
                                    case DbTypeCode.Byte:
                                        ((TLong)rowItemTemplate.Properties[pi]).Setter(jsonRow, (long)obj.GetByte(prop.Index));
                                        break;
                                    case DbTypeCode.DateTime:
                                        ((TString)rowItemTemplate.Properties[pi]).Setter(jsonRow, obj.GetDateTime(prop.Index).ToString());
                                        break;
                                    case DbTypeCode.Decimal:
                                        ((TDecimal)rowItemTemplate.Properties[pi]).Setter(jsonRow, (Decimal)obj.GetDecimal(prop.Index));
                                        break;
                                    case DbTypeCode.Double:
                                        ((TDouble)rowItemTemplate.Properties[pi]).Setter(jsonRow, (Double)obj.GetDouble(prop.Index));
                                        break;
                                    case DbTypeCode.Int16:
                                        ((TLong)rowItemTemplate.Properties[pi]).Setter(jsonRow, (long)obj.GetInt16(prop.Index));
                                        break;
                                    case DbTypeCode.Int32:
                                        ((TLong)rowItemTemplate.Properties[pi]).Setter(jsonRow, (long)obj.GetInt32(prop.Index));
                                        break;
                                    case DbTypeCode.Int64:
                                        ((TLong)rowItemTemplate.Properties[pi]).Setter(jsonRow, (long)obj.GetInt64(prop.Index));
                                        break;
                                    case DbTypeCode.Object:

                                        IObjectView value = obj.GetObject(prop.Index);
                                        if (value != null) {
                                            ((TString)rowItemTemplate.Properties[pi]).Setter(jsonRow, DbHelper.GetObjectNo(value).ToString());
                                        }
                                        break;
                                    case DbTypeCode.SByte:
                                        ((TLong)rowItemTemplate.Properties[pi]).Setter(jsonRow, (long)obj.GetSByte(prop.Index));
                                        break;
                                    case DbTypeCode.Single:
                                        ((TDouble)rowItemTemplate.Properties[pi]).Setter(jsonRow, (Double)obj.GetSingle(prop.Index));
                                        break;
                                    case DbTypeCode.String:
                                        ((TString)rowItemTemplate.Properties[pi]).Setter(jsonRow, obj.GetString(prop.Index));
                                        break;
                                    case DbTypeCode.UInt16:
                                        ((TLong)rowItemTemplate.Properties[pi]).Setter(jsonRow, (long)obj.GetUInt16(prop.Index));
                                        break;
                                    case DbTypeCode.UInt32:
                                        ((TLong)rowItemTemplate.Properties[pi]).Setter(jsonRow, (long)obj.GetUInt32(prop.Index));
                                        break;
                                    case DbTypeCode.UInt64:
										var col = rowItemTemplate.Properties[pi];
										if (col is TString) {
											((TString)col).Setter(jsonRow, obj.GetUInt64(prop.Index).ToString());
										} else {
											((TLong)col).Setter(jsonRow, (long)obj.GetUInt64(prop.Index));
										}
										break;
                                    default:
                                        throw new NotImplementedException(string.Format("The handling of the TypeCode {0} has not yet been implemented", prop.TypeCode.ToString()));
                                    #endregion
                                }
                            }
                        }


                        index++;

                    }

                    #endregion
                }
            }
            catch (Starcounter.SqlException ee) {

                results.sqlException.beginPosition = ee.BeginPosition;
                results.sqlException.endPosition = ee.EndPosition;
                results.sqlException.helpLink = ee.HelpLink;
                results.sqlException.message = ee.Message;
                results.sqlException.scErrorCode = ee.ScErrorCode;
                results.sqlException.token = ee.Token;
                results.sqlException.stackTrace = ee.StackTrace;
                results.hasSqlException = true;
            }
            catch (Exception e) {

                LogSources.Sql.LogException(e);

                results.exception.helpLink = e.HelpLink;
                results.exception.message = e.GetType().ToString() + ": " + e.Message;
                results.exception.stackTrace = e.StackTrace;
                results.hasException = true;
            }
            finally {
                if (sqle != null)
                    sqle.Dispose();
            }

            return results;
        }

        private static void AddProperty(TObject parent, SqlQueryResult.columnsElementJson col, DbTypeCode dbTypeCode) {
            switch (dbTypeCode) {
                case DbTypeCode.Binary:
                case DbTypeCode.Object:
                    parent.Add<TString>(col.value);
                    break;
                case DbTypeCode.Boolean:
                    parent.Add<TBool>(col.value);
                    break;
                case DbTypeCode.Byte:
                case DbTypeCode.Int16:
                case DbTypeCode.Int32:
                case DbTypeCode.Int64:
                case DbTypeCode.SByte:
                case DbTypeCode.UInt16:
                case DbTypeCode.UInt32:
					parent.Add<TLong>(col.value);
                    break;

                case DbTypeCode.UInt64:
					if (col.title == "ObjectNo") {
						col.type = DbTypeCode.String.ToString();
						parent.Add<TString>(col.value);
					} else {
						parent.Add<TLong>(col.value);
					}
                    break;

                case DbTypeCode.DateTime:
                case DbTypeCode.String:
                    parent.Add<TString>(col.value);
                    break;
                case DbTypeCode.Decimal:
                    parent.Add<TDecimal>(col.value);
                    break;
                case DbTypeCode.Double:
                case DbTypeCode.Single:
                    parent.Add<TDouble>(col.value);
                    break;
                default: 
                    throw new NotSupportedException();
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

}
