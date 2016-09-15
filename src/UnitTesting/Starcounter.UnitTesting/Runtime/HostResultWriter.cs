
using System.IO;

namespace Starcounter.UnitTesting.Runtime
{
    public class HostResultWriter
    {
        internal readonly string ResultFile;
        public readonly TestHost Host;
        public readonly StreamWriter Writer;

        internal HostResultWriter(TestHost host, string filePath)
        {
            Host = host;
            ResultFile = filePath;
            Writer = File.CreateText(filePath);
        }

        internal void Close()
        {
            Writer.Flush();
            Writer.Close();
        }
    }
}
