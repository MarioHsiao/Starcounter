using System.IO;
using System.Runtime.InteropServices;

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
        /// <summary>
        /// Gets the version of the current image file.
        /// </summary>
        public uint Version { get; private set; }

        private ImageFile() {
        }

        /// <summary>
        /// Reads the logical image file of the database with the
        /// given name, looking for it in the given directory.
        /// </summary>
        /// <param name="directory">The directory where the database
        /// image(s) are located.</param>
        /// <param name="databaseName">The name of the database.</param>
        /// <returns>An <see cref="ImageFile"/> representing the logical
        /// image file of the given database.</returns>
        public static ImageFile Read(string directory, string databaseName) {
            var imageFiles = DatabaseStorageService.GetImageFiles(directory, databaseName);
            var size = Marshal.SizeOf(typeof(KernelAPI.NativeStructImageHeader));
            var data = new byte[size];

            // If we can't properly find, read or interpret/validate the
            // content of the given image file, we should issue
            // ScErrCantOpenImageFile and ScErrCantReadImageFile respectively,
            // adding to them the reason why we could not do so (and the
            // proper exception, like FileNotFound, or FormatExceltion.
            // TODO:

            using (var file = File.OpenRead(imageFiles[0])) {
                var read = file.Read(data, 0, data.Length);
                if (read != size) {
                    throw ErrorCode.ToException(Error.SCERRCANTREADIMAGEFILE);
                }
            }

            return ImageFile.FromBytes(data);
        }

        static ImageFile FromBytes(byte[] data) {
            unsafe {
                fixed (byte* p = data) {
                    var ph = (KernelAPI.NativeStructImageHeader*)p;
                    return new ImageFile() { Version = ph->Version };
                }
            }
        }
    }
}
