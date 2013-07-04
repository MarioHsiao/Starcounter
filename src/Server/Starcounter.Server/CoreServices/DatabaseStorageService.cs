// ***********************************************************************
// <copyright file="DatabaseStorageService.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter.Configuration;
using System.IO;
using System.Diagnostics;

namespace Starcounter.Server {
    
    /// <summary>
    /// Exposes a set of methods that can be used to easily work with
    /// database storages, i.e. sets of image- and transaction log files.
    /// </summary>
    internal sealed class DatabaseStorageService {
        readonly ServerEngine engine;
        string creationToolPath;

        /// <summary>
        /// Intializes a new <see cref="DatabaseStorageService"/>.
        /// </summary>
        /// <param name="engine">The engine in which the current service
        /// runs.</param>
        internal DatabaseStorageService(ServerEngine engine) {
            this.engine = engine;
        }

        /// <summary>
        /// Executes setup of the <see cref="DatabaseStorageService"/>.
        /// </summary>
        internal void Setup() {
            creationToolPath = Path.Combine(engine.InstallationDirectory, "sccreatedb.exe");
            if (!File.Exists(creationToolPath)) {
                throw ErrorCode.ToException(Error.SCERRUNSPECIFIED, string.Format("Couldn't find creation tool: {0}", creationToolPath));
            }
        }

        /// <summary>
        /// Creates a key that the server can use when creating and deleting
        /// database files and folders to assure they are unique.
        /// </summary>
        /// <returns>An opaque key to use in the form of a string.</returns>
        internal string NewKey() {
            return DateTime.Now.ToString("yyyyMMddTHHmmssfff");
        }

        /// <summary>
        /// Creates a new storage for with the given values.
        /// </summary>
        /// <param name="name">The name of database to create a new storage for.</param>
        /// <param name="imagePath">The path to where image files should be created.</param>
        /// <param name="logPath">The path to where transaction logs should be created.</param>
        /// <param name="configuration">The <see cref="DatabaseStorageConfiguration"/> to use
        /// </param>
        internal void CreateStorage(string name, string imagePath, string logPath, DatabaseStorageConfiguration configuration) {
            ProcessStartInfo processStart;
            StringBuilder args;

            args = new StringBuilder();
            args.AppendFormat(" -ip \"{0}\"", imagePath);
            args.AppendFormat(" -lp \"{0}\"", logPath);
            args.AppendFormat(" -dbs {0}", configuration.MaxImageSize);
            args.AppendFormat(" -tls {0}", configuration.TransactionLogSize);
            args.AppendFormat(" -coll {0}", configuration.CollationFile);
            if (configuration.SupportReplication) {
                args.Append(" -repl");
            }
            args.AppendFormat(" {0}", name);
            processStart = new ProcessStartInfo(this.creationToolPath, args.ToString().Trim());

            ToolInvocationHelper.InvokeTool(processStart);
        }
    }
}