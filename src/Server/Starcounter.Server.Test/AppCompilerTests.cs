
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

        void DeleteCompilerResult(AppCompilerResult result)
        {
            File.Delete(result.ApplicationPath);
            File.Delete(result.SymbolFilePath);
            Directory.Delete(result.OutputDirectory);
        }
    }
}