using Starcounter.Administrator.API.Utilities;
using Starcounter.Server.PublicModel;
using Starcounter.Server.Rest.Representations.JSON;

namespace Starcounter.Administrator.API.Handlers
{

    internal static partial class DatabaseHandler {
        /// <summary>
        /// Handles a GET for this resource.
        /// </summary>
        /// <param name="name">
        /// The name of the database to return a representation of.</param>
        /// <param name="request">
        /// The REST request.</param>
        /// <returns>The response to be sent back to the client.</returns>
        static Response OnGET(string name, Request request) {
            var server = RootHandler.Host.Runtime;
            var applicationDatabase = server.GetDatabaseByName(name);
            if (applicationDatabase == null)
                return 404;
            
            var body = ToJSONDatabase(applicationDatabase).ToJson();
            return RESTUtility.JSON.CreateResponse(body);
        }

        /// <summary>
        /// Handles a GET for this resource's configuration.
        /// </summary>
        /// <param name="name">
        /// The name of the database whose config to return a representation of.</param>
        /// <param name="request">
        /// The REST request.</param>
        /// <returns>The response to be sent back to the client.</returns>
        static Response OnGETConfig(string name, Request request) {
            var server = RootHandler.Host.Runtime;
            var applicationDatabase = server.GetDatabaseByName(name);
            if (applicationDatabase == null)
                return 404;

            var body = ToJSONDatabase(applicationDatabase, true).ToJson();
            return RESTUtility.JSON.CreateResponse(body);
        }

        static Database ToJSONDatabase(DatabaseInfo applicationDatabase, bool includeConfiguration = false) {
            var admin = RootHandler.API;

            var db = new Database();
            db.Name = applicationDatabase.Name;
            db.InstanceID = (int)applicationDatabase.InstanceID;
            db.Configuration.Uri = admin.Uris.DatabaseConfiguration.ToAbsoluteUri(applicationDatabase.Name);
            db.Engine.Uri = admin.Uris.Engine.ToAbsoluteUri(applicationDatabase.Name);

            if (includeConfiguration) {
                db.Configuration.DefaultUserHttpPort = applicationDatabase.Configuration.Runtime.DefaultUserHttpPort;
                db.Configuration.DataDirectory = applicationDatabase.Configuration.Runtime.ImageDirectory;
                db.Configuration.TempDirectory = applicationDatabase.Configuration.Runtime.TempDirectory;
                db.Configuration.ChunksNumber = applicationDatabase.Configuration.Runtime.ChunksNumber;
                db.Configuration.SchedulerCount = applicationDatabase.Configuration.Runtime.GetSchedulerCountOrDefault();
                db.Configuration.SQLProcessPort = applicationDatabase.Configuration.Runtime.SQLProcessPort;
                db.Configuration.DefaultSessionTimeoutMinutes = applicationDatabase.Configuration.Runtime.DefaultSessionTimeoutMinutes;
            }

            return db;
        }
    }
}