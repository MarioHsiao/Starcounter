using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Server {
    /// <summary>
    /// Represents a logical database image file.
    /// </summary>
    /// <remarks>
    /// Starcounter secure data in image files, normally
    /// two files per database. One contains the latest secured
    /// checkpoint, the other one the upcoming checkpoint.
    /// </remarks>
    public sealed class ImageFile {
        private ImageFile() {
        }

        public static ImageFile Read(string directory, string databaseName) {
            throw new NotImplementedException();
        }
    }
}
