
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Starcounter.Internal.ExeModule {

    /// <summary>
    /// Message sent to the Starcounter Node process to make it load this .EXE module
    /// dynamically. The working directory is used to avoid the need to deploy (web) content.
    /// Instead, the content is fetched at the output directory provided by the user to MsBuild (i.e. the output directory).
    /// </summary>
    public class AppExeModule {
        /// <summary>
        /// The filename and path of the .EXE file
        /// </summary>
        public string ExeFileSpec;
        /// <summary>
        /// The current directory when the .EXE was started by the user.
        /// </summary>
        public string WorkingDirectory;

        public static bool IsRunningTests = false;


        /// <summary>
        /// After the server has started, the following expression is started using the command line start command. Observe that
        /// you can provide a http:// url as a start argument.
        /// </summary>
        public string Start { get; set; }

        /// <summary>
        /// A list of ports to use by default to listen to incoming http requests. If not set, port 80 will be used.
        /// </summary>
        public UInt16 HttpPorts { get; set; }

        /// <summary>
        /// Parses command line arguments into a new parameter object.
        /// TODO. Introduce proper parsing
        /// </summary>
        /// <param name="cla">The command line parameters to parse</param>
        /// <returns>An object containing the named parameters available in a Starcounter .EXE module</returns>
        public void ParseCommandLineArgs(string[] cla) {
           HttpPorts = 8080;
           foreach (var str in cla) {
              var upper = str.ToUpper();
              var c = upper[0];
              switch (c) {
                 case 'S':
                    if (upper.StartsWith("START")) {
                       Start = str.Substring(6);
                    }
                    break;
                 case 'H':
                    if (upper.StartsWith("HTTP_PORT")) {
                       HttpPorts = Convert.ToUInt16(str.Substring(11));
                    }
                    break;
              }
           }
        }

    }
}
