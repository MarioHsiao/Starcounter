using System.Collections.Generic;
using System.IO;

namespace Starcounter.Weaver {

    public class FilesByDirectory {
        public readonly Dictionary<string, List<string>> Files;

        public FilesByDirectory(List<string> files) {
            var filesByDirectory = new Dictionary<string, List<string>>();

            foreach (var file in files) {
                var dir = Path.GetDirectoryName(file);
                if (!filesByDirectory.ContainsKey(dir)) {
                    var filelist = new List<string>();
                    filesByDirectory.Add(dir, filelist);
                }

                filesByDirectory[dir].Add(Path.GetFileName(file));
            }

            Files = filesByDirectory;
        }
    }
}
