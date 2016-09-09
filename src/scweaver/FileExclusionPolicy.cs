
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Starcounter.Weaver {
    /// <summary>
    /// Governs the weaver file exclusion policy and support an API
    /// for the weaver to use to consult if a given file is to be
    /// excluded from weaving.
    /// </summary>
    internal class FileExclusionPolicy {
        readonly List<Regex> excludes = new List<Regex>();
        string[] configuredExcludes = null;

        public FileExclusionPolicy(string directory) {
            BuildExclusionList(directory);
        }

        public bool IsExcluded(string file) {
            var fileName = Path.GetFileName(file);
            var match = excludes.FirstOrDefault((regex) => { return regex.IsMatch(fileName); });
            return match != null;
        }

        public void BootDiagnose() {
            Program.WriteDebug("File exclusion policy:");

            var configCount = configuredExcludes == null ? 0 : configuredExcludes.Length;
            if (configCount == 0) {
                Program.WriteDebug(  "(No files configured with weaver.ignore)");
            }
            else {
                Program.WriteDebug("  {0} weaver.ignore files:", configCount);
                foreach (var ignore in configuredExcludes) {
                    Program.WriteDebug("  " + ignore);
                }
            }

            // Emit all resolved expressions too?
        }

        void BuildExclusionList(string directory) {
            foreach (var exclude in new string[] {
                "scerrres.dll",
                "schttpparser.dll",
                "PostSharp*.dll",
                "FasterThanJson.dll",
                "BizArk.Core.dll",
                "HtmlAgilityPack.dll",
                "Starcounter.dll",
                "RGiesecke.DllExport.Metadata.dll",
                "Starcounter.UriMatcher.dll",
                "Starcounter.REST.dll",
                "Starcounter.XSON.dll",
                "Starcounter.Logging.dll",
                "Starcounter.Hosting.dll",
                "Starcounter.Hypermedia.dll",
                "Starcounter.Bootstrap.dll",
                "Starcounter.Apps.JsonPatch.dll",
                "Starcounter.Internal.dll",
                "Starcounter.BitsAndBytes.Unsafe.dll",
                "Starcounter.XSON.JsTemplateParser.dll",
                "Mono.CSharp.dll",
                "NetworkIoTest.exe",
                "Starcounter.XSON.JsonByExample",
                "FSharp.Core",
                "xunit.*.dll"
            }) {
                AddExcludeExpression(exclude, excludes);
            }

            AddExcludeExpression("*.vshost.exe", excludes);

            var ignoreFile = Path.Combine(directory, "weaver.ignore");

            if (File.Exists(ignoreFile)) {
                configuredExcludes = File.ReadAllLines(ignoreFile);
                foreach (var exclude in configuredExcludes) {
                    var expr = exclude.Trim();
                    if (expr.Length != exclude.Length) {
                        Program.WriteInformation("Trimmed weaver.ignore expression \"{0}\" from whitespaces.", exclude);
                    }

                    AddExcludeExpression(expr, excludes);
                }
            }
        }

        void AddExcludeExpression(string specification, List<Regex> target) {
            target.Add(
                new Regex("^" + specification.Replace(".", "\\.").Replace("?", ".").Replace("*", ".*"),
                    RegexOptions.IgnoreCase
                    ));
        }
    }
}
