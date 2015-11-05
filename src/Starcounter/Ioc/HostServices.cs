using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Ioc {

    /// <summary>
    /// Expose services to components inside a code host.
    /// </summary>
    public sealed class HostServices :IServices {
        readonly ServiceContainer container;

        /// <summary>
        /// Gets the host-level instance.
        /// </summary>
        public readonly HostServices Instance;
        
        /// <summary>
        /// Gets the service container.
        /// </summary>
        public ServiceContainer Container {
            get { return container; }
        }        

        internal HostServices() {
            container = new ServiceContainer();
            Instance = this;
        }

        /// <inheritdoc/>
        public T Get<T>() {
            // Just forward to the container
            var s = (IServices)container;
            return s.Get<T>();
        }
    }
}