using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter.ABCIPC;
using Starcounter.ABCIPC.Internal;

namespace Starcounter.Server {
    /// <summary>
    /// Implements the functionality used by the server to interact with
    /// the the database host.
    /// </summary>
    internal sealed class DatabaseHostingService {
        /// <summary>
        /// Gets the server that has instantiated this service.
        /// </summary>
        readonly ServerEngine engine;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseHostingService"/>
        /// class.
        /// </summary>
        /// <param name="engine">The <see cref="ServerEngine"/> in which the
        /// service will live.</param>
        internal DatabaseHostingService(ServerEngine engine) {
            this.engine = engine;
        }

        /// <summary>
        /// Executes setup of the current <see cref="DatabaseHostingService"/>.
        /// </summary>
        internal void Setup() {
        }

        /// <summary>
        /// Gets the <see cref="Client"/> representing the local hosting
        /// interface of the given <see cref="Database"/>.
        /// </summary>
        /// <param name="database">The <see cref="Database"/> whose hosting
        /// interface is to be retreived.</param>
        /// <returns>A <see cref="Client"/> that can be used to send
        /// management commands to the host.</returns>
        internal Client GetHostingInterface(Database database) {
            var pipeName = ScUriExtensions.MakeLocalDatabasePipeString(engine.Name, database.Name);
            return ClientServerFactory.CreateClientUsingNamedPipes(pipeName);
        }
    }
}
