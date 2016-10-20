
using NUnit.Framework;
using Starcounter.Server.Compiler;
using System;
using System.IO;

namespace Starcounter.Server.Test
{
    public class AppCompilerTests
    {

        [Test]
        public void CompileNoSource()
        {
            var c = new AppCompiler("app").WithDefaultReferences();
            Assert.IsNotEmpty(c.MetadataReferences);
            var e = Assert.Throws<AppCompilerException>(() => c.Compile());
            Assert.True(e.Error == AppCompilerError.NoSourceSpecified);
        }

        [Test]
        public void CompileWithInvalidTargetPath()
        {
            var c = new AppCompiler("app")
                .WithDefaultReferences()
                .WithSourceCode("//Will be ignored");
            c.TargetPath = $@"This\Path\Should\Certainly\Not\{Guid.NewGuid().ToString()}\Exist";

            Assert.Throws<DirectoryNotFoundException>(() => c.Compile());
        }

        [Test]
        public void CompileMinimalApp()
        {
            var c = new AppCompiler("app")
                .WithDefaultReferences()
                .WithSourceCode("class Program { static void Main() {} }");

            var result = c.Compile();
            try
            {
                Assert.IsNotNull(result);
                Assert.IsNotNullOrEmpty(result.ApplicationPath);
                Assert.True(File.Exists(result.ApplicationPath));
            }
            finally
            {
                DeleteCompilerResult(result);
            }
        }

        [Test]
        public void CompileMinimalAppFromSourceOnDisk()
        {
            var c = new AppCompiler("app")
                .WithDefaultReferences()
                .WithSourceCodeFile(TestInputFile("EmptyProgramWithEmptyMain.cs"));

            var result = c.Compile();
            try
            {
                Assert.IsNotNull(result);
                Assert.IsNotNullOrEmpty(result.ApplicationPath);
                Assert.True(File.Exists(result.ApplicationPath));
            }
            finally
            {
                DeleteCompilerResult(result);
            }
        }

        [Test]
        public void AssureCompilerPreserveSpecifiedTargetPath()
        {
            // When we give the compiler an explicit target path, where
            // we ask it to compile into, make sure it doesn't add any
            // files except the artifacts.

            var inputFile = TestInputFile(@"WellDefinedInputFolder\MinimalIsolatedApp.cs");
            var targetDir = Path.GetFullPath(Path.GetDirectoryName(inputFile));

            // Before compiling, assert there is just our single file
            Assert.True(File.Exists(inputFile));
            Assert.AreEqual(1, Directory.GetFiles(targetDir).Length);

            var c = new AppCompiler("app")
                .WithDefaultReferences()
                .WithSourceCodeFile(inputFile);
            c.TargetPath = targetDir;

            var result = c.Compile();
            try
            {
                Assert.IsNotNull(result);
                Assert.IsNotNullOrEmpty(result.ApplicationPath);
                Assert.True(File.Exists(result.ApplicationPath));

                // Delete results emitted by compiler
                DeleteCompilerResult(result, false);

                // And then reassert pre-compilation conditions, assuring
                // the compiler has left our stuff alone
                Assert.True(File.Exists(inputFile));
                Assert.AreEqual(1, Directory.GetFiles(targetDir).Length);
            }
            finally
            {
                DeleteCompilerResult(result, false);
            }
        }

        string TestInputFile(string name)
        {
            return $@"TestInputs\Starcounter.Server.Test\{name}";
        }

        void DeleteCompilerResult(AppCompilerResult result, bool deleteDirectory = true)
        {
            File.Delete(result.ApplicationPath);
            File.Delete(result.SymbolFilePath);
            if (deleteDirectory)
            {
                Directory.Delete(result.OutputDirectory);
            }
        }
    }
}