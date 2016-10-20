
using NUnit.Framework;
using Starcounter.Server.Compiler;

namespace Starcounter.Server.Test
{
    public class AppCompilerTests {

        [Test]
        public void CompileNoSource() {
            var c = new AppCompiler("app").WithDefaultReferences();
            Assert.IsNotEmpty(c.MetadataReferences);
            var e = Assert.Throws<AppCompilerException>(() => c.Compile());
            Assert.True(e.Error == AppCompilerError.NoSourceSpecified);
        }
    }
}