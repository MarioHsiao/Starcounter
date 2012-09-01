
namespace Starcounter.Internal
{
    
    /// <summary>
    /// Basic host configuration.
    /// </summary>
    internal class Configuration
    {

        internal static Configuration Load()
        {
            return new Configuration();
        }

        private Configuration() { }

        internal string Name
        {
            get { return "DBSERVER1"; }
        }

        internal string CompilerPath
        {
            get { return @"C:/Test/MinGW/bin/x86_64-w64-mingw32-gcc.exe"; }
        }

        internal string DatabaseDirectory
        {
            get { return @"C:/Test"; }
        }

        internal string OutputDirectory
        {
            get { return @"C:/Test"; }
        }

        internal string TempDirectory
        {
            get { return @"C:/Test/Temp"; }
        }
    }
}
