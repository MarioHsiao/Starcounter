// ***********************************************************************
// <copyright file="Configuration.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

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

        /// <summary>
        /// Loads the specified program arguments.
        /// </summary>
        /// <param name="programArguments">The program arguments.</param>
        /// <returns>Configuration.</returns>
        public static Configuration Load(ApplicationArguments programArguments)
        {
            return new Configuration(programArguments);
        }

        /// <summary>
        /// Gets or sets the program arguments.
        /// </summary>
        /// <value>The program arguments.</value>
        private ApplicationArguments ProgramArguments { get; set; }

        /// <summary>
        /// Gets the scheduler count.
        /// </summary>
        /// <value>The scheduler count.</value>
        public uint SchedulerCount { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Configuration" /> class.
        /// </summary>
        /// <param name="programArguments">The program arguments.</param>
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

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get 
            {
                return this.ProgramArguments.CommandParameters[0];
            }
        }

        /// <summary>
        /// Gets the compiler path.
        /// </summary>
        /// <value>The compiler path.</value>
        public string CompilerPath
        {
            get {
                string prop;

                if (!this.ProgramArguments.TryGetProperty(ProgramCommandLine.OptionNames.CompilerPath, out prop))
                    prop = @"C:/Test/MinGW/bin/x86_64-w64-mingw32-gcc.exe";

                return prop;
            }
        }

        /// <summary>
        /// Gets the database directory.
        /// </summary>
        /// <value>The database directory.</value>
        public string DatabaseDirectory
        {
            get {
                string prop;

                if (!this.ProgramArguments.TryGetProperty(ProgramCommandLine.OptionNames.DatabaseDir, out prop))
                    prop = @"C:/Test";

                return prop;
            }
        }

        /// <summary>
        /// Gets the output directory.
        /// </summary>
        /// <value>The output directory.</value>
        public string OutputDirectory
        {
            get {
                string prop;

                if (!this.ProgramArguments.TryGetProperty(ProgramCommandLine.OptionNames.OutputDir, out prop))
                    prop = @"C:/Test";

                return prop;
            }
        }

        /// <summary>
        /// Gets the temp directory.
        /// </summary>
        /// <value>The temp directory.</value>
        public string TempDirectory
        {
            get {
                string prop;

                if (!this.ProgramArguments.TryGetProperty(ProgramCommandLine.OptionNames.TempDir, out prop))
                    prop = @"C:/Test/Temp";

                return prop;
            }
        }

        /// <summary>
        /// Gets the auto start exe path.
        /// </summary>
        /// <value>The auto start exe path.</value>
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

        /// <summary>
        /// Gets the name of the server.
        /// </summary>
        /// <value>The name of the server.</value>
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

        /// <summary>
        /// Gets the chunks number.
        /// </summary>
        /// <value>The chunks number.</value>
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

        /// <summary>
        /// Gets the SQL process port.
        /// </summary>
        /// <value>The SQL process port.</value>
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

        /// <summary>
        /// Gets a value indicating whether [no db].
        /// </summary>
        /// <value><c>true</c> if [no db]; otherwise, <c>false</c>.</value>
        public bool NoDb
        {
            get
            {
                return this.ProgramArguments.ContainsFlag(ProgramCommandLine.OptionNames.NoDb);
            }
        }

        /// <summary>
        /// Gets a value indicating whether [network apps].
        /// </summary>
        /// <value><c>true</c> if [network apps]; otherwise, <c>false</c>.</value>
        public bool NetworkApps
        {
            get
            {
                return this.ProgramArguments.ContainsFlag(ProgramCommandLine.OptionNames.NetworkApps);
            }
        }

        /// <summary>
        /// Gets a value indicating that the host should use standard
        /// streams / the console to accept local management requests,
        /// like the booting of executables.
        /// </summary>
        /// <remarks>
        /// Corresponds to the <see cref="ProgramCommandLine.OptionNames.UseConsole"/>
        /// flag.
        /// </remarks>
        /// <value><c>true</c> if standard streams should be used; otherwise,
        /// <c>false</c>.</value>
        public bool UseConsole {
            get {
                return this.ProgramArguments.ContainsFlag(ProgramCommandLine.OptionNames.UseConsole);
            }
        }
    }
}
