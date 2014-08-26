﻿
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Starcounter.Rest;
using Starcounter.XSON;
using System.Text;

namespace Starcounter.Internal {

    /// <summary>
    /// Registers the built in REST handler allowing clients to communicate with
    /// the public Session data of a Starcounter application.
    /// </summary>
    internal static class PuppetRestHandler {
        private static JsonPatch jsonPatch = new JsonPatch();
        private static List<UInt16> registeredPorts = new List<UInt16>();

        internal static void Register(UInt16 defaultUserHttpPort) {
            Starcounter.Rest.UriInjectMethods.SetHandlerRegisteredCallback(HandlerRegistered);
        }

        /// <summary>
        /// Name of the WebSocket Json-Patch channel.
        /// </summary>
        static String JsonPatchWebSocketChannelName = "jsonpatchws";

        /// <summary>
        /// Handles incoming WebSocket byte data.
        /// </summary>
        /// <param name="bs"></param>
        /// <param name="ws"></param>
        static void HandleWebSocketJson(Byte[] bs, WebSocket ws) {

            Json root = null;
            Session session = (Session) ws.Session;

            try {

                // Checking if session is presented still.
                if (session == null) {
                    ws.Disconnect("No session found.");
                    return;
                }

                // Checking if session has a tree.
                root = session.GetFirstData();
                if (root == null) {
                    ws.Disconnect("Session does not contain any state (session.Data).");
                    return;
                }

                // Running patches evaluation.
                jsonPatch.EvaluatePatches(session, bs);

                // Getting changes from the root.
                Byte[] patchResponse;
                Int32 sizeBytes = jsonPatch.CreateJsonPatchBytes(session, false, out patchResponse);

                // Sending the patch bytes to the client.
                ws.Send(patchResponse, sizeBytes, ws.IsText);

                return;

            } catch (JsonPatchException nex) {

                ws.Disconnect(nex.Message + " Patch: " + nex.Patch);
                return;
            }

        }

        private static void HandlerRegistered(string uri, ushort port) {
            if (registeredPorts.Contains(port))
                return;

            // We add the internal handlers for stateful access to json-objects
            // for each new port that is used.
            registeredPorts.Add(port);

            Handle.PATCH(port, ScSessionClass.DataLocationUriPrefix + Handle.UriParameterIndicator, (Session session, Request request) => {
                Json root = null;

                try {
                    if (session == null)
                        return CreateErrorResponse(404, "No session found for the specified uri.");
//                    root = session.Data;
                    root = session.GetFirstData();
                    if (root == null)
                        return CreateErrorResponse(404, "Session does not contain any state (session.Data).");

                    IntPtr bodyPtr;
                    uint bodySize;
                    request.GetBodyRaw(out bodyPtr, out bodySize);
                    jsonPatch.EvaluatePatches(session, bodyPtr, (int)bodySize);

                    return root;
                } catch (JsonPatchException nex) {
                    return CreateErrorResponse(400, nex.Message + " Patch: " + nex.Patch);
                }
            }, new HandlerOptions() { HandlerLevel = 0 });

            Handle.GET(port, ScSessionClass.DataLocationUriPrefix + Handle.UriParameterIndicator, (Session session) => {
                Json root = null;

                if (session == null)
                    return CreateErrorResponse(404, "No session found for the specified uri.");
                root = session.Data;
                if (root == null)
                    return CreateErrorResponse(404, "Session does not contain any state (session.Data).");

                return new Response() {
                    BodyBytes = root.ToJsonUtf8(),
                    ContentType = MimeTypeHelper.MimeTypeAsString(MimeType.Application_Json)
                };
            });

            // Handler to process Json-Patch WebSocket Upgrade HTTP request! :)
            Handle.GET(port, ScSessionClass.DataLocationUriPrefix + "wsupgrade/" + Handle.UriParameterIndicator, (Request req, Session session) => {

                // Checking if its a WebSocket Upgrade request.
                if (req.WebSocketUpgrade) {

                    // Sending an upgrade (note that we attach the existing session).
                    req.SendUpgrade(JsonPatchWebSocketChannelName, 0, null, session);

                    return HandlerStatus.Handled;
                }

                return 513;
            });

            // Handling WebSocket JsonPatch string message.
            Handle.Socket(port, JsonPatchWebSocketChannelName, (String s, WebSocket ws) => {
                
                Byte[] dataBytes = Encoding.UTF8.GetBytes(s);

                // Calling bytes data handler.
                HandleWebSocketJson(dataBytes, ws);
            });

            // Handling WebSocket JsonPatch byte array.
            Handle.Socket(port, JsonPatchWebSocketChannelName, HandleWebSocketJson);

            // Handling JsonPatch WebSocket disconnect here.
            Handle.SocketDisconnect(port, JsonPatchWebSocketChannelName, (UInt64 cargoId, IAppsSession session) => {

                // Do nothing!
            });
        }

        private static Response CreateErrorResponse(int statusCode, string message) {
            var response = Response.FromStatusCode(statusCode);
            response.Body = message;
            return response;
        }
    }
}
