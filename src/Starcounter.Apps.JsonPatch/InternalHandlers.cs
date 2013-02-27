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
                POST("/__sql/" + Db.Environment.DatabaseName, (HttpRequest r) => {

                    try {
                        //                    System.Uri myUri = new System.Uri("http://www.example.com" + r.Uri);
                        //                    NameValueCollection parameters = System.Web.HttpUtility.ParseQueryString(myUri.Query);
                        //                    string offset = parameters.Get("offset");
                        //                    string rows = parameters.Get("rows");
                        string bodyData = r.GetBodyStringUtf8_Slow();   // Retrice the sql command in the body
                        var sqlresult = Db.SQL(bodyData);

                        string result = JsonConvert.SerializeObject(sqlresult);
                        return result;
                    }
                    catch (Starcounter.SqlException sqle) {
                        return sqle.Message;
                    }
                    catch (Exception e) {
                        return e.ToString();
                    }

                });
            }

            PATCH("/__vm/{?}", (int viewModelId) => {
                Puppet rootApp;
                Session session;
                HttpResponse response = null;

                response = new HttpResponse();
                try {
                    session = Session.Current;
                    rootApp = session.GetRootApp(viewModelId);

                    JsonPatch.EvaluatePatches(rootApp, session.HttpRequest.GetBodyByteArray_Slow());

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
                if (!template.Bound)
                    continue;

                if (template is TObjArr) {
                    Arr l = app.GetValue((TObjArr)template);
                    foreach (Puppet childApp in l) {
                        RefreshAllValues(childApp, log);
                    }
                    continue;
                }

                if (template is TPuppet) {
                    RefreshAllValues((Puppet)app.GetValue((TPuppet)template), log);
                    continue;
                }

                if (template is TTrigger)
                    continue;

                ChangeLog.UpdateValue(app, (TValue)template);
            }
        }
    }
}
