using Starcounter.CommandLine;
using Starcounter.Internal;
using Starcounter.Rest.ExtensionMethods;
using Starcounter.Server.Rest;
using Starcounter.Server.Rest.Representations.JSON;
using System;
using System.Collections.Generic;

namespace Starcounter.CLI {
    /// <summary>
    /// Defines an API to for certain administrative tasks that we
    /// support from the CLI, such as listing running applications.
    /// </summary>
    public class AdminCLI {
        readonly ServerReference serverReference;
        readonly AdminAPI adminAPI;
        readonly Node node;

        /// <summary>
        /// Initialize a new <see cref="AdminCLI"/> instance, given
        /// a reference to a server.
        /// </summary>
        /// <param name="server">The server to bind to.</param>
        /// <param name="admin">Optional admin API to use.</param>
        public AdminCLI(ServerReference server, AdminAPI admin = null) {
            serverReference = server;
            adminAPI = admin ?? new AdminAPI();
            node = server.CreateNode();
        }

        /// <summary>
        /// Fetches the set of running applications found on the
        /// target admin server, optionally scoped by a specified
        /// database.
        /// </summary>
        /// <param name="database">Optional database to scope the
        /// request to.</param>
        /// <returns>A list of all applications matching the
        /// criteria, grouped by their database.
        /// </returns>
        public Dictionary<Engine, Executable[]> GetApplications(string database = null) {
            var admin = adminAPI;

            var result = new Dictionary<Engine, Executable[]>();
            var hosts = GetEngines(database);
            foreach (var host in hosts) {
                var apps = new List<Executable>();
                foreach (Engine.ExecutablesJson.ExecutingElementJson application in host.Executables.Executing) {
                    var response = node.GET(node.ToLocal(application.Uri));
                    response.FailIfNotSuccessOr(404);
                    if (response.IsSuccessStatusCode) {
                        var app = new Executable();
                        app.PopulateFromJson(response.Body);
                        apps.Add(app);
                    }
                }
                result.Add(host, apps.ToArray());
            }
            
            return result;
        }

        /// <summary>
        /// Gets the set of running database engines found on the target
        /// admin server, optionally limited to a single database.
        /// </summary>
        /// <param name="database">Optional database to scope the
        /// request to.</param>
        /// <returns>A list of all engines matching the criteria.
        /// </returns>
        public Engine[] GetEngines(string database = null) {
            var admin = adminAPI;

            var response = node.GET(admin.Uris.Engines);
            response.FailIfNotSuccessOr(503);
            if (response.StatusCode == 503) {
                throw ErrorCode.ToException(Error.SCERRSERVERNOTRUNNING);
            }

            // Iterate all engines (i.e. databases that are connected to at
            // least one running database process). Take the lightweight reference
            // and fetch the full engine from it.

            var engines = new EngineCollection();
            engines.PopulateFromJson(response.Body);
            var result = new List<Engine>(engines.Engines.Count);
            foreach (EngineCollection.EnginesElementJson engineRef in engines.Engines) {
                if (database != null && !engineRef.Name.Equals(database, StringComparison.InvariantCultureIgnoreCase)) {
                    continue;
                }

                response = node.GET(node.ToLocal(engineRef.Uri));
                response.FailIfNotSuccessOr(404);
                if (response.IsSuccessStatusCode) {
                    var engine = new Engine();
                    engine.PopulateFromJson(response.Body);
                    result.Add(engine);
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// Gets a set of all databases found on the target
        /// admin server.
        /// </summary>
        /// <returns>List of databases found.</returns>
        public Database[] GetDatabases() {
            var admin = adminAPI;

            var response = node.GET(admin.Uris.Databases);
            response.FailIfNotSuccessOr(503);
            if (response.StatusCode == 503) {
                throw ErrorCode.ToException(Error.SCERRSERVERNOTRUNNING);
            }

            var refs = new DatabaseCollection();
            refs.PopulateFromJson(response.Body);
            var result = new List<Database>(refs.Databases.Count);
            foreach (DatabaseCollection.DatabasesElementJson dbref in refs.Databases) {
                response = node.GET(node.ToLocal(dbref.Uri));
                response.FailIfNotSuccessOr(404);
                if (response.IsSuccessStatusCode) {
                    var db = new Database();
                    db.PopulateFromJson(response.Body);
                    result.Add(db);
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// Stops the database specified in <paramref name="database"/>.
        /// </summary>
        /// <param name="database">The database to stop.</param>
        /// <param name="writeErrorToConsole">Instructs the CLI library to
        /// write out eventual error information to the console.</param>
        /// <returns>Zero on success; an error code otherwise. If an error
        /// is returned, a formatted error has already been written to the
        /// console, depending on the value of </returns>
        /// <param name="failIfMissing">Indicates if the method should
        /// treat it as a failure if the database does not exist.</param>
        public int StopDatabase(string database, bool writeErrorToConsole = false, bool failIfMissing = false) {
            if (string.IsNullOrWhiteSpace(database)) {
                throw new ArgumentNullException("database");
            }

            var response = node.DELETE(
                adminAPI.FormatUri(adminAPI.Uris.Engine, database), (string)null, null);

            if (!response.IsSuccessStatusCode) {
                ErrorMessage msg = null;
                try {
                    var detail = new ErrorDetail();
                    detail.PopulateFromJson(response.Body);
                    
                    if (writeErrorToConsole) {
                        if (failIfMissing || detail.ServerCode != Error.SCERRDATABASENOTFOUND) {
                            SharedCLI.ShowErrorAndSetExitCode(detail);
                        }
                    }
                    return (int)detail.ServerCode;
                }
                catch {
                    if (response.StatusCode == 503) {
                        throw ErrorCode.ToException(Error.SCERRSERVERNOTRUNNING);
                    }
                    else {
                        // Trigger final fallback
                        response.FailIfNotSuccess();
                    }
                }
                throw msg.ToException();
            }

            return 0;
        }

        /// <summary>
        /// Deletes the database specified in <paramref name="database"/>.
        /// </summary>
        /// <param name="database">The database to delete.</param>
        /// <param name="writeErrorToConsole">Instructs the CLI library to
        /// write out eventual error information to the console.</param>
        /// <returns>Zero on success; an error code otherwise. If an error
        /// is returned, a formatted error has already been written to the
        /// console, depending on the value of </returns>
        /// <param name="failIfMissing">Indicates if the method should
        /// treat it as a failure if the database does not exist.</param>
        /// <param name="stopIfRunning">Stops the database before deleting it, 
        /// if it's found to be running.</param>
        public int DeleteDatabase(string database, bool writeErrorToConsole = false, bool failIfMissing = false, bool stopIfRunning = true) {
            if (string.IsNullOrWhiteSpace(database)) {
                throw new ArgumentNullException("database");
            }

            var response = node.DELETE(
                adminAPI.FormatUri(adminAPI.Uris.Database, database), (string)null, null);
            
            if (!response.IsSuccessStatusCode) {
                ErrorMessage msg = null;
                try {
                    var detail = new ErrorDetail();
                    detail.PopulateFromJson(response.Body);

                    if (stopIfRunning && detail.ServerCode == Error.SCERRDATABASERUNNING) {
                        int stopped = StopDatabase(database, writeErrorToConsole, failIfMissing);
                        if (stopped != 0) {
                            return stopped;
                        }

                        // Invoke ourselves recursively, but without instruction to stop to
                        // make sure we don't acceidentaly end up in a loop we can't get out
                        // of.
                        return DeleteDatabase(database, writeErrorToConsole, failIfMissing, false);
                    }
                    
                    if (writeErrorToConsole) {
                        if (failIfMissing || detail.ServerCode != Error.SCERRDATABASENOTFOUND) {
                            SharedCLI.ShowErrorAndSetExitCode(detail);
                        }
                    }
                    return (int) detail.ServerCode;
                }
                catch {
                    if (response.StatusCode == 503) {
                        throw ErrorCode.ToException(Error.SCERRSERVERNOTRUNNING);
                    }
                    else {
                        // Trigger final fallback
                        response.FailIfNotSuccess();
                    }
                }
                throw msg.ToException();
            }

            return 0;
        }
    }
}