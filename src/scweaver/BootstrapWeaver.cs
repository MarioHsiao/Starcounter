
using Mono.Cecil;
using Mono.Cecil.Cil;
using Starcounter.Internal;
using System;
using System.Reflection;

namespace Weaver {
    /// <summary>
    /// Implements the new bootstrap weaver, responsible for weaving
    /// executables so that they can be launched from the shell and can
    /// take part in Apps infrastructure initialization (when loaded
    /// into the user code host).
    /// </summary>
    internal static class BootstrapWeaver {
        /// <summary>
        /// Weaves the given executable to support OS shell bootstrapping.
        /// </summary>
        /// <param name="executablePath">The full path to the executable to be
        /// weaved.</param>
        public static void WeaveExecutable(string executablePath) {
            MethodBase appsInitializerMethod;
            MethodBase appsShellBootstrapper;
            MethodReference appsInitializerMethRef;
            MethodReference appsShellBootstrapMethRef;
            MethodDefinition generatedAppsInitializer;

            var assembly = AssemblyDefinition.ReadAssembly(
                executablePath,
                new ReaderParameters(ReadingMode.Immediate) { ReadSymbols = true });
            var voidType = assembly.MainModule.Import(typeof(void));
            var stringType = assembly.MainModule.Import(typeof(string));
            var stringArrayType = assembly.MainModule.Import(typeof(string[]));

            // Set up the proper references. After this sequence, we have made sure
            // the about-to-be-weaved assembly will have proper references to the
            // needed assemblies.

            appsInitializerMethod = typeof(AppsBootstrapper).GetMethod("Bootstrap", new Type[] { typeof(String), typeof(UInt16) });
            appsInitializerMethRef = assembly.MainModule.Import(appsInitializerMethod);
            appsShellBootstrapper = typeof(Starcounter.Apps.Bootstrap.AppProcess).GetMethod("AssertInDatabaseOrSendStartRequest");
            appsShellBootstrapMethRef = assembly.MainModule.Import(appsShellBootstrapper);

            // Define and implement the infrastructure initializer method we'll
            // have the user code bootstrap host look for, and invoke before the
            // entrypoint, if found.
            generatedAppsInitializer = new MethodDefinition(
                "STARCOUNTERGENERATED_InitializeAppsInfrastructure",
                Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.Static,
                voidType);
            generatedAppsInitializer.Parameters.Add(new ParameterDefinition(stringType) { Name = "workingDirectory" });
            generatedAppsInitializer.Parameters.Add(new ParameterDefinition(stringArrayType) { Name = "args" });

            var ilWriter = generatedAppsInitializer.Body.GetILProcessor();
            generatedAppsInitializer.Body.MaxStackSize = 8;
            ilWriter.Emit(OpCodes.Nop);
            ilWriter.Emit(OpCodes.Ldarg_0);
            ilWriter.Emit(OpCodes.Ldc_I4_0);
            ilWriter.Emit(OpCodes.Call, appsInitializerMethRef);
            ilWriter.Emit(OpCodes.Nop);
            ilWriter.Emit(OpCodes.Ret);

            // Add the method to the same type as defines the entrypoint.

            assembly.EntryPoint.DeclaringType.Methods.Add(generatedAppsInitializer);

            // Reimplement the entrypoint to call the static, process-level
            // OS shell bootstrapper API.

            assembly.EntryPoint.Body.GetILProcessor().InsertBefore(
                assembly.EntryPoint.Body.Instructions[0],
                ilWriter.Create(OpCodes.Call, appsShellBootstrapMethRef));

            // Write back the now geared assembly to disk.

            assembly.Write(executablePath, new WriterParameters() { WriteSymbols = true });
        }
    }
}
