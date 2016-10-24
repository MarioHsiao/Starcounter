
using NUnit.Framework;
using System;
using Starcounter.Server.Compiler;
using System.IO;
using System.Linq;

namespace Starcounter.Weaver.Test
{
    [TestFixture]
    public class WeaverTests
    {
        [Test]
        public void WeaverWithInvalidArgumentsShouldRaiseException()
        {
            var existingDir = Environment.CurrentDirectory;
            var nonExistingDir = Path.Combine(existingDir, Guid.NewGuid().ToString());
            var existingFile = Directory.GetFiles(existingDir).First();
            var nonExistingFile = Path.Combine(existingDir, Guid.NewGuid().ToString() + ".exe");

            Assert.True(!Directory.Exists(nonExistingDir));
            Assert.True(!File.Exists(nonExistingFile));

            var e = Assert.Throws<ArgumentNullException>(() => new CodeWeaver(
                host: null,
                directory: existingDir, 
                file: existingFile, 
                outputDirectory: existingDir, 
                cacheDirectory : existingDir));
            Assert.AreEqual(e.ParamName.ToLowerInvariant(), "host");

            Assert.Throws<DirectoryNotFoundException>(() => new CodeWeaver(
                host: new DefaultTestWeaverHost(),
                directory: nonExistingDir,
                file: existingFile,
                outputDirectory: existingDir,
                cacheDirectory: existingDir));

            Assert.Throws<ArgumentNullException>(() => new CodeWeaver(
                host: new DefaultTestWeaverHost(),
                directory: null,
                file: existingFile,
                outputDirectory: existingDir,
                cacheDirectory: existingDir));

            Assert.Throws<DirectoryNotFoundException>(() => new CodeWeaver(
                host: new DefaultTestWeaverHost(),
                directory: existingDir,
                file: existingFile,
                outputDirectory: nonExistingDir,
                cacheDirectory: existingDir));

            Assert.Throws<ArgumentNullException>(() => new CodeWeaver(
                host: new DefaultTestWeaverHost(),
                directory: existingDir,
                file: existingFile,
                outputDirectory: null,
                cacheDirectory: existingDir));

            Assert.Throws<DirectoryNotFoundException>(() => new CodeWeaver(
                host: new DefaultTestWeaverHost(),
                directory: existingDir,
                file: existingFile,
                outputDirectory: existingDir,
                cacheDirectory: nonExistingDir));

            Assert.Throws<ArgumentNullException>(() => new CodeWeaver(
                host: new DefaultTestWeaverHost(),
                directory: existingDir,
                file: existingFile,
                outputDirectory: existingDir,
                cacheDirectory: null));

            Assert.Throws<FileNotFoundException>(() => new CodeWeaver(
                host: new DefaultTestWeaverHost(),
                directory: existingDir,
                file: nonExistingFile,
                outputDirectory: existingDir,
                cacheDirectory: existingDir));

            Assert.Throws<ArgumentNullException>(() => new CodeWeaver(
                host: new DefaultTestWeaverHost(),
                directory: existingDir,
                file: null,
                outputDirectory: existingDir,
                cacheDirectory: existingDir));
        }

        [Test]
        public void CompileAndWeaveMinimalApp()
        {
            var compiler = new AppCompiler("app")
                .WithDefaultReferences()
                .WithSourceCode("class Program { static void Main() {} }")
                .WithSourceCode("[Starcounter.Database] public class Foo { }");

            var result = compiler.Compile();

            CodeWeaver weaver = null;
            try
            {
                var outDir = Path.Combine(result.OutputDirectory, ".weaved");
                var weaverCache = Path.Combine(result.OutputDirectory, ".weavercache");
                Directory.CreateDirectory(outDir);
                Directory.CreateDirectory(weaverCache);

                weaver = new CodeWeaver(
                    new DefaultTestWeaverHost(),
                    result.OutputDirectory,
                    result.ApplicationPath,
                    outDir,
                    weaverCache
                );

                var weaverResult = CodeWeaver.ExecuteCurrent(weaver);
                Assert.IsTrue(weaverResult);
            }
            finally
            {
                SafeCleanupCompilationAndWeaverResult(result, weaver);
            }
        }

        static void SafeCleanupCompilationAndWeaverResult(AppCompilerResult compilerResult, CodeWeaver weaver)
        {
            if (compilerResult != null)
            {
                SafeCleanupCompilationResult(compilerResult);
            }

            if (weaver != null)
            {
                SafeCleanupWeaverResult(weaver);
            }
        }

        static void SafeCleanupCompilationResult(AppCompilerResult compilerResult, bool deleteTopLevelDirectory = true)
        {
            SafeDeleteFile(compilerResult.ApplicationPath);
            SafeDeleteFile(compilerResult.SymbolFilePath);
            if (deleteTopLevelDirectory)
            {
                SafeDeleteDirectory(compilerResult.OutputDirectory);
            }
        }

        static void SafeCleanupWeaverResult(CodeWeaver weaver)
        {
            SafeDeleteDirectory(weaver.OutputDirectory, true);
        }

        static bool? SafeDeleteFile(string file)
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

        static bool? SafeDeleteDirectory(string directory, bool recursively = false)
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
