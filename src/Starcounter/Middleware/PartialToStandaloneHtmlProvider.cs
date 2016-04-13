
using System;
using System.Text;

namespace Starcounter {
    /// <summary>
    /// Provide "standalone HTML" when a JSON is returned from a handler, and
    /// that JSON provide a partial HTML (normally, using HtmlFromJsonProvider
    /// middleware).
    /// </summary>
    public class PartialToStandaloneHtmlProvider : IMiddleware {
        static Encoding defaultEncoding = Encoding.UTF8;

        #region Implicit standalone page template
        const string ImplicitStandaloneTemplate = @"<!DOCTYPE html>

<html>
<head>
    <meta charset=""utf-8"">
    <title>{0}</title>

    <script src=""/sys/webcomponentsjs/webcomponents.js""></script>
    <script>
        window.Polymer = window.Polymer || {{}};
        window.Polymer.dom = ""shadow"";
    </script>
    <link rel=""import"" href=""/sys/polymer/polymer.html"">

    <!-- Import Starcounter specific components -->
    <link rel=""import"" href=""/sys/starcounter.html"">
    <link rel=""import"" href=""/sys/starcounter-include/starcounter-include.html"">
    <link rel=""import"" href=""/sys/starcounter-debug-aid/src/starcounter-debug-aid.html"">
    <link rel=""import"" href=""/sys/dom-bind-notifier/dom-bind-notifier.html"">
    <link rel=""import"" href=""/sys/bootstrap.html"">
    <style>
    body {{
      padding: 20px;
      font-size: 14px;
    }}
  </style>
</head>
<body>
    <template is=""dom-bind"" id=""puppet-root""><template is=""imported-template"" content$=""{{{{model.Html}}}}"" model=""{{{{model}}}}""></template>
<dom-bind-notifier path=""model"" observed-object=""{{{{model}}}}"" deep></dom-bind-notifier></template>
    <puppet-client ref=""puppet-root""></puppet-client>
    <starcounter-debug-aid></starcounter-debug-aid>
</body>
</html>";
        #endregion

        void IMiddleware.Register(Application application) {
            application.Use(MimeProvider.Html(this.Invoke));
        }

        void Invoke(MimeProviderContext context, Action next) {
            var content = context.Result;

            if (content != null) {
                var json = context.Resource as Json;
                if (json != null) {
                    if (!IsFullPageHtml(content)) {
                        content = ProvideImplicitStandalonePage(content, context.Request.HandlerAppName);
                        context.Result = content;
                    }
                }
            }

            next();
        }

        internal static byte[] ProvideImplicitStandalonePage(byte[] content, string appName) {
            var html = String.Format(ImplicitStandaloneTemplate, appName);
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
