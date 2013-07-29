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
using System.Globalization;

namespace Starcounter.Server {
    
    /// <summary>
    /// Exposes a set of methods that can be used to easily work with
    /// database storages, i.e. sets of image- and transaction log files.
    /// </summary>
    internal sealed class DatabaseStorageService {
        const string keyFormat = "yyyyMMddTHHmmssfff";
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
            return DateTime.Now.ToString(keyFormat);
        }

        /// <summary>
        /// Creates a key that the server can use when creating and deleting
        /// database files and folders to assure they are unique, including
        /// the name of the database.
        /// </summary>
        /// <param name="databaseName">The name of the database to be included
        /// in the generated key.</param>
        /// <returns>An opaque key to use in the form of a string.</returns>
        internal string NewNamedKey(string databaseName) {
            return ToNamedKeyFormat(databaseName, NewKey());
        }

        /// <summary>
        /// Checks if the given directory (optinally part of a path) is considered
        /// named with a specified named key combination.
        /// </summary>
        /// <param name="directory">The directory to consult.</param>
        /// <param name="database">The database name in the named key.</param>
        /// <param name="key">The unique, opaque key part of the named key.</param>
        /// <returns>True if the directory match the named key; false otherwise.</returns>
        internal bool IsNamedKeyDirectory(string directory, string database, string key = null) {
            var namedKey = ToNamedKeyFormat(database, key);
            directory = Path.GetFileName(directory.TrimEnd('\\'));
            if (!string.IsNullOrEmpty(key)) {
                // Match exact, including key information.
                return namedKey.Equals(directory, StringComparison.InvariantCultureIgnoreCase);
            }

            string dirDatabaseName;
            FromNamedKeyFormat(directory, out dirDatabaseName, out key);
            if (database.Equals(dirDatabaseName, StringComparison.InvariantCultureIgnoreCase)) {
                try {
                    DateTime.ParseExact(key, keyFormat, CultureInfo.InvariantCulture);
                    return true;
                } catch {
                }
            }

            return false;
        }

        internal string[] GetImageFiles(string directory, string databaseName) {
            var pattern = string.Format("{0}.?.sci", databaseName);
            return Directory.GetFiles(directory, pattern);
        }

        internal string[] GetTransactionLogFiles(string directory, string databaseName) {
            var pattern = string.Format("{0}.???.log", databaseName);
            return Directory.GetFiles(directory, pattern);
        }

        /// <summary>
        /// Creates a new storage for with the given values.
        /// </summary>
        /// <param name="name">The name of database to create a new storage for.</param>
        /// <param name="imagePath">The path to where image files should be created.</param>
        /// <param name="logPath">The path to where transaction logs should be created.</param>
        /// <param name="configuration">The <see cref="DatabaseStorageConfiguration"/> to use
        /// </param>
        internal void CreateStorage(string name, string imagePath, DatabaseStorageConfiguration configuration) {
            ProcessStartInfo processStart;
            StringBuilder args;

            args = new StringBuilder();
            args.AppendFormat(" -ip \"{0}\"", imagePath);
            args.AppendFormat(" -dbs {0}", configuration.MaxImageSize);
            args.AppendFormat(" -coll {0}", configuration.CollationFile);
            args.AppendFormat(" {0}", name);
            processStart = new ProcessStartInfo(this.creationToolPath, args.ToString().Trim());

            ToolInvocationHelper.InvokeTool(processStart);
        }

        static void FromNamedKeyFormat(string namedKey, out string database, out string key) {
            var tokens = namedKey.Split('-');
            database = tokens[0];
            key = tokens[1];
        }

        string ToNamedKeyFormat(string database, string key) {
            return string.Format("{0}-{1}", database, key);
        }
    }
}