using Starcounter.Administrator.API.Utilities;
using Starcounter.Advanced;
using Starcounter.Server.PublicModel.Commands;
using Starcounter.Server.Rest.Representations.JSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Administrator.API.Handlers {
    
    internal static partial class DatabaseCollectionHandler {
        /// <summary>
        /// Handles a POST to this resource.
        /// </summary>
        /// <param name="request">
        /// The REST request.</param>
        /// <returns>The response to be sent back to the client.</returns>
        static Response OnPOST(Request request) {
            var engine = RootHandler.Host.Engine;
            var runtime = RootHandler.Host.Runtime;
            var admin = RootHandler.API;
            Database db;

            var response = RESTUtility.JSON.CreateFromRequest<Database>(request, out db);
            if (response != null)
                return response;

            var name = db.Name;
            var command = new CreateDatabaseCommand(engine, name) {
                EnableWaiting = true
            };
            ApplyCustomSettings(command, db);

            var info = runtime.Execute(command);
            if (command.EnableWaiting) {
                info = runtime.Wait(info);
            }
            if (info.HasError) {
                return ToErrorResponse(info);
            }

            // Assure we only return the neccessary bits (in accordance with with our
            // REST API documentation) by recreating the database object and set them.
            
            db = new Database();
            db.Name = name;
            db.Uri = admin.Uris.Database.ToAbsoluteUri(name);

            return RESTUtility.JSON.CreateResponse(db.ToJson(), 201);
        }

        static void ApplyCustomSettings(CreateDatabaseCommand cmd, Database db) {
            var config = cmd.SetupProperties.Configuration;
            var storageConfig = cmd.SetupProperties.StorageConfiguration;

            if (db.Configuration.DefaultUserHttpPort > 0) {
                config.Runtime.DefaultUserHttpPort = (ushort)db.Configuration.DefaultUserHttpPort;
            }
            if (!string.IsNullOrWhiteSpace(db.Configuration.DataDirectory)) {
                config.Runtime.ImageDirectory = db.Configuration.DataDirectory;
                config.Runtime.TransactionLogDirectory = db.Configuration.DataDirectory;
            }
            if (!string.IsNullOrWhiteSpace(db.Configuration.TempDirectory)) {
                config.Runtime.ImageDirectory = db.Configuration.DataDirectory;
                config.Runtime.TempDirectory = db.Configuration.TempDirectory;
            }
            if (db.Configuration.FirstObjectID > 0) {
                storageConfig.FirstObjectID = db.Configuration.FirstObjectID;
            }
            if (db.Configuration.LastObjectID > 0) {
                storageConfig.LastObjectID = db.Configuration.LastObjectID;
            }
        }
    }
}