
using NUnit.Framework;
using System;

namespace Starcounter.Weaver.Test
{
    public class WeaverTests
    {
        [Test]
        public void WeaverWithNoHostShouldRaiseException()
        {
            var e = Assert.Throws<ArgumentNullException>(() => new CodeWeaver(
                null, 
                Environment.CurrentDirectory, 
                "Ignored.cs", 
                Environment.CurrentDirectory, 
                Environment.CurrentDirectory));
            Assert.AreEqual(e.ParamName.ToLowerInvariant(), "host");
        }
    }
}
