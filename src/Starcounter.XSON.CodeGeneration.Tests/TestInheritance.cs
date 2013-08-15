

using NUnit.Framework;
using System;
namespace Starcounter.Internal.XSON.Tests {

    [TestFixture]
    public static class TestInheritance {

        [Test]
        public static void TestClassNames() {
            var code = PartialClassGenerator.GenerateTypedJsonCode(
                "ThreadPage.json",
                "ThreadPage.json.cs", false );
            Console.WriteLine( code );
        }
    }
}
