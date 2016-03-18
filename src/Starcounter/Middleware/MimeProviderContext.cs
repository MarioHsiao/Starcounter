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

        /// <summary>
        /// Gets the number of providers invoked in this chain.
        /// </summary>
        /// <remarks>Providers consulting this value from within the handler
        /// should be aware it illustrate how many providers BEFORE the current
        /// one has been invoked. Hence, the count for the entire request chain
        /// is not established until AFTER the last provider has been invoked.
        /// </remarks>
        public int ProvidersInvoked { get; internal set;  }

        internal MimeProviderContext(Request request, IResource resource) {
            Request = request;
            Resource = resource;
            Result = null;
            ProvidersInvoked = 0;
        }
    }
}
