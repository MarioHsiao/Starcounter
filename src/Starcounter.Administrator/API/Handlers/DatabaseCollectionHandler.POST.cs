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

            var db = new Database();
            db.PopulateFromJson(request.GetBodyStringUtf8_Slow());

            var name = db.Name;
            var command = new CreateDatabaseCommand(engine, name) {
                EnableWaiting = true
            };

            var info = runtime.Execute(command);
            if (command.EnableWaiting) {
                info = runtime.Wait(info);
            }

            if (info.HasError) {
                // Check for already existing database - return 200 with a reference.
                // And map other errors.
                // TODO:
                throw new NotImplementedException();
            }

            // Assure we only return the neccessary bits (in accordance with with our
            // REST API documentation) by recreating the database object and set them.
            
            db = new Database();
            db.Name = name;
            db.Uri = RootHandler.MakeAbsoluteUri(admin.Uris.Database, name);

            return RESTUtility.CreateJSONResponse(db.ToJson(), 201);
        }
    }
}