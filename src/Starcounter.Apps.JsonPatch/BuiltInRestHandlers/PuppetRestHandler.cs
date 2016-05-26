
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Starcounter.Rest;
using Starcounter.XSON;
using System.Text;
using Starcounter.Advanced;

namespace Starcounter.Internal {

    /// <summary>
    /// Registers the built in REST handler allowing clients to communicate with
    /// the public Session data of a Starcounter application.
    /// </summary>
    internal static class PuppetRestHandler {

        private static JsonPatch jsonPatch = new JsonPatch();

        private static List<UInt16> registeredPorts = new List<UInt16>();

        /// <summary>
        /// Name of the WebSocket Json-Patch channel.
        /// </summary>
        static String JsonPatchWebSocketGroupName = "jsonpatchws";

        /// <summary>
        /// Handles incoming WebSocket byte data.
        /// </summary>
        /// <param name="bs"></param>
        /// <param name="ws"></param>
        static void HandleWebSocketJson(Byte[] bs, WebSocket ws) {

            // Incrementing the initial call level for handles.
            Handle.CallLevel++;

            Json root = null;
            Session session = (Session) ws.Session;

            try {

                // Checking if session is presented still.
                if (session == null) {
                    ws.Disconnect("No session found.", WebSocket.WebSocketCloseCodes.WS_CLOSE_UNEXPECTED_CONDITION);
                    return;
                }

                // Checking if session has a tree.
                root = session.PublicViewModel;
                if (root == null) {
                    ws.Disconnect("Session does not contain any state (session.Data).", WebSocket.WebSocketCloseCodes.WS_CLOSE_UNEXPECTED_CONDITION);
                    return;
                }

                // Running patches evaluation.
                int patchCount = jsonPatch.Apply(root, bs, session.CheckOption(SessionOptions.StrictPatchRejection));

                // -1 means that the patch was queued due to clientversion mismatch. We send no response.
                if (patchCount != -1) { 
                    // Getting changes from the root.
                    Byte[] patchResponse;
                    Int32 sizeBytes = jsonPatch.Generate(root, true, session.CheckOption(SessionOptions.IncludeNamespaces), out patchResponse);

                    // Sending the patch bytes to the client.
                    ws.Send(patchResponse, sizeBytes, ws.IsText);
                }
                return;
            } catch (JsonPatchException nex) {
                ws.Disconnect(nex.Message, WebSocket.WebSocketCloseCodes.WS_CLOSE_UNEXPECTED_CONDITION);
                return;
            } 
        }

        internal static void RegisterJsonPatchHandlers(ushort port) {

            if (registeredPorts.Contains(port))
                return;

            // We add the internal handlers for stateful access to json-objects
            // for each new port that is used.
            registeredPorts.Add(port);

            Handle.PATCH(port, ScSessionClass.DataLocationUriPrefix + Handle.UriParameterIndicator, (Session session, Request request) => {
                Json root = null;

                // Incrementing the initial call level for handles.
                Handle.CallLevel++;

                try {
                    if (session == null)
                        return CreateErrorResponse(404, "No session found for the specified uri.");
                    //                    root = session.Data;
                    root = session.PublicViewModel;
                    if (root == null)
                        return CreateErrorResponse(404, "Session does not contain any state (session.Data).");

                    IntPtr bodyPtr;
                    uint bodySize;
                    request.GetBodyRaw(out bodyPtr, out bodySize);
                    int patchCount = jsonPatch.Apply(root, bodyPtr, (int)bodySize, session.CheckOption(SessionOptions.StrictPatchRejection));

                    if (patchCount == -1) { // -1 means that the patch was queued due to clientversion mismatch.
                        return new Response() {
                            StatusCode = 202,
                            StatusDescription = "Patch enqueued until earlier versions have arrived"
                        };
                    }

                    return root;
                } catch (JsonPatchException nex) {
                    return CreateErrorResponse(400, nex.Message + " Patch: " + nex.Patch);
                }
            }, new HandlerOptions() { ProxyDelegateTrigger = true });

            Handle.GET(port, ScSessionClass.DataLocationUriPrefix + Handle.UriParameterIndicator, (Request request, Session session) => {
                if (session == null)
                    return CreateErrorResponse(404, "No resource found for the specified uri.");

                if (request.WebSocketUpgrade) {
                    // Sending an upgrade (note that we attach the existing session).
                    request.SendUpgrade(JsonPatchWebSocketGroupName, null, null, session);
                    session.CalculatePatchAndPushOnWebSocket();
                    return HandlerStatus.Handled;
                } else if (request.PreferredMimeType == MimeType.Application_Json) {
                    Json root = session.PublicViewModel;
                    if (root == null)
                        return CreateErrorResponse(404, "Session does not contain any state (session.Data).");

                    byte[] body = null;
                    session.enableNamespaces = true;
                    session.enableCachedReads = true;
                    try {
                        body = root.ToJsonUtf8();

                        if (root.ChangeLog != null)
                            root.ChangeLog.Checkpoint();
                    } finally {
                        session.enableNamespaces = false;
                        session.enableCachedReads = false;
                    }
                    
                    return new Response() {
                        BodyBytes = body,
                        ContentType = MimeTypeHelper.MimeTypeAsString(MimeType.Application_Json)
                    };
                } else {
                    return CreateErrorResponse(513, String.Format("Unsupported mime type {0}.", request.PreferredMimeTypeString));
                }
            });

            // Handling WebSocket JsonPatch string message.
            Handle.WebSocket(port, JsonPatchWebSocketGroupName, (String s, WebSocket ws) => {
                
                Byte[] dataBytes = Encoding.UTF8.GetBytes(s);

                // Calling bytes data handler.
                HandleWebSocketJson(dataBytes, ws);
            });

            // Handling WebSocket JsonPatch byte array.
            Handle.WebSocket(port, JsonPatchWebSocketGroupName, HandleWebSocketJson);

            // Handling JsonPatch WebSocket disconnect here.
            Handle.WebSocketDisconnect(port, JsonPatchWebSocketGroupName, (WebSocket ws) => {

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
