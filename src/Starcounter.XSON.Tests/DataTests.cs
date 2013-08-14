
using NUnit.Framework;
using Starcounter.Templates;
using System;
using System.Collections.Generic;
namespace Starcounter.Internal.XSON.Tests {

    [TestFixture]
    static class DataTests {


        [Test]
        public static void TestDynamicJson() {

            dynamic j = new Json();
            dynamic nicke = new Json();
            nicke.FirstName = "Nicke";

            j.FirstName = "Joachim";
            j.Age = 43;
            j.Length = 184.7;            
            j.Friends = new List<Obj>() { nicke };

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
            Assert.DoesNotThrow(() => olle.Template = new TObj());

        }

        [Test]
        public static void TestSimpleBinding() {
            var p = new Person();
            p.FirstName = "Joachim";
            p.LastName = "Wester";

            dynamic j = new Json();
            TJson t = new TJson();
            var prop = t.Add<TString>("FirstName");
            prop.Bind = "FirstName";
            prop.Bound = true;
            j.Template = t;
            j.Data = p;

            Assert.AreEqual("Joachim", p.FirstName);
            Assert.AreEqual("Joachim", j.FirstName);

            j.FirstName = "Douglas";
            Assert.AreEqual("Douglas", j.FirstName);
            Assert.AreEqual("Douglas", p.FirstName);
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
