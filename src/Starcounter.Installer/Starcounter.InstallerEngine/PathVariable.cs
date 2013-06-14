
using System;
using System.Text;

namespace Starcounter.InstallerEngine {
    
    /// <summary>
    /// Expose a set of utility methods providing programmatic
    /// access to the environment PATH variable.
    /// </summary>
    public static class PathVariable {

        public static bool ContainsPath(string pathToCheck, EnvironmentVariableTarget target = EnvironmentVariableTarget.Process) {
            var current = Environment.GetEnvironmentVariable("Path", target);
            if (current == null) return false;

            pathToCheck = pathToCheck.TrimEnd('\\');
            foreach (var path in current.Split(';')) {
                var p = path.TrimEnd('\\');
                if (p.Equals(pathToCheck, StringComparison.InvariantCultureIgnoreCase)) {
                    return true;
                }
            }

            return false;
        }

        public static bool AddPath(string pathToAdd, EnvironmentVariableTarget target = EnvironmentVariableTarget.Process) {
            var current = Environment.GetEnvironmentVariable("Path", target);
            pathToAdd = pathToAdd.TrimEnd('\\');
            var builder = new StringBuilder();

            if (current != null) {
                foreach (var path in current.Split(';')) {
                    var p = path.TrimEnd('\\');
                    if (p.Equals(pathToAdd, StringComparison.InvariantCultureIgnoreCase)) {
                        return false;
                    }
                    builder.Append(path);
                    builder.Append(";");
                }
            }

            builder.Append(pathToAdd);
            Environment.SetEnvironmentVariable("Path", builder.ToString(), target);
            return true;
        }

        public static bool RemovePath(string pathToRemove, EnvironmentVariableTarget target = EnvironmentVariableTarget.Process) {
            var current = Environment.GetEnvironmentVariable("Path", target);
            if (current == null) return false;

            pathToRemove = pathToRemove.TrimEnd('\\');
            var builder = new StringBuilder();
            var found = false;

            foreach (var path in current.Split(';')) {
                var p = path.TrimEnd('\\');
                if (p.Equals(pathToRemove, StringComparison.InvariantCultureIgnoreCase)) {
                    found = true;
                    continue;
                }
                builder.Append(path);
                builder.Append(";");
            }

            if (found) {
                if (builder.Length > 0) {
                    builder.Remove(builder.Length - 1, 1);
                }
                Environment.SetEnvironmentVariable("Path", builder.ToString(), target);
            }

            return found;
        }
    }
}
