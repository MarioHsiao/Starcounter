
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Starcounter.Rest;
using jp = Starcounter.Internal.JsonPatch;

namespace Starcounter.Internal {

    /// <summary>
    /// Registers the built in REST handler allowing clients to communicate with
    /// the public Session data of a Starcounter application.
    /// </summary>
    internal static class PuppetRestHandler {
        private static List<UInt16> registeredPorts = new List<UInt16>();

        internal static void Register(UInt16 defaultUserHttpPort) {
            HandlersManagement.SetHandlerRegisteredCallback(HandlerRegistered);
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
                    root = session.Data;
                    if (root == null)
                        return CreateErrorResponse(404, "Session does not contain any state (session.Data).");

                    jp::JsonPatch.EvaluatePatches(root, request.BodyBytes);

                    return root;
                } catch (NotSupportedException nex) {
                    return CreateErrorResponse(415, nex.Message);
                }
            });

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
        }

        private static Response CreateErrorResponse(int statusCode, string message) {
            var response = Response.FromStatusCode(statusCode);
            response.Body = message;
            return response;
        }
    }
}
