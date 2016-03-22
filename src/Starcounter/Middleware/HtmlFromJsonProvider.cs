using System;
using System.Text;

namespace Starcounter {
    
    /// <summary>
    /// Built-in MIME provider that react to conversions of Json resources into
    /// HTML by investigating the JSON (view model) for a property referencing a
    /// static file, and provide the content of that file via internal request.
    /// </summary>
    public class HtmlFromJsonProvider : IMiddleware {
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
    }
}
