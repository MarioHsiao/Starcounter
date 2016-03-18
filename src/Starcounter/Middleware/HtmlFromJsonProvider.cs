using System;
using System.Text;

namespace Starcounter {
    
    /// <summary>
    /// Built-in MIME provider that react to conversions of Json resources into
    /// HTML by investigating the JSON (view model) for a property referencing a
    /// static file, and provide the content of that file via internal request.
    /// </summary>
    public class HtmlFromJsonProvider : IMiddleware {
        static Encoding defaultEncoding = Encoding.UTF8;

        #region Implicit standalone page template
        const string ImplicitStandaloneTemplate = @"<!DOCTYPE html>

<html>
<head>
    <meta charset=""utf-8"">
    <title>{0}</title>

    <script src=""/sys/object.observe/dist/object-observe.min.js""></script>
    <script src=""/sys/array.observe/array-observe.min.js""></script>

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

        /// <summary>
        /// Gets or sets a value if the current provider should provision "partial
        /// HTML" wrapped up in default standalone pages. Defaults to <c>false</c>.
        /// </summary>
        public bool ProvisionImplicitStandalonePages { get; set; }

        /// <summary>
        /// Gets or sets a value relaxing the provider to ignore any resource that
        /// does not expose a property referencing HTML. The default is <c>false</c>,
        /// and in such case, the provider will raise an error on any resource that
        /// miss a property referencing a HTML view path.
        /// </summary>
        public bool IgnoreJsonWithoutHtml { get; set; }

        /// <summary>
        /// Initialize a new <see cref="HtmlFromJsonProvider"/> instance.
        /// </summary>
        public HtmlFromJsonProvider() {
            ProvisionImplicitStandalonePages = false;
            IgnoreJsonWithoutHtml = false;
        }

        void IMiddleware.Register(Application application) {
            application.Use(MimeProvider.Html(this.Invoke));
        }

        void Invoke(MimeProviderContext context, Action next) {
            var json = context.Resource as Json;
            byte[] result = null;

            if (json != null) {
                var filePath = json["Html"] as string;
                if (filePath == null) {
                    if (!this.IgnoreJsonWithoutHtml) {
                        throw ErrorCode.ToException(Error.SCERRINVALIDOPERATION,
                            string.Format("Json instance {0} missing 'Html' property.", json));
                    }
                }
                else {
                    result = ProvideFromFilePath<byte[]>(filePath);

                    // Move this to separate provider.
                    // TODO:
                    if (ProvisionImplicitStandalonePages) {
                        if (!IsFullPageHtml(result)) {
                            result = ProvideImplicitStandalonePage(result, json._appName);
                        }
                    }
                }
            }

            if (result != null) {
                context.Result = result;
            }

            next();
        }

        internal static T ProvideFromFilePath<T>(string filePath) {
            var result = Self.GET<T>(filePath);
            if (result == null) {
                throw new ArgumentOutOfRangeException("Can not find referenced Html file: \"" + filePath + "\"");
            }

            return result;
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
