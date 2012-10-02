using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Starcounter.Internal {
    public class Paths {
        public static string StripFileNameWithoutExtention(string fileSpec) {
            string fileName = Path.GetFileName(fileSpec);
            string[] parts = fileName.Split('.');
            return parts[0];
        }
    }
}
