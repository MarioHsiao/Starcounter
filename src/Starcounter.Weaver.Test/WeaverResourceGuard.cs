
using Starcounter.Server.Compiler;
using System;
using System.IO;

namespace Starcounter.Weaver.Test
{
    internal class WeaverResourceGuard : IDisposable
    {
        AppCompilerResult compilerResult;
        WeaverSetup weaverSetup;

        public void Add(AppCompilerResult result)
        {
            compilerResult = result;
        }

        public void Add(WeaverSetup setup)
        {
            weaverSetup = setup;
        }

        void IDisposable.Dispose()
        {
            SafeCleanupCompilationAndWeaverResult(compilerResult, weaverSetup);
        }

        public static void SafeCleanupCompilationAndWeaverResult(AppCompilerResult compilerResult, WeaverSetup weaverSetup)
        {
            if (compilerResult != null)
            {
                SafeCleanupCompilationResult(compilerResult);
            }

            if (weaverSetup != null)
            {
                SafeCleanupWeaverResult(weaverSetup);
            }
        }

        public static void SafeCleanupCompilationResult(AppCompilerResult compilerResult, bool deleteTopLevelDirectory = true)
        {
            SafeDeleteFile(compilerResult.ApplicationPath);
            SafeDeleteFile(compilerResult.SymbolFilePath);
            if (deleteTopLevelDirectory)
            {
                SafeDeleteDirectory(compilerResult.OutputDirectory, true);
            }
        }

        public static void SafeCleanupWeaverResult(WeaverSetup setup)
        {
            SafeDeleteDirectory(setup.OutputDirectory, true);
        }

        public static bool? SafeDeleteFile(string file)
        {
            if (!File.Exists(file))
            {
                return null;
            }

            try
            {
                File.Delete(file);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool? SafeDeleteDirectory(string directory, bool recursively = false)
        {
            if (!Directory.Exists(directory))
            {
                return null;
            }

            try
            {
                Directory.Delete(directory, recursive: recursively);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
