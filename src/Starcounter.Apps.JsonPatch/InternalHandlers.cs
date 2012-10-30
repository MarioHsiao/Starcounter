// ***********************************************************************
// <copyright file="InternalHandlers.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Text;
using HttpStructs;
using Starcounter.Internal.Application;
using Starcounter.Internal.Web;

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
            Console.WriteLine("Registering internal handlers for patch and getting root apps");

            GET("/__vm/@s", (string sessionId) =>
            {
                HttpResponse response = null;
                Session s = HardcodedStuff.Here.Sessions.GetSession(sessionId);

                s.Execute(HardcodedStuff.Here.HttpRequest, () => {
                    Byte[] json = s.RootApp.ToJsonUtf8(false);
                    response = new HttpResponse() { Uncompressed = HttpResponseBuilder.CreateMinimalOk200WithContent(json, 0, (uint)json.Length) };
                });

                return response;
            });

            PATCH("/__vm/@s", (string sessionId) =>
            {
                HttpResponse response = null;
                Session s = HardcodedStuff.Here.Sessions.GetSession(sessionId);

                s.Execute(HardcodedStuff.Here.HttpRequest, () =>
                {
                    App rootApp = Session.Current.RootApp;
                    HttpRequest request = Session.Current.HttpRequest;

                    JsonPatch.EvaluatePatches(request.GetBodyByteArray());

                    response = new HttpResponse();
                    response.Uncompressed = HttpPatchBuilder.CreateHttpPatchResponse(Session.Current._changeLog);
                });
                return response;
            });
        }
    }
}
