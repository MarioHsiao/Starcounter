using Starcounter.Ioc;
using System;

namespace Starcounter.Hosting
{
    /// <summary>
    /// The default code host (i.e the shared app code host).
    /// </summary>
    internal sealed class DefaultHost : ICodeHost {
        public static DefaultHost Current { get; private set; }

        internal readonly ServiceContainer ServiceContainer;

        /// <summary>
        /// Create the singleton default host and assigns it.
        /// </summary>
        public static void InstallCurrent() {
            Current = new DefaultHost();
        }

        private DefaultHost() {
            ServiceContainer = new ServiceContainer();
        }

        IServices ICodeHost.Services {
            get {
                return ServiceContainer;
            }
        }

        void ICodeHost.Run(Action applicationMainLoop)
        {
            throw ErrorCode.ToException(Error.SCERRCANTRUNSHAREDAPPHOST);
        }
    }
}
