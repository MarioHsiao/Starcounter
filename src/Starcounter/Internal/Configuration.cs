
using Starcounter.CommandLine;

namespace Starcounter.Internal
{
    
    /// <summary>
    /// Basic host configuration.
    /// </summary>
    internal class Configuration
    {
        internal ApplicationArguments ProgramArguments { get; private set; }

        internal static Configuration Load(ApplicationArguments arguments)
        {
            return new Configuration() { ProgramArguments = arguments };
        }

        private Configuration() { }

        internal string Name
        {
            get 
            {
                return this.ProgramArguments.CommandParameters[0];
            }
        }

        internal string CompilerPath
        {
            get {
                string prop;

                if (!this.ProgramArguments.TryGetProperty("CompilerPath", out prop))
                    prop = @"C:/Test/MinGW/bin/x86_64-w64-mingw32-gcc.exe";

                return prop;
            }
        }

        internal string DatabaseDirectory
        {
            get {
                string prop;
                
                if (!this.ProgramArguments.TryGetProperty("DatabaseDir", out prop))
                    prop = @"C:/Test";

                return prop;
            }
        }

        internal string OutputDirectory
        {
            get {
                string prop;

                if (!this.ProgramArguments.TryGetProperty("OutputDir", out prop))
                    prop = @"C:/Test";

                return prop;
            }
        }

        internal string TempDirectory
        {
            get {
                string prop;

                if (!this.ProgramArguments.TryGetProperty("TempDir", out prop))
                    prop = @"C:/Test/Temp";

                return prop;
            }
        }
    }
}
