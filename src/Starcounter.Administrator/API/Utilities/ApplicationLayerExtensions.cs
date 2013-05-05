using Starcounter.Server.PublicModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Administrator.API.Utilities {
    /// <summary>
    /// Provides a set of utility methods that extends the application
    /// layer, promoting code sharing inside the REST API.
    /// </summary>
    internal static class ApplicationLayerExtensions {

        public static bool HasNoDbSwitch(this EngineInfo engine) {
            var args = engine.CodeHostArguments;
            return string.IsNullOrEmpty(args) ? false : ContainsFlag(args, "NoDb");
        }

        public static bool HasLogStepsSwitch(this EngineInfo engine) {
            var args = engine.CodeHostArguments;
            return string.IsNullOrEmpty(args) ? false : ContainsFlag(args, "LogSteps");
        }

        public static AppInfo GetExecutable(this EngineInfo engine, string exePath) {
            foreach (var exe in engine.HostedApps) {
                if (exe.ExecutablePath.Equals(exePath, StringComparison.InvariantCultureIgnoreCase)) {
                    return exe;
                }
            }
            return null;
        }

        static bool ContainsFlag(string arguments, string flag) {
            var compare = StringComparison.InvariantCultureIgnoreCase;
            var candidates = new string[] { "--FLAG:" + flag, "--" + flag };
            foreach (var candidate in candidates) {
                if (arguments.IndexOf(candidate, compare) != -1) {
                    return true;
                }
            }
            return false;
        }
    }
}
