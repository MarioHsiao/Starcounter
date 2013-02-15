// ***********************************************************************
// <copyright file="InternalHandlers.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using Starcounter.Apps;
using Starcounter.Internal.Web;
using Starcounter.Templates;

namespace Starcounter.Internal.JsonPatch
{
    /// <summary>
    /// Class InternalHandlers
    /// </summary>
    public class InternalHandlers : App
    {
        /// <summary>
        /// Registers this instance.
        /// </summary>
        public static void Register()
        {
            GET<int>("/__vm/{?}", (int viewModelId) =>
            {
                App rootApp;
                Byte[] json;
                HttpResponse response = null;

                rootApp = Session.Current.GetRootApp(viewModelId);
                json = rootApp.ToJsonUtf8();
                response = new HttpResponse() { Uncompressed = HttpResponseBuilder.CreateMinimalOk200WithContent(json, 0, (uint)json.Length) };

                return response;
            });

            PATCH<int>("/__vm/{?}", (int viewModelId) =>
            {
                App rootApp;
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
                } catch (NotSupportedException nex) {
                    response.Uncompressed = HttpPatchBuilder.Create415Response(nex.Message);
                } catch (Exception ex) {
                    response.Uncompressed = HttpPatchBuilder.Create400Response(ex.Message);
                }
                return response;
            });
        }

        private static void RefreshAllValues(App app, ChangeLog log) {
            foreach (Template template in app.Template.Children) {
                if (!template.Bound)
                    continue;

                if (template is ObjArrTemplate) {
                    Listing l = app.GetValue((ObjArrTemplate)template);
                    foreach (App childApp in l) {
                        RefreshAllValues(childApp, log);
                    }
                    continue;
                }
                
                if (template is AppTemplate) {
                    RefreshAllValues(app.GetValue((AppTemplate)template), log);
                    continue;
                }
                
                if (template is ActionProperty)
                    continue;

                ChangeLog.UpdateValue(app, (Property)template);
            }
        }
    }
}
