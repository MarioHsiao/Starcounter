
using Starcounter.Legacy;
using System;
using System.Diagnostics;

namespace Starcounter.Hosting {
    /// <summary>
    /// Expose some hosting-relating functionality to <see cref="IApplicationHost"/>
    /// implementations.
    /// </summary>
    public sealed class CodeHost : IDisposable {
        // The current implementation creates a new instance for every application
        // being launched, so we can keep a reference to it here. This is an
        // implementation detail though. Any public API should act as it operates
        // on a process-wide, host level, see GetLegacyContext(Application) for
        // example.
        Application application;

        internal CodeHost(Application app) {
            application = app;
        }

        public LegacyContext GetLegacyContext(Application application) {
            Trace.Assert(application.Equals(application));
            return LegacyContext.GetContext(application);
        }

        void IDisposable.Dispose() {
            if (application != null) {
                LegacyContext.Exit(application);
                application = null;
            }
        }
    }
}
