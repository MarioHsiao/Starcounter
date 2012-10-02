using System;
using HttpStructs;
using Starcounter.Internal.Application;
using Starcounter.Internal.Web;

namespace Starcounter.Internal.JsonPatch
{
    public class InternalHandlers : App
    {
        public static void Register()
        {
            Console.WriteLine("Registering internal handlers for patch and getting root apps");

            GET("/__vm/@s", (string sessionId) =>
            {
                Session s = HardcodedStuff.Here.Sessions.GetSession(sessionId);
                return s.RootApp;
            });

            PATCH("/__vm/@s", (string sessionId) =>
            {
                HttpResponse response = null;
                Session s = HardcodedStuff.Here.Sessions.GetSession(sessionId);

                s.Execute(HardcodedStuff.Here.HttpRequest, () =>
                {
                    App rootApp = Session.Current.RootApp;
                    HttpRequest request = Session.Current.HttpRequest;

                    JsonPatch.EvaluatePatches(request.GetBody());

                    response = new HttpResponse();
                    response.Uncompressed = HttpPatchBuilder.CreateHttpPatchResponse(Session.Current._changeLog);
                });
                return response;
            });
        }
    }
}
