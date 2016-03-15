namespace Starcounter {

    /// <summary>
    /// Built-in MIME provider that react to conversions of Json resources into
    /// HTML by investigating the JSON (view model) for a property referencing a
    /// static file, and provide the content of that file via internal request.
    /// </summary>
    public class HtmlFromJsonProvider : IMiddleware {

        // TODO:
        // Decide how to handle if HTML is not defined, and incorporate the
        // Page/Partial error handling when it is found, but reference a file
        // that is not found.

        // TODO:
        // public bool WrapPartialHtml { get; set; }

        //public HtmlFromJsonProvider() {
        //    WrapPartialHtml = true;
        //}

        void IMiddleware.Register(Application application) {
            application.Use(MimeProvider.Html(HtmlFromJsonProvider.Invoke));
        }

        static byte[] Invoke(IResource resource) {
            var json = resource as Json;
            byte[] result = null;

            if (json != null) {
                var foo = json["Html"] as string;
                if (foo != null) {
                    result = Self.GET<byte[]>(foo);
                }
            }

            return result;
        } 
    }
}
