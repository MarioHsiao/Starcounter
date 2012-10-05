
using Starcounter;
using Starcounter.CommandLine;

namespace StarcounterInternal.Bootstrap
{
    
    /// <summary>
    /// Basic host configuration.
    /// </summary>
    public class Configuration
    {
        public ApplicationArguments ProgramArguments { get; private set; }

        public static Configuration Load(ApplicationArguments arguments)
        {
            return new Configuration() { ProgramArguments = arguments };
        }

        private Configuration() { }

        public string Name
        {
            get 
            {
                return this.ProgramArguments.CommandParameters[0];
            }
        }

        public string CompilerPath
        {
            get {
                string prop;

                if (!this.ProgramArguments.TryGetProperty(ProgramCommandLine.OptionNames.CompilerPath, out prop))
                    prop = @"C:/Test/MinGW/bin/x86_64-w64-mingw32-gcc.exe";

                return prop;
            }
        }

        public string DatabaseDirectory
        {
            get {
                string prop;

                if (!this.ProgramArguments.TryGetProperty(ProgramCommandLine.OptionNames.DatabaseDir, out prop))
                    prop = @"C:/Test";

                return prop;
            }
        }

        public string OutputDirectory
        {
            get {
                string prop;

                if (!this.ProgramArguments.TryGetProperty(ProgramCommandLine.OptionNames.OutputDir, out prop))
                    prop = @"C:/Test";

                return prop;
            }
        }

        public string TempDirectory
        {
            get {
                string prop;

                if (!this.ProgramArguments.TryGetProperty(ProgramCommandLine.OptionNames.TempDir, out prop))
                    prop = @"C:/Test/Temp";

                return prop;
            }
        }

        public string AutoStartExePath
        {
            get
            {
                string autoStartExePath;

                if (!this.ProgramArguments.TryGetProperty(ProgramCommandLine.OptionNames.AutoStartExePath, out autoStartExePath))
                    autoStartExePath = null;

                return autoStartExePath;
            }
        }

        public string ServerName
        {
            get
            {
                string serverName;

                if (!this.ProgramArguments.TryGetProperty(ProgramCommandLine.OptionNames.ServerName, out serverName))
                    serverName = "PERSONAL";

                // Making server name upper case.
                serverName = serverName.ToUpper();

                return serverName;
            }
        }

        public uint ChunksNumber
        {
            get
            {
                // Default communication shared chunks number.
                uint chunksNumber = 128;

                string chunksNumberStr;
                if (this.ProgramArguments.TryGetProperty(ProgramCommandLine.OptionNames.ChunksNumber, out chunksNumberStr))
                {
                    chunksNumber = uint.Parse(chunksNumberStr);

                    // Checking if number of chunks is correct.
                    if ((chunksNumber < 128) || (chunksNumber > 4096 * 128))
                    {
                        throw ErrorCode.ToException(Error.SCERRBADCHUNKSNUMBERCONFIG);
                    }
                }

                return chunksNumber;
            }
        }
    }
}
