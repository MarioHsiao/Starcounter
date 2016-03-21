using System;

namespace Starcounter {

    /// <summary>
    /// Defines the signature of a MIME providers provision method.
    /// </summary>
    /// <param name="context">The context passed to the method.</param>
    /// <param name="next">The next provider in the chain.</param>
    public delegate void MimeProvisionDelegate(MimeProviderContext context, Action next);

    /// <summary>
    /// Support custom mime providers to be installed as middleware, affecting the
    /// request pipeline for an application by governing in the convertion from a
    /// resource into a certain MIME representation.
    /// </summary>
    /// <remarks>Currently, installed MIME providers are activated and invoked only
    /// when the cargo is based on Json. This is an implementation detail though, and
    /// we recommend all custom providers not to assume this as it will likely change
    /// eventually.
    /// </remarks>
    public class MimeProvider : IMiddleware {
        readonly MimeType mimeType;
        readonly MimeProvisionDelegate provisioner;

        private MimeProvider(MimeType type, MimeProvisionDelegate provisioner) {
            this.mimeType = type;
            this.provisioner = provisioner;
        }
        
        /// <summary>
        /// Gets the MIME type of this provider.
        /// </summary>
        internal MimeType MimeType {
            get { return mimeType; }
        }

        /// <summary>
        /// Creates an <see cref="IMiddleware"/> abstraction representing a mime provider
        /// backed by the given <paramref name="provisioner"/> delegate.
        /// </summary>
        /// <param name="mimeType">The MIME type this provider shall handle.</param>
        /// <param name="provisioner">The provisioner method.</param>
        /// <returns>An instance of the provider that can be installed as middleware.
        /// </returns>
        public static IMiddleware For(string mimeType, MimeProvisionDelegate provisioner) {
            if (provisioner == null) {
                throw new ArgumentNullException("provisioner");
            }
            var type = MimeTypeHelper.StringToMimeType(mimeType);
            if (type == MimeType.Unspecified || type == MimeType.Other) {
                throw ErrorCode.ToException(Error.SCERRBADARGUMENTS, string.Format("Mime providers for type {0} not supported", mimeType));
            }

            return new MimeProvider(type, provisioner);
        }

        /// <summary>
        /// Provide a simple way to create a MIME type provider for "text/html".
        /// </summary>
        /// <param name="provisioner">The provisioner method.</param>
        /// <returns>An instance of the provider that can be installed as middleware.
        /// </returns>
        public static IMiddleware Html(MimeProvisionDelegate provisioner) {
            if (provisioner == null) {
                throw new ArgumentNullException("provisioner");
            }
            return new MimeProvider(MimeType.Text_Html, provisioner);
        }

        void IMiddleware.Register(Application application) {
            application.MimeProviders.Install(this.mimeType, this.provisioner);
        }

        internal static void Terminator(MimeProviderContext context, Action next) {
            // Lets keep a terminator, and see if we can use that at
            // some point in the future.
            // Make sure never to invoke next() here.
        }
    }
}
