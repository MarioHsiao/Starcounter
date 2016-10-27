using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Starcounter.Weaver.Test
{
    internal class WeaverErrorLog
    {
        public readonly IEnumerable<ErrorAndMessage> Errors;

        private WeaverErrorLog(IEnumerable<ErrorAndMessage> errors)
        {
            Errors = errors;
        }

        public static WeaverErrorLog OpenThenDelete(string file)
        {
            var log = Open(file);
            File.Delete(file);
            return log;
        }

        public static WeaverErrorLog Open(string file)
        {
            var errors = new List<ErrorAndMessage>();

            using (var reader = File.OpenText(file))
            {
                do
                {
                    var error = ErrorAndMessage.ReadFrom(reader);
                    if (error == null)
                    {
                        break;
                    }
                    errors.Add(error);

                } while (true);
            }

            return new WeaverErrorLog(errors);
        }

        public ErrorAndMessage GetSingleErrorMessage(uint code)
        {
            return Errors.Single((candidate) => candidate.Code == code);
        }
    }
}
