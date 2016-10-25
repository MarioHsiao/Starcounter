
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

            var setup = new WeaverSetup()
            {
                InputDirectory = existingDir,
                AssemblyFile = existingFile,
                OutputDirectory = nonExistingDir,
                CacheDirectory = existingDir
            };

            var e = Assert.Throws<ArgumentNullException>(() => WeaverFactory.CreateWeaver(setup, null));
            Assert.AreEqual(e.ParamName.ToLowerInvariant(), "host");

            setup = new WeaverSetup()
            {
                InputDirectory = nonExistingDir,
                AssemblyFile = existingFile,
                OutputDirectory = existingDir,
                CacheDirectory = existingDir
            };
            Assert.Throws<DirectoryNotFoundException>(() => WeaverFactory.CreateWeaver(setup, new DefaultTestWeaverHost()));

            setup = new WeaverSetup()
            {
                InputDirectory = null,
                AssemblyFile = existingFile,
                OutputDirectory = existingDir,
                CacheDirectory = existingDir
            };
            Assert.Throws<ArgumentNullException>(() => WeaverFactory.CreateWeaver(setup, new DefaultTestWeaverHost()));

            setup = new WeaverSetup()
            {
                InputDirectory = existingDir,
                AssemblyFile = existingFile,
                OutputDirectory = nonExistingDir,
                CacheDirectory = existingDir
            };
            Assert.Throws<DirectoryNotFoundException>(() => WeaverFactory.CreateWeaver(setup, new DefaultTestWeaverHost()));

            setup = new WeaverSetup()
            {
                InputDirectory = existingDir,
                AssemblyFile = existingFile,
                OutputDirectory = null,
                CacheDirectory = existingDir
            };
            Assert.Throws<ArgumentNullException>(() => WeaverFactory.CreateWeaver(setup, new DefaultTestWeaverHost()));

            setup = new WeaverSetup()
            {
                InputDirectory = existingDir,
                AssemblyFile = existingFile,
                OutputDirectory = existingDir,
                CacheDirectory = nonExistingDir
            };
            Assert.Throws<DirectoryNotFoundException>(() => WeaverFactory.CreateWeaver(setup, new DefaultTestWeaverHost()));

            setup = new WeaverSetup()
            {
                InputDirectory = existingDir,
                AssemblyFile = existingFile,
                OutputDirectory = existingDir,
                CacheDirectory = null
            };
            Assert.Throws<ArgumentNullException>(() => WeaverFactory.CreateWeaver(setup, new DefaultTestWeaverHost()));

            setup = new WeaverSetup()
            {
                InputDirectory = existingDir,
                AssemblyFile = nonExistingFile,
                OutputDirectory = existingDir,
                CacheDirectory = existingDir
            };
            Assert.Throws<FileNotFoundException>(() => WeaverFactory.CreateWeaver(setup, new DefaultTestWeaverHost()));

            setup = new WeaverSetup()
            {
                InputDirectory = existingDir,
                AssemblyFile = null,
                OutputDirectory = existingDir,
                CacheDirectory = nonExistingDir
            };
            Assert.Throws<ArgumentNullException>(() => WeaverFactory.CreateWeaver(setup, new DefaultTestWeaverHost()));
        }

        [Test]
        public void CompileAndWeaveMinimalApp()
        {
            var compiler = new AppCompiler("app")
                .WithDefaultReferences()
                .WithSourceCode("class Program { static void Main() {} }")
                .WithSourceCode("[Starcounter.Database] public class Foo { }");

            var result = compiler.Compile();

            IWeaver weaver = null;
            try
            {
                var outDir = Path.Combine(result.OutputDirectory, ".weaved");
                var weaverCache = Path.Combine(result.OutputDirectory, ".weavercache");
                Directory.CreateDirectory(outDir);
                Directory.CreateDirectory(weaverCache);

                var weaverSetup = new WeaverSetup()
                {
                    InputDirectory = result.OutputDirectory,
                    AssemblyFile = result.ApplicationPath,
                    OutputDirectory = outDir,
                    CacheDirectory = weaverCache
                };

                weaver = WeaverFactory.CreateWeaver(weaverSetup, new DefaultTestWeaverHost());
                weaver.Execute();
                Assert.IsTrue(true);
            }
            finally
            {
                SafeCleanupCompilationAndWeaverResult(result, weaver);
            }
        }

        [Test]
        public void CompileAndWeaveMinimalAppInOtherDomain()
        {
            var compiler = new AppCompiler("app")
                .WithDefaultReferences()
                .WithSourceCode("class Program { static void Main() {} }")
                .WithSourceCode("[Starcounter.Database] public class Foo { }");

            var result = compiler.Compile();

            IWeaver weaver = null;
            try
            {
                var outDir = Path.Combine(result.OutputDirectory, ".weaved");
                var weaverCache = Path.Combine(result.OutputDirectory, ".weavercache");
                Directory.CreateDirectory(outDir);
                Directory.CreateDirectory(weaverCache);
                
                var weaverSetup = new WeaverSetup()
                {
                    InputDirectory = result.OutputDirectory,
                    AssemblyFile = result.ApplicationPath,
                    OutputDirectory = outDir,
                    CacheDirectory = weaverCache
                };

                weaver = WeaverFactory.CreateWeaver(weaverSetup, typeof(DefaultTestWeaverHost));
                weaver.Execute();
                var weaverResult = true;
                Assert.IsTrue(weaverResult);
            }
            finally
            {
                SafeCleanupCompilationAndWeaverResult(result, weaver);
            }
        }

        static void SafeCleanupCompilationAndWeaverResult(AppCompilerResult compilerResult, IWeaver weaver)
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

        static void SafeCleanupWeaverResult(IWeaver weaver)
        {
            SafeDeleteDirectory(weaver.Setup.OutputDirectory, true);
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
