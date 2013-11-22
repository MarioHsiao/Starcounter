using Starcounter.Advanced;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Tools.Service.Task {
    internal class ShutdownTask {

        public static void Execute(StarcounterService service) {

            string uri = string.Format("{0}:{1}{2}", service.IPAddress, service.Port, "/api/server");

            Response resonse = X.DELETE(uri, string.Empty, null, 10000);

            if (!resonse.IsSuccessStatusCode) {
                throw new Exception(resonse.StatusDescription);
            }

        }

    }
}
