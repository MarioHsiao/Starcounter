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
                CommandToken = ProcessorToken,
                CommandDescription = "Executes an executable inside a Starcounter host",
                Tasks = new TaskInfo[] { 
                    Task.CheckExeOutOfDate.ToPublicModel(), 
                    Task.CreateDatabase.ToPublicModel() 
                }
            };
        }

        internal static class Task {

            internal static readonly CommandTask CheckExeOutOfDate = new CommandTask(
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
        }
    }
}