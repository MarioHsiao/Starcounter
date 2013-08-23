
using NUnit.Framework;
using Starcounter.Templates;
using System;
using System.Collections.Generic;
using TJson = Starcounter.Templates.Schema<Starcounter.Json<object>>;


namespace Starcounter.Internal.XSON.Tests {

    [TestFixture]
    static class ExpandoTests {


        [Test]
        public static void TestDynamicJson() {

            dynamic j = new Json<object>();
            dynamic nicke = new Json<object>();
            nicke.FirstName = "Nicke";

            j.FirstName = "Joachim";
            j.Age = 43;
            j.Length = 184.7;            
            j.Friends = new List<Json<object>>() { nicke };

            Assert.AreEqual("Joachim", j.FirstName);
            Assert.AreEqual(43, j.Age);
            Assert.AreEqual("Nicke",j.Friends[0].FirstName);
        }


        [Test]
        public static void TestDynamicJsonTemplateProtection() {

            dynamic j = new Json<object>();
            dynamic nicke = new Json<object>();
            dynamic olle = new Json<object>();

            j.FirstName = "Joachim";

            Assert.Throws(typeof(Exception), () => { nicke.Template = j.Template; });
            Assert.DoesNotThrow(() => olle.Template = new TJson());

        }


//        [Test]
        public static void TestDynamicJsonBinding() {
            var p = new Person();
            p.FirstName = "Joachim";
            p.LastName = "Wester";

            dynamic j = new Json<object>();
            j.Data = p;

            Assert.AreEqual("Joachim", p.FirstName);
            Assert.AreEqual("Joachim", j.FirstName);
        }
    }
}
