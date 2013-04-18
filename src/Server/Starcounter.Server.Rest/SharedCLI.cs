
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Server {
    /// <summary>
    /// Provides a set of utilities that can be used by applications
    /// and tools that offer a command-line interface to Starcounter.
    /// </summary>
    /// <remarks>
    /// Examples of standard components and tools that will use this
    /// is star.exe, staradmin.exe and the Visual Studio extension,
    /// the later supporting customization when debugging executables
    /// via the Debug | Command Line project property.
    /// </remarks>
    public static class SharedCLI {
        /// <summary>
        /// Defines well-known options, offered by most CLI tools.
        /// </summary>
        public static class Option {
            public const string Serverport = "serverport";
            public const string Server = "server";
            public const string ServerHost = "serverhost";
            public const string Db = "database";
            public const string LogSteps = "logsteps";
            public const string NoDb = "nodb";
            public const string NoAutoCreateDb = "noautocreate";
        }
    }
}