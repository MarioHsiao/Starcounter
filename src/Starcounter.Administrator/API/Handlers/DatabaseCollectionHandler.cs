
using Starcounter.Administrator.API.Utilities;
using Starcounter.Advanced;
using Starcounter.Internal;
using Starcounter.Server.PublicModel;
using Starcounter.Server.Rest.Representations.JSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Administrator.API.Handlers {
    /// <summary>
    /// Provides the handlers for the admin server root database
    /// collection resource.
    /// </summary>
    internal static partial class DatabaseCollectionHandler {
        /// <summary>
        /// Install handlers for the resource represented by this class and
        /// perform custom setup.
        /// </summary>
        internal static void Setup() {
            var uri = RootHandler.API.Uris.Databases;

            Handle.GET<Request>(uri, OnGET);
            Handle.POST<Request>(uri, OnPOST);
            RootHandler.Register405OnAllUnsupported(uri, new string[] { "GET", "POST" });
        }

        internal static Response ToErrorResponse(CommandInfo commandInfo) {
            ErrorInfo single;
            ErrorMessage msg;
            ErrorDetail detail;

            single = null;
            if (ErrorInfoExtensions.TryGetSingleReasonErrorBasedOnServerConvention(commandInfo.Errors, out single)) {
                if (single.GetErrorCode() == Error.SCERRDATABASEALREADYEXISTS || single.GetErrorCode() == Error.SCERRFORBIDDENDATABASENAME) {
                    msg = single.ToErrorMessage();
                    detail = RESTUtility.JSON.CreateError(msg.Code, msg.Body, msg.Helplink);
                    return RESTUtility.JSON.CreateResponse(detail.ToJson(), 422);
                }
            }

            if (single == null)
                single = commandInfo.Errors[0];

            msg = single.ToErrorMessage();
            detail = RESTUtility.JSON.CreateError(msg.Code, msg.ToString(), msg.Helplink);
            return RESTUtility.JSON.CreateResponse(detail.ToJson(), 500);
        }
    }
}