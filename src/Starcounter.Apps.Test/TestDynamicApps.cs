// ***********************************************************************
// <copyright file="TestDynamicApps.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System.Collections.Generic;
using NUnit.Framework;
using Starcounter.Templates;
using Starcounter.Advanced;
using System;
using Starcounter.XSON.CodeGeneration;

namespace Starcounter.Client.Tests.Application {

    /// <summary>
    /// Class AppTests
    /// </summary>
    [TestFixture]
    public class AppTests {
        [TestFixtureSetUp]
        public static void Setup() {
            Obj.Factory = new TypedJsonFactory();
        }

        /// <summary>
        /// Creates a template (schema) and Puppets using that schema in code.
        /// </summary>
        /// <remarks>
        /// Template schemas can be created on the fly in Starcounter using the API. It is not necessary
        /// to have compile time or run time .json files to create template schemas.
        /// </remarks>
        [Test]
        public static void CreateTemplatesAndAppsByCode() {
            _CreateTemplatesAndAppsByCode();
        }

        /// <summary>
        /// Creates some.
        /// </summary>
        /// <returns>List{App}.</returns>
        private static List<Json> _CreateTemplatesAndAppsByCode() {

            // First, let's create the schema (template)
            var personSchema = new TJson();
            var firstName = personSchema.Add<TString>("FirstName$");
            var lastName = personSchema.Add<TString>("LastName");
            var age = personSchema.Add<TLong>("Age");

            var phoneNumber = new TJson();
            var phoneNumbers = personSchema.Add<TArr<Json,TJson>>("Phonenumbers", phoneNumber);
            var number = phoneNumber.Add<TString>("Number");

            Assert.AreEqual("FirstName$", firstName.TemplateName);
            Assert.AreEqual("FirstName", firstName.PropertyName);

            // Now let's create instances using that schema
            Json jocke = new Json() { Template = personSchema };
            Json tim = new Json() { Template = personSchema };

            jocke.Set(firstName, "Joachim");
            jocke.Set(lastName, "Wester");
            jocke.Set(age, 30);

            tim.Set(firstName, "Timothy");
            tim.Set(lastName, "Wester");
            tim.Set(age, 16);

            Assert.AreEqual(0, firstName.TemplateIndex);
            Assert.AreEqual(1, lastName.TemplateIndex);
            Assert.AreEqual(2, age.TemplateIndex);
            Assert.AreEqual(3, phoneNumbers.TemplateIndex);
            Assert.AreEqual(0, number.TemplateIndex);

            Assert.AreEqual("Joachim", jocke.Get(firstName));
            Assert.AreEqual("Wester", jocke.Get(lastName));
            Assert.AreEqual("Timothy", tim.Get(firstName));
            Assert.AreEqual("Wester", tim.Get(lastName));

            var ret = new List<Json>();
            ret.Add(jocke);
            ret.Add(tim);
            return ret;
        }
         
        [Test]
        public static void TestDynamic() {
            // First, let's create the schema (template)
            var personSchema = new TJson();
            var firstName = personSchema.Add<TString>("FirstName$");
            var lastName = personSchema.Add<TString>("LastName");
            var age = personSchema.Add<TLong>("Age");

            var phoneNumber = new TJson();
            var phoneNumbers = personSchema.Add<TArr<Json, TJson>>("Phonenumbers", phoneNumber);
            var number = phoneNumber.Add<TString>("Number");

            Assert.AreEqual("FirstName$", firstName.TemplateName);
            Assert.AreEqual("FirstName", firstName.PropertyName);

            // Now let's create instances using that schema
            dynamic jocke = new Json() { Template = personSchema };
            dynamic tim = new Json() { Template = personSchema };

            jocke.FirstName = "Joachim";
            jocke.LastName = "Wester";
            jocke.Age = 30;

            tim.FirstName = "Timothy";
            tim.LastName = "Wester";
            tim.Age = 16;

            Assert.AreEqual(0, firstName.TemplateIndex);
            Assert.AreEqual(1, lastName.TemplateIndex);
            Assert.AreEqual(2, age.TemplateIndex);
            Assert.AreEqual(3, phoneNumbers.TemplateIndex);
            Assert.AreEqual(0, number.TemplateIndex);

            Assert.AreEqual("Joachim", jocke.FirstName);
            Assert.AreEqual("Wester", jocke.LastName);
            Assert.AreEqual("Timothy", tim.FirstName);
            Assert.AreEqual("Wester", tim.LastName);

            var ret = new List<Json>();
            ret.Add(jocke);
            ret.Add(tim);
        }

        /// <summary>
        /// Test using dynamic codegen
        /// </summary>
        [Test]
        public static void ReadDynamic() {
            return;

            //List<App> apps = CreateSome();
            //dynamic jocke = apps[0];
            //dynamic tim = apps[1];

            //Assert.AreEqual("Joachim", jocke.FirstName);
            //Assert.AreEqual("Wester", jocke.LastName);
            //Assert.AreEqual(30, jocke.Age);

            //Assert.AreEqual("Timothy", tim.FirstName);
            //Assert.AreEqual("Wester", tim.LastName);
            //Assert.AreEqual(16, tim.Age);
        }

        /// <summary>
        /// Writes the dynamic.
        /// </summary>
        [Test]
        public static void WriteDynamic() {
            return;
            //List<App> apps = CreateSome();
            //dynamic a = apps[0];
            //dynamic b = apps[1];
            //dynamic c = new App() { Template = b.Template };

            //a.FirstName = "Adrienne";
            //a.LastName = "Wester";
            //a.Age = 24;

            //b.FirstName = "Douglas";
            //b.LastName = "Wester";
            //b.Age = 7;

            //c.FirstName = "Charlie";
            //c.LastName = "Wester";
            //c.Age = 4;

            //Assert.AreEqual("Adrienne", a.FirstName);
            //Assert.AreEqual("Wester", a.LastName);
            //Assert.AreEqual(24, a.Age);

            //Assert.AreEqual("Douglas", b.FirstName);
            //Assert.AreEqual("Wester", b.LastName);
            //Assert.AreEqual(7, b.Age);

            //Assert.AreEqual("Charlie", c.FirstName);
            //Assert.AreEqual("Wester", c.LastName);
            //Assert.AreEqual(4, c.Age);
        }

        [Test]
        public static void TestSlowSerializeSimpleDynamicApp() {
            TObj personSchema = CreateSimplePersonTemplate();


            dynamic p1 = new Json() { Template = personSchema };
            p1.FirstName = "Allan";
            p1.LastName = "Ballan";
            p1.Age = 19;

            dynamic n = p1.PhoneNumbers.Add();
            n.Number = "123-555-7890";

            string expectedJson = "{\"FirstName$\":\"Allan\",\"LastName\":\"Ballan\",\"Age\":19,\"PhoneNumbers\":[{\"Number\":\"123-555-7890\"}]}";
            string json = p1.ToJson();

            Assert.AreEqual(expectedJson, json);
        }

        [Test]
        public static void TestSlowSerializeComplexDynamicApp() {
            TObj personSchema = CreateComplexPersonTemplate();

            dynamic p1 = new Json() { Template = personSchema };
            p1.FirstName = "Allan";
            p1.LastName = "Ballan";
            p1.Age = 19;
            p1.Stats = 39.4567m;

            dynamic n1 = p1.Fields.Add();
            n1.Type = "Phone";
            n1.Info.Text = "123-555-7890";

            dynamic n2 = p1.Fields.Add();
            n2.Type = "Email";
            n2.Info.Text = "allanballan@gmail.com";

            p1.ExtraInfo.Text = "Hi ha ho he";

            Assert.IsInstanceOf<MyFieldMessage>(n1);
            Assert.IsInstanceOf<MyFieldMessage>(n2);

            string expectedJson = "{\"FirstName$\":\"Allan\",\"LastName\":\"Ballan\",\"Age\":19,\"Stats\":39.4567,\"Fields\":[{\"Type\":\"Phone\",\"Info\":{\"Text\":\"123-555-7890\"}},{\"Type\":\"Email\",\"Info\":{\"Text\":\"allanballan@gmail.com\"}}],\"ExtraInfo\":{\"Text\":\"Hi ha ho he\"}}";
            string json = p1.ToJson();

            Assert.AreEqual(expectedJson, json);
        }

        [Test]
        public static void TestSlowDeserializeSimpleDynamicApp() {
            TObj personSchema = CreateSimplePersonTemplate();
            string json = "{\"FirstName$\":\"Allan\",\"LastName\":\"Ballan\",\"Age\":19,\"PhoneNumbers\":[{\"Number\":\"123-555-7890\"}]}";

            dynamic p1 = new Json() { Template = personSchema };
            p1.PopulateFromJson(json);

            Assert.AreEqual("Allan", p1.FirstName);
            Assert.AreEqual("Ballan", p1.LastName);
            Assert.AreEqual(19, p1.Age);
            Assert.AreEqual(1, p1.PhoneNumbers.Count);
            Assert.AreEqual("123-555-7890", p1.PhoneNumbers[0].Number);
        }

        [Test]
        public static void TestSlowDeserializeComplexDynamicApp() {
            TObj personSchema = CreateComplexPersonTemplate();
            string json = "{\"FirstName$\":\"Allan\",\"LastName\":\"Ballan\",\"Age\":19,\"Stats\":39.4567,\"Fields\":[{\"Type\":\"Phone\",\"Info\":{\"Text\":\"123-555-7890\"}},{\"Type\":\"Email\",\"Info\":{\"Text\":\"allanballan@gmail.com\"}}],\"ExtraInfo\":{\"Text\":\"Hi ha ho he\"}}";

            dynamic p1 = new Json() { Template = personSchema };
            p1.PopulateFromJson(json);

            Assert.AreEqual("Allan", p1.FirstName);
            Assert.AreEqual("Ballan", p1.LastName);
            Assert.AreEqual(19, p1.Age);
            Assert.AreEqual(39.4567m, p1.Stats);

            Assert.AreEqual(2, p1.Fields.Count);

            var field = p1.Fields[0];
            Assert.IsInstanceOf<MyFieldMessage>(field);
            Assert.AreEqual("Phone", field.Type);
            Assert.AreEqual("123-555-7890", field.Info.Text);

            field = p1.Fields[1];
            Assert.IsInstanceOf<MyFieldMessage>(field);
            Assert.AreEqual("Email", field.Type);
            Assert.AreEqual("allanballan@gmail.com", field.Info.Text);

            Assert.AreEqual("Hi ha ho he", p1.ExtraInfo.Text);
        }

        [Test]
        public static void TestDataBinding() {
            StarcounterBase._DB = new FakeDbImpl();

            dynamic msg = new Json<PersonObject> { Template = CreateSimplePersonTemplateWithDataBinding() };

            var myDataObj = new PersonObject() { FirstName = "Kalle", Surname = "Kula", Age = 21, Misc = "Lorem Ipsum" };
            myDataObj.Number = new PhoneNumberObject() { Number = "123-555-7890" };
            msg.Data = myDataObj;

            msg.Updated = "2013-04-25 13:15:12";

            // Reading bound values.
            Assert.AreEqual("Kalle", msg.FirstName);
            Assert.AreEqual("Kula", msg.LastName);
            Assert.AreEqual(0, msg._Age); // Since Age shouldn't be bound we should get a zero back and not 21.
            Assert.IsNotNullOrEmpty(msg.Created);
            Assert.IsNullOrEmpty(msg.Misc); // Same as above. Not bound so no value should be retrieved.
            Assert.AreEqual("123-555-7890", msg._PhoneNumber.Number); // Should be bound even if the name start with '_' since we specify a binding when registering the template.

            // Setting bound values.
            msg.FirstName = "Allan";
            msg.LastName = "Ballan";
            msg._Age = 109L;
            msg._PhoneNumber.Number = "666";
            msg.Misc = "Changed!";
             
            // Check dataobject is changed.
            Assert.AreEqual("Allan", myDataObj.FirstName);
            Assert.AreEqual("Ballan", myDataObj.Surname);
            Assert.AreEqual(21, myDataObj.Age); // Age is not bound so updating the message should not alter the dataobject.
            Assert.AreEqual("666", myDataObj.Number.Number);
            Assert.AreEqual("Lorem Ipsum", myDataObj.Misc); // Not bound so updating the message should not alter the dataobject.
        }

        private static TJson CreateSimplePersonTemplateWithDataBinding() {
            var personSchema = new TJson() { BindChildren = true };
            personSchema.Add<TString>("FirstName$"); // Bound to FirstName
            personSchema.Add<TString>("LastName", "Surname"); // Bound to Surname
            personSchema.Add<TLong>("_Age"); // Will not be bound
            personSchema.Add<TString>("Created");
            personSchema.Add<TString>("Updated");
            
            var misc = personSchema.Add<TString>("Misc");
            misc.Bind = null; // Removing the binding for this specific template.
           
            var phoneNumber = personSchema.Add<TJson>("_PhoneNumber", "Number"); // Bound to Number even though name start with '_'
            phoneNumber.BindChildren = true;
            phoneNumber.Add<TString>("Number"); // Bound to Number
            
            return personSchema;
        }

        private static TObj CreateSimplePersonTemplate() {
            var personSchema = new TJson();
            personSchema.Add<TString>("FirstName$");
            personSchema.Add<TString>("LastName");
            personSchema.Add<TLong>("Age");
            
            var phoneNumber = new TJson();
            phoneNumber.Add<TString>("Number");
            personSchema.Add<TArr<Json,TJson>>("PhoneNumbers", phoneNumber);

            return personSchema;
        }

        private static TObj CreateComplexPersonTemplate() {
            var personSchema = new TJson();
            personSchema.Add<TString>("FirstName$");
            personSchema.Add<TString>("LastName");
            personSchema.Add<TLong>("Age");
            personSchema.Add<TDecimal>("Stats");

            var field = new TJson();
            field.Add<TString>("Type");
            var info = field.Add<TJson>("Info");
            info.Add<TString>("Text");
            field.InstanceType = typeof(MyFieldMessage);
            personSchema.Add<TArr<MyFieldMessage, TJson>>("Fields", field);

            var extraInfo = personSchema.Add<TJson>("ExtraInfo");
            extraInfo.Add<TString>("Text");

            return personSchema;
        }
    }

    internal class MyFieldMessage : Json {
    }

    internal class PersonObject : BasePerson {
        public int Age { get; set; }

        public PhoneNumberObject Number { get; set; }
        public string Misc { get; set; }
    }

    internal class PhoneNumberObject : IBindable {
        public ulong Identity {
            get { return 0; }
        }

        public string Number { get; set; }
    }

    internal class BasePerson : IBindable {
        public BasePerson() {
            Created = DateTime.Now;
        }

        public ulong Identity {
            get { return 0; }
        }

        public string FirstName { get; set; }
        public string Surname { get; set; }
        public DateTime Created { get; private set; }
        public DateTime Updated { get; set; }
    }
}
