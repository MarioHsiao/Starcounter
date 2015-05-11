using Codeplex.Data;
using Starcounter.Advanced;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Tools.Service.Task {
    internal class StatusTask {

        public static void Execute(StarcounterWatcher service, out StatusEventArgs statusEventArgs) {

            string url = string.Format("{0}:{1}{2}", service.IPAddress, service.Port, "/api/server");

            Response response = Http.GET(url, null, 10000);

            if (response.IsSuccessStatusCode) {
                bool interactiveMode = false;

                try {
                    dynamic incomingJson = DynamicJson.Parse(response.Body);
                    bool bValid = incomingJson.IsDefined("Context");
                    if (bValid) {
                        string context = incomingJson.Context;
                        interactiveMode = context.Contains('@');
                    }
                }
                catch (Exception e) {
                    throw new Exception(e.ToString());
                }

                statusEventArgs = new StatusEventArgs() { Running = true, InteractiveMode = interactiveMode };

            }
            else {
                statusEventArgs = new StatusEventArgs() { Running = false };
            }
        }

    }
}
