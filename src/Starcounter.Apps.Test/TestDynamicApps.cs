// ***********************************************************************
// <copyright file="TestDynamicApps.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System.Collections.Generic;
using NUnit.Framework;
using Starcounter.Client;
using Starcounter.Templates.Interfaces;
using Starcounter.Templates;

namespace Starcounter.Client.Tests.Application {

    /// <summary>
    /// Class AppTests
    /// </summary>
    [TestFixture]
    public class AppTests {

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
        private static List<Puppet> _CreateTemplatesAndAppsByCode() {

            // First, let's create the schema (template)
            var personSchema = new TPuppet();
            var firstName = personSchema.Add<TString>("FirstName$");
            var lastName = personSchema.Add<TString>("LastName");
            var age = personSchema.Add<TLong>("Age");

            var phoneNumber = new TPuppet();
            var phoneNumbers = personSchema.Add<TArr<Puppet,TPuppet>>("Phonenumbers", phoneNumber);
            var number = phoneNumber.Add<TString>("Number");

            Assert.AreEqual("FirstName$", firstName.Name);
            Assert.AreEqual("FirstName", firstName.PropertyName);

            // Now let's create instances using that schema
            Puppet jocke = new Puppet() { Template = personSchema };
            Puppet tim = new Puppet() { Template = personSchema };

            jocke.SetValue(firstName, "Joachim");
            jocke.SetValue(lastName, "Wester");
            jocke.SetValue(age, 30);

            tim.SetValue(firstName, "Timothy");
            tim.SetValue(lastName, "Wester");
            tim.SetValue(age, 16);

            Assert.AreEqual(0, firstName.Index);
            Assert.AreEqual(1, lastName.Index);
            Assert.AreEqual(2, age.Index);
            Assert.AreEqual(3, phoneNumbers.Index);
            Assert.AreEqual(0, number.Index);

            Assert.AreEqual("Joachim", jocke.GetValue(firstName));
            Assert.AreEqual("Wester", jocke.GetValue(lastName));
            Assert.AreEqual("Timothy", tim.GetValue(firstName));
            Assert.AreEqual("Wester", tim.GetValue(lastName));

            var ret = new List<Puppet>();
            ret.Add(jocke);
            ret.Add(tim);
            return ret;
        }

        [Test]
        public static void TestDynamic() {
            // First, let's create the schema (template)
            var personSchema = new TPuppet();
            var firstName = personSchema.Add<TString>("FirstName$");
            var lastName = personSchema.Add<TString>("LastName");
            var age = personSchema.Add<TLong>("Age");

            var phoneNumber = new TPuppet();
            var phoneNumbers = personSchema.Add<TArr<Puppet, TPuppet>>("Phonenumbers", phoneNumber);
            var number = phoneNumber.Add<TString>("Number");

            Assert.AreEqual("FirstName$", firstName.Name);
            Assert.AreEqual("FirstName", firstName.PropertyName);

            // Now let's create instances using that schema
            dynamic jocke = new Puppet() { Template = personSchema };
            dynamic tim = new Puppet() { Template = personSchema };

            jocke.FirstName = "Joachim";
            jocke.LastName = "Wester";
            //jocke.Age = 30;

            tim.FirstName = "Timothy";
            tim.LastName = "Wester";
            //tim.Age = 16;

            Assert.AreEqual(0, firstName.Index);
            Assert.AreEqual(1, lastName.Index);
            Assert.AreEqual(2, age.Index);
            Assert.AreEqual(3, phoneNumbers.Index);
            Assert.AreEqual(0, number.Index);

            Assert.AreEqual("Joachim", jocke.GetValue(firstName));
            Assert.AreEqual("Wester", jocke.GetValue(lastName));
            Assert.AreEqual("Timothy", tim.GetValue(firstName));
            Assert.AreEqual("Wester", tim.GetValue(lastName));

            var ret = new List<Puppet>();
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

            dynamic p1 = new Message() { Template = personSchema };
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

            dynamic p1 = new Message() { Template = personSchema };
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

            dynamic p1 = new Message() { Template = personSchema };
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

            dynamic p1 = new Message() { Template = personSchema };
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

        private static TObj CreateSimplePersonTemplate() {
            var personSchema = new TMessage();
            personSchema.Add<TString>("FirstName$");
            personSchema.Add<TString>("LastName");
            personSchema.Add<TLong>("Age");

            var phoneNumber = new TMessage();
            phoneNumber.Add<TString>("Number");
            personSchema.Add<TArr<Message, TMessage>>("PhoneNumbers", phoneNumber);

            return personSchema;
        }

        private static TObj CreateComplexPersonTemplate() {
            var personSchema = new TMessage();
            personSchema.Add<TString>("FirstName$");
            personSchema.Add<TString>("LastName");
            personSchema.Add<TLong>("Age");
            personSchema.Add<TDecimal>("Stats");

            var field = new TMessage();
            field.Add<TString>("Type");
            var info = field.Add<TMessage>("Info");
            info.Add<TString>("Text");
            field.InstanceType = typeof(MyFieldMessage);
            personSchema.Add<TArr<MyFieldMessage, TMessage>>("Fields", field);

            var extraInfo = personSchema.Add<TMessage>("ExtraInfo");
            extraInfo.Add<TString>("Text");

            return personSchema;
        }
    }

    internal class MyFieldMessage : Message {
    }
}
