
using Starcounter.Weaver;
using System.IO;

namespace Starcounter.CLI.Weaver
{
    /// <summary>
    /// Weaver utility used by our CLI tools when they need to weave
    /// on the fly, providing good and unified defaults for that.
    /// </summary>
    public class CLIToolingWeaver
    {
        /// <summary>
        /// Weaves the given app. If successfull, the path will point to the weaved
        /// application on return.
        /// </summary>
        /// <param name="executablePath">Path to app executable.</param>
        public static uint Weave(ref string executablePath)
        {
            var appDir = Path.GetDirectoryName(executablePath);
            var starDir = Path.Combine(appDir, ".starcounter");
            var cacheDirectory = Path.Combine(starDir, "cache");

            if (!Directory.Exists(starDir))
            {
                Directory.CreateDirectory(starDir);
            }

            if (!Directory.Exists(cacheDirectory))
            {
                Directory.CreateDirectory(cacheDirectory);
            }
           
            var weaver = new InPlaceWeaver(executablePath, cacheDirectory);
            var result = weaver.Weave(typeof(ConsoleWriterWeaverHost));
            if (result)
            {
                weaver.CopyWeavedArtifactsToTargetDirectory();
                return 0;
            }
            else
            {
                // The host will have reported the specific error(s) to
                // the console.

                return Error.SCERRWEAVINGERROR;
            }
        }
    }
}
