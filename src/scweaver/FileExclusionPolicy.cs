﻿
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Weaver {
    /// <summary>
    /// Governs the weaver file exclusion policy and support an API
    /// for the weaver to use to consult if a given file is to be
    /// excluded from weaving.
    /// </summary>
    internal class FileExclusionPolicy {
        readonly List<Regex> excludes = new List<Regex>();

        public FileExclusionPolicy(string directory) {
            BuildExclusionList(directory);
        }

        public bool IsExcluded(string file) {
            var fileName = Path.GetFileName(file);
            var match = excludes.FirstOrDefault((regex) => { return regex.IsMatch(fileName); });
            return match != null;
        }

        void BuildExclusionList(string directory) {
            foreach (var exclude in new string[] {
                "scerrres.dll",
                "schttpparser.dll",
                "PostSharp*.dll",
                "Roslyn*.dll",
                "FasterThanJson.dll",
                "Newtonsoft.Json.dll",
                "BizArk.Core.dll",
                "HtmlAgilityPack.dll",
                "Starcounter.dll",
                "RGiesecke.DllExport.Metadata.dll",
                "Starcounter.UriMatcher.dll",
                "Starcounter.REST.dll",
                "Starcounter.Node.dll",
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
                "Starcounter.XSON.JsonByExample"
            }) {
                AddExcludeExpression(exclude, excludes);
            }

            AddExcludeExpression("*.vshost.exe", excludes);

            var ignoreFile = Path.Combine(directory, "weaver.ignore");

            if (File.Exists(ignoreFile)) {
                var configuredExcludes = File.ReadAllLines(ignoreFile);
                foreach (var exclude in configuredExcludes) {
                    AddExcludeExpression(exclude, excludes);
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
