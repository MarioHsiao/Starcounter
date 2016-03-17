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
        readonly Func<IResource, byte[]> provider;

        private MimeProvider(MimeType type, Func<IResource, byte[]> provider) {
            this.mimeType = type;
            this.provider = provider;
        }

        /// <summary>
        /// Gets the MIME type of this provider.
        /// </summary>
        internal MimeType MimeType {
            get { return mimeType; }
        }

        /// <summary>
        /// Creates an <see cref="IMiddleware"/> abstraction representing a mime provider
        /// backed by the given <paramref name="provider"/> delegate.
        /// </summary>
        /// <param name="mimeType">The MIME type this provider shall handle.</param>
        /// <param name="provider">The providing method.</param>
        /// <returns>An instance of the provider that can be installed as middleware.
        /// </returns>
        public static IMiddleware For(string mimeType, Func<IResource, byte[]> provider) {
            if (provider == null) {
                throw new ArgumentNullException("provider");
            }
            var type = MimeTypeHelper.StringToMimeType(mimeType);
            if (type == MimeType.Unspecified || type == MimeType.Other) {
                throw ErrorCode.ToException(Error.SCERRBADARGUMENTS, string.Format("Mime providers for type {0} not supported", mimeType));
            }

            return new MimeProvider(type, provider);
        }

        /// <summary>
        /// Provide a simple way to create a MIME type provider for "text/html".
        /// </summary>
        /// <param name="provider">The providing method.</param>
        /// <returns>An instance of the provider that can be installed as middleware.
        /// </returns>
        public static IMiddleware Html(Func<IResource, byte[]> provider) {
            if (provider == null) {
                throw new ArgumentNullException("provider");
            }
            return new MimeProvider(MimeType.Text_Html, provider);
        }

        void IMiddleware.Register(Application application) {
            application.RegisterMimeProvider(this);
        }

        internal static byte[] InvokeInstalledProviders(string application, MimeType type, Request request, IResource resource) {
            byte[] result = null;

            if (!string.IsNullOrEmpty(application)) {
                var app = Application.GetFastNamedApplication(application);
                MimeProvider provider;
                var found = app.MimeProviders.TryGetValue(type, out provider);
                if (found) {
                    result = provider.InvokeProvider(resource);
                }
            }

            return result;
        }

        internal byte[] InvokeProvider(IResource resource) {
            return provider.Invoke(resource);
        }
    }
}
