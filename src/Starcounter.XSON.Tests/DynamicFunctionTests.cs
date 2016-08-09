
using NUnit.Framework;
using Starcounter.Templates;
using System;
using System.Collections.Generic;
using TJson = Starcounter.Templates.TObject;


namespace Starcounter.Internal.XSON.Tests {

    [TestFixture]
    static class DynamicFunctionTests {


        [Test]
        public static void TestDynamicJson() {

            dynamic j = new Json();
            dynamic nicke = new Json();
            nicke.FirstName = "Nicke";

            j.FirstName = "Joachim";
            j.Age = 43;
            j.Length = 184.7;            
            j.Friends = new List<Json>() { nicke };

            Assert.AreEqual("Joachim", j.FirstName);
            Assert.AreEqual(43, j.Age);
            Assert.AreEqual("Nicke",j.Friends[0].FirstName);
        }


        [Test]
        public static void TestDynamicJsonTemplateProtection() {

            dynamic j = new Json();
            dynamic nicke = new Json();
            dynamic olle = new Json();

            j.FirstName = "Joachim";

            Assert.Throws(typeof(Exception), () => { nicke.Template = j.Template; });
            Assert.DoesNotThrow(() => olle.Template = new TJson());

        }


//        [Test]
        public static void TestDynamicJsonBinding() {
            var p = new Person();
            p.FirstName = "Joachim";
            p.LastName = "Wester";

            dynamic j = new Json();
            j.Data = p;

            Assert.AreEqual("Joachim", p.FirstName);
            Assert.AreEqual("Joachim", j.FirstName);
        }
    }
}
