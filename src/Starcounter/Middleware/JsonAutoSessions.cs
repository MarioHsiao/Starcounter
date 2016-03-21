using System;

namespace Starcounter {
    /// <summary>
    /// Middleware that, when installed, make sure that when HTML is
    /// requested and the handler respond with JSON, the JSON instance
    /// returned is attached to a (patching) session if it's not
    /// already.
    /// </summary>
    public class JsonAutoSessions : IMiddleware {
        void IMiddleware.Register(Application application) {
            application.Use(MimeProvider.Html(this.Invoke));
        }

        void Invoke(MimeProviderContext context, Action next) {
            var json = context.Resource as Json;
            if (json != null) {
                if (Session.Current == null) {
                    json.Session = new Session(SessionOptions.PatchVersioning);
                }
            }

            next();
        }
    }
}
