using System;
using NUnit.Framework;

namespace Starcounter.Internal.Tests {
    [TestFixture]
    public class StringTest {
        [Test]
        public void TestReverseFullName() {
            string aClassName = "Starcounter.Metadata.BaseType";
            Assert.AreEqual("BaseType.Metadata.Starcounter", aClassName.ReverseFullName());
            aClassName = "FirstName.SecondName";
            Assert.AreEqual("SecondName.FirstName", aClassName.ReverseFullName());
            aClassName = "TableName";
            Assert.AreEqual(aClassName, aClassName.ReverseFullName());
            aClassName = "";
            Assert.AreEqual(aClassName, aClassName.ReverseFullName());
        }

        [Test]
        public void TestReverseFirstName() {
            string aClassName = "Starcounter.Metadata.BaseType";
            Assert.AreEqual("Starcounter", aClassName.FirstName());
            aClassName = "FirstName.SecondName";
            Assert.AreEqual("FirstName", aClassName.FirstName());
            aClassName = "TableName";
            Assert.AreEqual(aClassName, aClassName.FirstName());
            aClassName = "";
            Assert.AreEqual(aClassName, aClassName.FirstName());
        }
    }
}
