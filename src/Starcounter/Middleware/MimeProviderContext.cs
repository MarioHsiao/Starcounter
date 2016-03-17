namespace Starcounter {

    /// <summary>
    /// Context passed to installed MIME providers.
    /// </summary>
    public class MimeProviderContext {
        /// <summary>
        /// Gets the originating request.
        /// </summary>
        public readonly Request Request;

        /// <summary>
        /// Gets the resource returned from the handler.
        /// </summary>
        public readonly IResource Resource;

        /// <summary>
        /// Gets or sets the result of this or any previous provider.
        /// </summary>
        public byte[] Result { get; set; }

        internal MimeProviderContext(Request request, IResource resource) {
            Request = request;
            Resource = resource;
        }
    }
}
