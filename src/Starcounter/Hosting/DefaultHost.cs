using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter.Ioc;

namespace Starcounter.Hosting {
    /// <summary>
    /// The default code host.
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
    }
}
