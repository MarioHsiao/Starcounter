// ***********************************************************************
// <copyright file="InvokableCommandProcessor.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter.Server.PublicModel.Commands;

namespace Starcounter.Server.Commands {

    [CommandProcessor(typeof(InvokableCommand))]
    internal sealed class InvokableCommandProcessor : CommandProcessor {
        /// <summary>
        /// Initializes a new <see cref="InvokableCommandProcessor"/>.
        /// </summary>
        /// <param name="server">The server in which the processor executes.</param>
        /// <param name="command">The <see cref="InvokableCommand"/> the
        /// processor should exeucte.</param>
        public InvokableCommandProcessor(ServerEngine server, ServerCommand command)
            : base(server, command) {
        }

        /// <inheritdoc />
        protected override void Execute() {
            ((InvokableCommand)this.Command).Invoke(this);
        }
    }
}
