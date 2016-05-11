
using Starcounter.Internal;
using Starcounter.Logging;

namespace Starcounter.Server
{
    /// <summary>
    /// Encapsulates the starting and stopping of the Starcounter
    /// gateway process.
    /// </summary>
    internal sealed class GatewayService {
        readonly LogSource log = ServerLogSources.Default;
        readonly ServerEngine engine;
        
        /// <summary>
        /// Initializes a new <see cref="GatewayService"/> running as part
        /// of the given server engine.
        /// </summary>
        /// <param name="engine">The <see cref="ServerEngine"/> under which
        /// the gateway service runs.</param>
        internal GatewayService(ServerEngine engine) {
            this.engine = engine;
            this.log = ServerLogSources.Default;
        }

        /// <summary>
        /// Executes setup of the current <see cref="GatewayService"/> when
        /// running as part of a server.
        /// </summary>
        internal void Setup() {
        }

        /// <summary>
        /// Unregisters existing codehost.
        /// </summary>
        internal void UnregisterCodehost(string databaseName) {
            var body = databaseName + " " + MixedCodeConstants.EndOfRequest;

            var r = Http.DELETE(
                "http://localhost:" + StarcounterEnvironment.Default.SystemHttpPort + "/gw/codehost", body, null);

            if (!r.IsSuccessStatusCode) {
                var errCodeStr = r.Headers[MixedCodeConstants.ScErrorCodeHttpHeader];
                var code = string.IsNullOrWhiteSpace(errCodeStr) ? Error.SCERRUNSPECIFIED : uint.Parse(errCodeStr);

                log.LogError(
                    "GatewayService: failed to unregister code host for database {0}. Gateway returned: {1}/{2}.", databaseName, r.StatusCode, code);

                throw ErrorCode.ToException(code, r.Body);
            }
        }
    }
}
