
using Mono.Cecil;
using Mono.Cecil.Cil;
using Starcounter.Internal;
using System;
using System.Reflection;

namespace Weaver {
    /// <summary>
    /// Implements the new bootstrap weaver, responsible for weaving
    /// executables so that they can be launched from the shell.
    /// </summary>
    internal static class BootstrapWeaver {
        /// <summary>
        /// Weaves the given executable to support OS shell bootstrapping.
        /// </summary>
        /// <param name="executablePath">The full path to the executable to be
        /// weaved.</param>
        public static void WeaveExecutable(string executablePath) {
            MethodBase appsShellBootstrapper;
            MethodReference appsShellBootstrapMethRef;

            var assembly = AssemblyDefinition.ReadAssembly(
                executablePath,
                new ReaderParameters(ReadingMode.Immediate) { ReadSymbols = true });
            var voidType = assembly.MainModule.Import(typeof(void));
            var stringType = assembly.MainModule.Import(typeof(string));
            var stringArrayType = assembly.MainModule.Import(typeof(string[]));

            // Set up the proper references. After this sequence, we have made sure
            // the about-to-be-weaved assembly will have proper references to the
            // needed assemblies.

			appsShellBootstrapper = typeof(Starcounter.CLI.Shell).GetMethod("BootInHost");
            appsShellBootstrapMethRef = assembly.MainModule.Import(appsShellBootstrapper);

            // Reimplement the entrypoint to call the static, process-level
            // OS shell bootstrapper API.
			var ilWriter = assembly.EntryPoint.Body.GetILProcessor();
			ilWriter.InsertBefore(
				assembly.EntryPoint.Body.Instructions[0],
				ilWriter.Create(OpCodes.Call, appsShellBootstrapMethRef)
			);

            // Write back the now geared assembly to disk.

            assembly.Write(executablePath, new WriterParameters() { WriteSymbols = true });
        }
    }
}
