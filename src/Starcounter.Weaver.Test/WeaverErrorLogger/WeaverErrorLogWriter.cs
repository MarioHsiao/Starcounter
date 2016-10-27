using System.Collections.Generic;
using System.IO;

namespace Starcounter.Weaver.Test
{
    internal class WeaverErrorLogWriter
    {
        readonly string path;
        readonly IEnumerable<ErrorAndMessage> content;

        public WeaverErrorLogWriter(string filePath, IEnumerable<ErrorAndMessage> errors)
        {
            path = filePath;
            content = errors;
        }

        public void Write()
        {
            using (var file = File.CreateText(path))
            {
                foreach (var error in content)
                {
                    error.WriteTo(file);
                }

                file.Flush();
                file.Close();
            }
        }
    }
}
