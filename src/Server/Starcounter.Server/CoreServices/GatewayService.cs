#if false // TODO: Remove!
using System.Diagnostics;
using System.IO;

namespace Starcounter.Server {
    /// <summary>
    /// Encapsulates the starting and stopping of the Starcounter
    /// gateway process.
    /// </summary>
    internal sealed class GatewayService {
        readonly ServerEngine engine;
        string gatewayExePath;
        string gatewayXmlConfigPath;
        string monitorOutputDirectory;

        /// <summary>
        /// A constant value holding the name of the gateway executable.
        /// </summary>
        internal const string ExeName = StarcounterConstants.ProgramNames.ScNetworkGateway + ".exe";

        /// <summary>
        /// Launches the Starcounter gateway process with the given arguments.
        /// </summary>
        /// <param name="exePath">The path to the gateway executable.</param>
        /// <param name="serverTypeName">The name/type of the server to give to the gateway.</param>
        /// <param name="xmlConfigPath">The path to the gateway configuration file.</param>
        /// <param name="monitoringOutputPath">The path to the monitoring output directory.</param>
        internal static void LaunchGatewayProcess(
            string exePath,
            string serverTypeName,
            string xmlConfigPath,
            string monitoringOutputPath) {
            var process = new Process();
            process.StartInfo.FileName = exePath;
            process.StartInfo.Arguments = string.Format("{0} \"{1}\" \"{2}\"", serverTypeName, xmlConfigPath, monitoringOutputPath);
            ToolInvocationHelper.StartTool(process);
        }
        
        /// <summary>
        /// Initializes a new <see cref="GatewayService"/> running as part
        /// of the given server engine.
        /// </summary>
        /// <param name="engine">The <see cref="ServerEngine"/> under which
        /// the gateway service runs.</param>
        internal GatewayService(ServerEngine engine) {
            this.engine = engine;
        }

        /// <summary>
        /// Executes setup of the current <see cref="GatewayService"/> when
        /// running as part of a server.
        /// </summary>
        internal void Setup() {
            gatewayExePath = Path.Combine(engine.InstallationDirectory, GatewayService.ExeName);
            if (!File.Exists(gatewayExePath)) {
                throw ErrorCode.ToException(Error.SCERRUNSPECIFIED, string.Format("Unable to find the gateway executable: {0}", gatewayExePath));
            }
            gatewayXmlConfigPath = Path.Combine(engine.InstallationDirectory, "scnetworkgateway.xml");
            if (!File.Exists(gatewayExePath)) {
                throw ErrorCode.ToException(Error.SCERRUNSPECIFIED, string.Format("Unable to find the gateway configuration file: {0}", gatewayXmlConfigPath));
            }
            monitorOutputDirectory = engine.Configuration.LogDirectory;
        }

        /// <summary>
        /// Starts the <see cref="GatewayService"/>, effectively starting the
        /// Starcounter gateway process using arguments fetched from the server
        /// engine under which the current service runs.
        /// </summary>
        internal void Start() {
            LaunchGatewayProcess(gatewayExePath, engine.Name, gatewayXmlConfigPath, monitorOutputDirectory);
        }
    }
}
#endif
