using StarcounterInternal.Hosting;
using System;
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

            // If image files not found - returning null.
            if (imageFiles.Length < 1) {
                return null;
            }

            var size = Marshal.SizeOf(typeof(KernelAPI.NativeStructImageHeader));
            var data = new byte[size];

            // If we can't properly find, read or interpret/validate the
            // content of the given image file, we should issue
            // ScErrCantOpenImageFile and ScErrCantReadImageFile respectively,
            // adding to them the reason why we could not do so (and the
            // proper exception, like FileNotFound, or FormatExceltion.
            // TODO:

            using (var file = File.Open(imageFiles[0], FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                var read = file.Read(data, 0, data.Length);
                if (read != size) {
                    throw ErrorCode.ToException(Error.SCERRCANTREADIMAGEFILE, 
                        string.Format("Unable to read image header; only {0} of {1} bytes read.", read, size));
                }
            }

            return ImageFile.FromBytes(data);
        }

        /// <summary>
        /// Gets current image version as expected by the runtime of the
        /// current installation.
        /// </summary>
        /// <returns>The image version of the current runtime.</returns>
        public static uint GetRuntimeImageVersion() {
            uint version, ignored;
            orange.GetRuntimeImageSymbols(out version, out ignored);
            return version;
        }

        static ImageFile FromBytes(byte[] data) {
            uint version, magic;
            orange.GetRuntimeImageSymbols(out version, out magic);

            unsafe {
                fixed (byte* p = data) {
                    var ph = (KernelAPI.NativeStructImageHeader*)p;
                    if (ph->Magic != magic) {
                        throw ErrorCode.ToException(
                            Error.SCERRCANTREADIMAGEFILE,
                            string.Format("Magic number didn't match; {0} != {1}.", ph->Magic, magic));
                    }
                    return new ImageFile() { Version = ph->Version };
                }
            }
        }
    }
}
