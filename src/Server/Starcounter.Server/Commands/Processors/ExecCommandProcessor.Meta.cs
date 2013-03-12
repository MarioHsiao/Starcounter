// ***********************************************************************
// <copyright file="ExecAppCommandProcessor.Meta.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.ABCIPC;
using Starcounter.Internal;
using Starcounter.Server.PublicModel;
using Starcounter.Server.PublicModel.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Starcounter.Server.Commands {

    internal sealed partial class ExecCommandProcessor : CommandProcessor {

        public static int ProcessorToken = CreateToken(typeof(ExecCommandProcessor));
        
        public static CommandDescriptor MakeDescriptor() {
            return new CommandDescriptor() {
                ProcessorToken = ProcessorToken,
                CommandDescription = "Executes an executable inside a Starcounter host",
                Tasks = new TaskInfo[] { 
                    Task.CheckRunningExeUpToDate.ToPublicModel(), 
                    Task.CreateDatabase.ToPublicModel() 
                }
            };
        }

        internal static class Task {

            internal static readonly CommandTask CheckRunningExeUpToDate = new CommandTask(
                ExecCommand.DefaultProcessor.Tasks.CheckRunningExeUpToDate,
                "Checking executable",
                TaskDuration.ShortIndeterminate,
                "Checks if the executable is already running and can be considered up to date."
                );

            internal static readonly CommandTask CreateDatabase = new CommandTask(
                ExecCommand.DefaultProcessor.Tasks.CreateDatabase,
                "Creating database",
                TaskDuration.NormalIndeterminate,
                "Creates a database if one with the given name is not found and automatic creation is not disabled."
                );

            internal static readonly CommandTask StartDataAndHostProcesses = new CommandTask(
                ExecCommand.DefaultProcessor.Tasks.StartDataAndHostProcesses,
                "Preparing database engine",
                TaskDuration.NormalIndeterminate,
                "Assures neccessary processes such as scdata and sccode is up and ready."
                );

            internal static readonly CommandTask WeaveOrPrepareForNoDb = new CommandTask(
                ExecCommand.DefaultProcessor.Tasks.WeaveOrPrepareForNoDb,
                "Preparing user executables and files",
                TaskDuration.NormalIndeterminate,
                "Prepares the user code to be hosted in the code host, weaving and/or copying it."
                );

            internal static readonly CommandTask PingOrLoad = new CommandTask(
                ExecCommand.DefaultProcessor.Tasks.PingOrLoad,
                "Setting up code host process",
                TaskDuration.NormalIndeterminate,
                "Communications with the code host, by pinging it (in case of preparation) or loading user code."
                );
        }
    }
}