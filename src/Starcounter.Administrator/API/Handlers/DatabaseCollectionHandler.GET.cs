using Starcounter.Administrator.API.Utilities;
using Starcounter.Advanced;
using Starcounter.Server.Rest.Representations.JSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Administrator.API.Handlers {

    internal static partial class DatabaseCollectionHandler {
        /// <summary>
        /// Handles a GET for this resource.
        /// </summary>
        /// <param name="request">
        /// The REST request.</param>
        /// <returns>The response to be sent back to the client.</returns>
        static Response OnGET(Request request) {
            var server = RootHandler.Host.Runtime;
            var applicationDatabases = server.GetDatabases();
            var admin = RootHandler.API;

            var result = new DatabaseCollection();
            foreach (var applicationDatabase in applicationDatabases) {
                var db = result.Databases.Add();
                db.Name = applicationDatabase.Name;
                db.Uri = admin.Uris.Database.ToAbsoluteUri(applicationDatabase.Name);
            }

            return RESTUtility.JSON.CreateResponse(result.ToJson());
        }
    }
}