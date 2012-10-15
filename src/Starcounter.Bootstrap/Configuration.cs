
using Starcounter;
using Starcounter.CommandLine;
using System;

namespace StarcounterInternal.Bootstrap
{
    
    /// <summary>
    /// Basic host configuration.
    /// </summary>
    public class Configuration
    {

        public static Configuration Load(ApplicationArguments programArguments)
        {
            return new Configuration(programArguments);
        }

        private ApplicationArguments ProgramArguments { get; set; }

        public uint SchedulerCount { get; private set; }

        private Configuration(ApplicationArguments programArguments)
        {
            ProgramArguments = programArguments;

            string prop;
            if (ProgramArguments.TryGetProperty(ProgramCommandLine.OptionNames.SchedulerCount, out prop))
            {
                try
                {
                    SchedulerCount = uint.Parse(prop);
                }
                catch (Exception e)
                {
                    throw ErrorCode.ToException(Error.SCERRBADSCHEDCOUNTCONFIG, e);
                }
            }
            else
            {
                SchedulerCount = (uint)Environment.ProcessorCount;
            }
        }

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
                uint chunksNumber = 1 << 14; // 16K chunks.

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

        public int SQLProcessPort
        {
            get
            {
                int v = 0;
                string str;
                if (this.ProgramArguments.TryGetProperty(ProgramCommandLine.OptionNames.SQLProcessPort, out str))
                {
                    v = int.Parse(str);
                }
                return v;
            }
        }

        public bool NoDb {
            get {
                return this.ProgramArguments.ContainsFlag(ProgramCommandLine.OptionNames.NoDb);
            }
        }
    }
}
