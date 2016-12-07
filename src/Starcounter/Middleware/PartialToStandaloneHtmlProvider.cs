
using System;
using System.Text;

namespace Starcounter {
    /// <summary>
    /// For any HTTP request with `Accept: text/html` with a response that is HTML,
    /// checks if the HTML document is a partial (does not start with a doctype).
    /// If yes, returns an implicit HTML template that contains code to bootstrap 
    /// a PuppetJs connection instead of that HTML. 
    /// 
    /// The HTML template cointans the session URL that was returned from the handler, 
    /// so that PuppetJs can request the relevant Json in a following request.
    /// 
    /// Must be used after `HtmlFromJsonProvider` middleware.
    /// 
    /// A custom HTML template can be provided as a string parameter to the constructor.
    /// 
    /// Middleware only wraps requests that have a HTTP handler. Since this middleware
    /// wraps the actual response, it will also have the HTTP status code
    /// that was returned from the handler.
    /// </summary>
    public class PartialToStandaloneHtmlProvider : IMiddleware {
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

    <puppet-client ref=""puppet-root"" remote-url=""{1}""></puppet-client>

    <starcounter-debug-aid></starcounter-debug-aid>
</body>
</html>";
        #endregion

        /// <summary>
        /// Creates a new instance of <see cref="PartialToStandaloneHtmlProvider"/>
        /// using the predefined default template.
        /// </summary>
        public PartialToStandaloneHtmlProvider() : this(ImplicitStandaloneTemplate) {
        }

        /// <summary>
        /// Creates a new instance of <see cref="PartialToStandaloneHtmlProvider"/>
        /// using the given standalone page template.
        /// </summary>
        public PartialToStandaloneHtmlProvider(string standaloneTemplate) {
            if (string.IsNullOrEmpty(standaloneTemplate)) throw new ArgumentNullException("standaloneTemplate");
            template = standaloneTemplate;
        }

        void IMiddleware.Register(Application application) {
            application.Use(MimeProvider.Html(this.Invoke));
        }

        void Invoke(MimeProviderContext context, Action next) {
            var content = context.Result;

            if (content != null) {
                var json = context.Resource as Json;
                if (json != null) {
                    if (!IsFullPageHtml(content) && json.Session != null) {
                        content = ProvideImplicitStandalonePage(content, context.Request.HandlerAppName, json.Session.SessionUri, template);
                        context.Result = content;
                    }
                }
            }

            next();
        }

        internal static byte[] ProvideImplicitStandalonePage(byte[] content, string appName, string sessionUri, string template = ImplicitStandaloneTemplate) {
            var html = String.Format(template, appName, sessionUri);
            return defaultEncoding.GetBytes(html);
        }

        internal static bool IsFullPageHtml(Byte[] html) {
            // This method is just copied here from obsolete Partial class, as is. It should
            // be reviewed and probably improved, or alternatively redesigned.

            //TODO test for UTF-8 BOM
            byte[] fullPageTest = defaultEncoding.GetBytes("<!"); //full page starts with <!doctype or <!DOCTYPE;
            var indicatorLength = fullPageTest.Length;

            if (html.Length < indicatorLength) {
                return false; // this is too short for a full html
            }

            for (var i = 0; i < indicatorLength; i++) {
                if (html[i] == fullPageTest[i]) {
                    continue;
                }
                return false; //it's a partial
            }

            return true; //it's a full html
        }
    }
}