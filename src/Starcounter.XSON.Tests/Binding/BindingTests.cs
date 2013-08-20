
using System;
using NUnit.Framework;
using Starcounter.Templates;
namespace Starcounter.Internal.XSON.Tests {

    public class BindingTests {


        //[Test] // Awaiting Christians push of autobinding as default
        public static void TestDefaultAutoBinding() {
            Person p = new Person();
            p.FirstName = "Albert";
            dynamic j = new Json();
            j.Data = p;
            j.FirstName = "Abbe";
            Assert.AreEqual("Abbe", j.FirstName);
            Assert.AreEqual("Abbe", p.FirstName);
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
            prop.Bound = Bound.Yes;
            j.Template = t;
            j.Data = p;

            Json temp = (Json)j;

            Assert.AreEqual("Joachim", p.FirstName); // Get firstname using data object
            Assert.AreEqual("Joachim", temp.Get(prop)); // Get firstname using JSON data binding using API
            Assert.AreEqual("Joachim", j.FirstName); // Get firstname using JSON data binding using dynamic code-gen

            j.FirstName = "Douglas";
            Assert.AreEqual("Douglas", p.FirstName);
            Assert.AreEqual("Douglas", j.FirstName);
        }

		[Test]
		public static void TestAutoBinding() {
			var p = new Person();
			p.FirstName = "Joachim";
			p.LastName = "Wester";

			dynamic j = new Json();
			TJson t = new TJson();
			var prop = t.Add<TString>("FirstName");
			prop.Bound = Bound.Auto;

			var noteProp = t.Add<TString>("Notes");
			noteProp.Bound = Bound.Yes;

			j.Template = t;
			j.Data = p;

			Assert.Throws(typeof(Exception), () => { string notes = j.Notes; });
			noteProp.Bound = Bound.Auto;
			Assert.DoesNotThrow(() => { string notes = j.Notes; });

			Assert.AreEqual("Joachim", p.FirstName);
			Assert.AreEqual("Joachim", j.FirstName);

			j.FirstName = "Douglas";
			Assert.AreEqual("Douglas", j.FirstName);
			Assert.AreEqual("Douglas", p.FirstName);
		}
    }
}
