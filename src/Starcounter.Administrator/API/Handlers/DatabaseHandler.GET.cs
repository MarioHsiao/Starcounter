using Starcounter.Administrator.API.Utilities;
using Starcounter.Advanced;
using Starcounter.Server.PublicModel;
using Starcounter.Server.Rest.Representations.JSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Administrator.API.Handlers {

    internal static partial class DatabaseHandler {
        /// <summary>
        /// Handles a GET for this resource.
        /// </summary>
        /// <param name="name">
        /// The name of the database to return a representation of.</param>
        /// <param name="request">
        /// The REST request.</param>
        /// <returns>The response to be sent back to the client.</returns>
        static object OnGET(string name, Request request) {
            var server = RootHandler.Host.Runtime;
            var applicationDatabase = server.GetDatabaseByName(name);
            if (applicationDatabase == null)
                return 404;

            var body = ToJSONDatabase(applicationDatabase).ToJson();
            return RESTUtility.JSON.CreateResponse(body);
        }

        static Database ToJSONDatabase(DatabaseInfo applicationDatabase) {
            var admin = RootHandler.API;
            var engineUri = admin.FormatUri(admin.Uris.Engine, applicationDatabase.Name);
            
            var db = new Database();
            db.Name = applicationDatabase.Name;
            db.Engine.Uri = RootHandler.MakeAbsoluteUri(engineUri);
            return db;
        }
    }
}