using System;
using System.Text;
using Starcounter.Internal;

namespace Starcounter {
    /// <summary>
    /// For any HTTP request with `Accept: text/html` without a Starcounter session
    /// returns an implicit HTML template that contains code to bootstrap a PuppetJs 
    /// connection instead of the HTTP handler.
    /// 
    /// A custom HTML template can be provided as a string parameter to the constructor.
    /// 
    /// For any HTTP request with `Accept: application/json` that has a response of type Json
    /// and a Session attached, adds the session URL as a `X-Location` response header. 
    /// The header is used by PuppetJs to upgrade the connection to WebSocket.
    /// 
    /// Middleware only wraps requests that have a HTTP handler. However, Be cautious about the 
    /// response status, because this middleware always returns 200 OK status for any request 
    /// that matches the handler.
    /// </summary>
    public class PuppetJsProvider : IMiddleware {
        static Encoding defaultEncoding = Encoding.UTF8;
        readonly string template = ImplicitStandaloneTemplate;

        #region Implicit standalone page template
        const string ImplicitStandaloneTemplate = @"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <title>{0}</title>

    <script src=""/sys/webcomponentsjs/webcomponents.min.js""></script>
    <link rel=""import"" href=""/sys/polymer/polymer.html"">
    <link rel=""import"" href=""/sys/starcounter.html"">
    <link rel=""import"" href=""/sys/starcounter-include/starcounter-include.html"">
    <link rel=""import"" href=""/sys/starcounter-debug-aid/src/starcounter-debug-aid.html"">

    <link rel=""import"" href=""/sys/bootstrap.html"">
    <style>
        body {{
            margin: 20px;
        }}
    </style>
</head>
<body>
    <template is=""dom-bind"" id=""puppet-root"">
        <template is=""imported-template"" content$=""{{{{model.Html}}}}"" model=""{{{{model}}}}""></template>
    </template>

    <puppet-client ref=""puppet-root""></puppet-client>

    <starcounter-debug-aid></starcounter-debug-aid>
</body>
</html>";
        #endregion

        public PuppetJsProvider() : this(ImplicitStandaloneTemplate) {
        }

        public PuppetJsProvider(string standaloneTemplate) {
            if (string.IsNullOrEmpty(standaloneTemplate)) throw new ArgumentNullException("standaloneTemplate");
            template = standaloneTemplate;
        }

        void IMiddleware.Register(Application application) {
            application.Use((Request req) => {
                if (req.PreferredMimeType == MimeType.Text_Html && req.Session == null) {
                    var content = new byte[0];
                    return new Response() {
                        StatusCode = 200,
                        StatusDescription = "OK",
                        BodyBytes = ProvideImplicitStandalonePage(content, req.HandlerAppName, template)
                    };
                }
                return null;
            });

            application.Use((Request req, Response res) => {
                if (req.PreferredMimeType == MimeType.Application_Json) {
                    var json = res.Resource as Json;
                    if (json != null && json.Session != null) {
                        res.Headers["X-Location"] = ScSessionClass.DataLocationUriPrefix + json.Session.SessionId;
                        res.Headers["Set-Cookie"] = ""; //prevent Starcounter from setting Location cookie; remove this line when Starcounter does not set the Location cookie anymore
                    }
                }
                return null;
            });
        }

        internal static byte[] ProvideImplicitStandalonePage(byte[] content, string appName, string template = ImplicitStandaloneTemplate) {
            var html = String.Format(template, appName);
            return defaultEncoding.GetBytes(html);
        }
    }
}