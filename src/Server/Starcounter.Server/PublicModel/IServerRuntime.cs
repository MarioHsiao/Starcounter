// ***********************************************************************
// <copyright file="IServerRuntime.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter.Server.PublicModel.Commands;

namespace Starcounter.Server.PublicModel {

    /// <summary>
    /// Defines the core runtime API to be used by hosts when
    /// interacting with a running <see cref="ServerEngine"/>.
    /// </summary>
    public interface IServerRuntime {

        /// <summary>
        /// Returns the functionality supported by the running
        /// server, in the form of a set of <see cref="CommandDescriptor"/>
        /// instances.
        /// </summary>
        CommandDescriptor[] Functionality { get; }

        /// <summary>
        /// Executes the given <see cref="ServerCommand"/>.
        /// </summary>
        /// <remarks>
        /// Execution of server commands are always asynchronous, meaning
        /// this call will return when some basic validation has occured
        /// and the returned value will represent the state of the command
        /// just after it has been enqueued. To find it the result of the
        /// completed command, use <see cref="Wait(CommandId)"/>, <see cref="Wait(CommandInfo)"/>
        /// or use <see cref="GetCommand"/> to get the latest snapshot of
        /// the command state.
        /// </remarks>
        /// <param name="command">The <see cref="ServerCommand"/>
        /// to execute.</param>
        /// <returns>A <see cref="CommandInfo"/> representing the state of
        /// the command.</returns>
        CommandInfo Execute(ServerCommand command);

        /// <summary>
        /// Waits for the server command represented by the given
        /// <see cref="CommandInfo"/> to complete.
        /// </summary>
        /// <param name="info">The <see cref="CommandInfo"/> representing
        /// the command to wait for.</param>
        /// <returns>A <see cref="CommandInfo"/> representing the state
        /// of the command after it has finished.</returns>
        CommandInfo Wait(CommandInfo info);

        /// <summary>
        /// Waits for the server command represented by the given
        /// <see cref="CommandId"/> to complete.
        /// </summary>
        /// <param name="id">The <see cref="CommandId"/> of the command
        /// to wait for.</param>
        /// <returns>A <see cref="CommandInfo"/> representing the state
        /// of the command after it has finished.</returns>
        CommandInfo Wait(CommandId id);

        /// <summary>
        /// Gets a snapshot of the latest state of the command
        /// represented by the given <see cref="CommandId"/>.
        /// </summary>
        /// <remarks>
        /// Command information stays available only for a certain
        /// time, after which information is abandoned by the server.
        /// </remarks>
        /// <param name="id">The <see cref="CommandId"/> holding the
        /// identity of the command whose state we want to retrieve.
        /// </param>
        /// <returns>A <see cref="CommandInfo"/> representing the state of
        /// the given command.</returns>
        CommandInfo GetCommand(CommandId id);

        /// <summary>
        /// Gets a snapshot of the latest state of all commands
        /// currently being references by the server. This list
        /// can include commands pending, commands currently being
        /// executed and commands that has finished executing.
        /// </summary>
        /// <returns>An array of <see cref="CommandInfo"/> representing
        /// the state of all commands.</returns>
        CommandInfo[] GetCommands();

        /// <summary>
        /// Gets the state of the principal server.
        /// </summary>
        /// <returns>A <see cref="ServerInfo"/> representing a snapshot
        /// of the servers current state.</returns>
        ServerInfo GetServerInfo();

        /// <summary>
        /// Gets a database, represented by a <see cref="DatabaseInfo"/>,
        /// by it's URI.
        /// </summary>
        /// <param name="uri">The URI of the database to retreive.</param>
        /// <returns>A <see cref="DatabaseInfo"/> representing a snapshot
        /// of the databases current state.</returns>
        DatabaseInfo GetDatabase(string uri);

        /// <summary>
        /// Gets all databases, represented by their <see cref="DatabaseInfo"/>,
        /// from the executing server.
        /// </summary>
        /// <returns>A <see cref="DatabaseInfo"/> representing a snapshot
        /// of the current state for each database maintained by the server
        /// being queried.</returns>
        DatabaseInfo[] GetDatabases();
    }
}