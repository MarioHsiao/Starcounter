using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter.Configuration;

namespace StarcounterServer {

    /// <summary>
    /// Representing the running server, hosted in a server program.
    /// </summary>
    internal class ServerNode {

        public readonly ServerConfiguration Configuration;
        
            public ServerNode(ServerConfiguration configuration) {
            this.Configuration = configuration;
        }
    }
}
