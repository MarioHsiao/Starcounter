using System.Collections.Generic;
using System.IO;

namespace Starcounter.Weaver.Test
{
    internal class WeaverHostLoggingErrors : DefaultTestWeaverHost
    {
        public const string FilePropertyKey = nameof(WeaverHostLoggingErrors) + "." + "LogFilePath";
        WeaverSetup weaverSetup;
        
        readonly List<ErrorAndMessage> errors = new List<ErrorAndMessage>();

        public override void OnWeaverSetup(WeaverSetup setup)
        {
            weaverSetup = setup;
        }

        public override void WriteError(uint code, string message, params object[] parameters)
        {
            var msg = string.Format(message, parameters);
            errors.Add(new ErrorAndMessage() { Code = code, Message = msg });
        }

        public override void OnWeaverDone(bool result)
        {
            WriteToLog();
        }

        void WriteToLog()
        {
            string logFile;
            var configured = weaverSetup.HostProperties.TryGetValue(FilePropertyKey, out logFile);
            if (configured)
            {
                var logger = new WeaverErrorLogWriter(logFile, errors);
                logger.Write();
            }
        }
    }
}
