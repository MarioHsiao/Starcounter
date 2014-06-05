using System;
using NUnit.Framework;

namespace Starcounter.Internal.Tests {
    [TestFixture]
    public class StringTest {
        [Test]
        public void TestReverseFullName() {
            string aClassName = "Starcounter.Metadata.BaseType";
            Assert.AreEqual("BaseType.Metadata.Starcounter", aClassName.ReverseOrderDotWords());
            aClassName = "FirstName.SecondName";
            Assert.AreEqual("SecondName.FirstName", aClassName.ReverseOrderDotWords());
            aClassName = "TableName";
            Assert.AreEqual(aClassName, aClassName.ReverseOrderDotWords());
            aClassName = "";
            Assert.AreEqual(aClassName, aClassName.ReverseOrderDotWords());
        }

        [Test]
        public void TestReverseFirstName() {
            string aClassName = "Starcounter.Metadata.BaseType";
            Assert.AreEqual("BaseType", aClassName.LastDotWord());
            aClassName = "FirstName.SecondName";
            Assert.AreEqual("SecondName", aClassName.LastDotWord());
            aClassName = "TableName";
            Assert.AreEqual(aClassName, aClassName.LastDotWord());
            aClassName = "";
            Assert.AreEqual(aClassName, aClassName.LastDotWord());
        }
    }
}
