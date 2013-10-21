
using System;
using System.Collections.Generic;
using NUnit.Framework;
using Starcounter.Advanced;
using Starcounter.Templates;
using TJson = Starcounter.Templates.TObject;


namespace Starcounter.Internal.XSON.Tests {

    public class BindingTests {
		[Test]
		public static void TestPathBindings() {
			Person person = new Person() { FirstName = "Arne", LastName = "Anka" };
			person.Address = new Address() { Street = "Nybrogatan" }; 
			Company company = new Company() { Person = person };
			
			Person person2 = new Person() { Address = null };
			Company company2 = new Company() { Person = person2 };

			var jsonTemplate = new TObject();
			var streetTemplate = jsonTemplate.Add<TString>("Street", "Person.Address.Street");
			streetTemplate.BindingStrategy = BindingStrategy.Bound;

			var firstNameTemplate = jsonTemplate.Add<TString>("FirstName", "Person.FirstName");
			firstNameTemplate.BindingStrategy = BindingStrategy.Bound;

			dynamic json = (Json)jsonTemplate.CreateInstance();
			json.Data = company;
			Assert.AreEqual(person.Address.Street, json.Street);
			Assert.AreEqual(person.FirstName, json.FirstName);

			json.Street = "Härjedalsvägen";
			json.FirstName = "Nisse";
			Assert.AreEqual("Härjedalsvägen", person.Address.Street);
			Assert.AreEqual("Nisse", person.FirstName);

			json.Data = company2;
			Assert.DoesNotThrow(() => {
				string value = json.Street;
				json.Street = "Härjedalsvägen";

				value = json.FirstName;
				json.FirstName = "Nisse";
			});
		}

        [Test]
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
            var t = new TJson();
            var prop = t.Add<TString>("FirstName");
            prop.Bind = "FirstName";
            prop.BindingStrategy = BindingStrategy.Bound;
            j.Template = t;
            j.Data = p;

            var temp = (Json)j;

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
			var t = new TJson();
			var prop = t.Add<TString>("FirstName");
			prop.BindingStrategy = BindingStrategy.Auto;

			var noteProp = t.Add<TString>("Notes");
			noteProp.BindingStrategy = BindingStrategy.Bound;

			j.Template = t;
			j.Data = p;

			Assert.Throws(typeof(Exception), () => { string notes = j.Notes; });
			noteProp.BindingStrategy = BindingStrategy.Unbound;
			Assert.DoesNotThrow(() => { string notes = j.Notes; });

			Assert.AreEqual("Joachim", p.FirstName);
			Assert.AreEqual("Joachim", j.FirstName);

			j.FirstName = "Douglas";
			Assert.AreEqual("Douglas", j.FirstName);
			Assert.AreEqual("Douglas", p.FirstName);
		}

		[Test]
		public static void TestArrayBinding() {
			Recursive r = new Recursive() { Name = "One" };
			Recursive r2 = new Recursive() { Name = "Two" };
			Recursive r3 = new Recursive() { Name = "Three" };

			r.Recursives.Add(r2);
			r2.Recursives.Add(r3);

			var mainTemplate = new TJson();

			var arrItemTemplate1 = new TJson();
			arrItemTemplate1.Add<TString>("Name");

			var arrItemTemplate2 = new TJson();
			arrItemTemplate2.Add<TString>("Name");

			var objArrTemplate = arrItemTemplate1.Add<TObjArr>("Recursives");
			objArrTemplate.ElementType = arrItemTemplate2;

			objArrTemplate = mainTemplate.Add<TObjArr>("Recursives");
			objArrTemplate.ElementType = arrItemTemplate1;

			var json = new Json();
			json.Template = mainTemplate;
			json.Data = r;
		}
    }
}
