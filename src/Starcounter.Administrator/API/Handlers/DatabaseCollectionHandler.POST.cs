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
        static object OnPOST(Request request) {
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
    }
}