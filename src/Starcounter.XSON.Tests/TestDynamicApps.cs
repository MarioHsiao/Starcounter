// ***********************************************************************
// <copyright file="TestDynamicApps.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System.Collections.Generic;
using NUnit.Framework;
using Starcounter.Templates;
using System;
using Starcounter.XSON.CodeGeneration;
using Starcounter.Advanced;
using Starcounter.Internal;

namespace Starcounter.XSON.Tests {

    /// <summary>
    /// Class AppTests
    /// </summary>
    [TestFixture]
    public class AppTests {
        /// <summary>
        /// Sets up the test.
        /// </summary>
        [TestFixtureSetUp]
        public static void Setup() {

            DataBindingFactory.ThrowExceptionOnBindindRecreation = true;
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
        
        /// <summary>
        /// Tests dynamic.
        /// </summary>
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

        /// <summary>
        /// Tests TestCorrectJsonInstances.
        /// </summary>
        [Test]
        public static void TestCorrectJsonInstances() {
            TObj personSchema = CreateComplexPersonTemplate();

            dynamic p1 = personSchema.CreateInstance();
            dynamic n1 = p1.Fields.Add();
            dynamic n2 = p1.Fields.Add();

            Assert.IsInstanceOf<Obj>(p1);
            Assert.IsInstanceOf<MyFieldMessage>(n1);
            Assert.IsInstanceOf<MyFieldMessage>(n2);

        }

        /// <summary>
        /// Tests TestDataBinding.
        /// </summary>
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

        /// <summary>
        /// Tests TestDataBinding.
        /// </summary>
        [Test]
        public static void TestDataBindingWithDifferentClasses() {
            StarcounterBase._DB = new FakeDbImpl();

            // Bound to SimpleBase datatype.
            TObj tSimple = Obj.Factory.CreateJsonTemplateFromFile("simple.json");
            dynamic simpleJson = tSimple.CreateInstance();

            var simpleData = new SubClass1(); 
            simpleJson.Data = simpleData;
            simpleJson.BaseValue = "SubClass1";
            simpleJson.AbstractValue = "SubClass1";
            string virtualValue = simpleJson.VirtualValue;

            Assert.AreEqual("SubClass1", simpleData.BaseValue);
            Assert.AreEqual("SubClass1", simpleData.AbstractValue);
            Assert.AreEqual("SubClass1", virtualValue);

            var simpleData2 = new SubClass2();
            Assert.DoesNotThrow(() => { simpleJson.Data = simpleData2; });
            Assert.DoesNotThrow(() => { simpleJson.BaseValue = "SubClass2"; });
            Assert.DoesNotThrow(() => { simpleJson.AbstractValue = "SubClass2"; });
            virtualValue = simpleJson.VirtualValue;

            Assert.AreEqual("SubClass2", simpleData2.BaseValue);
            Assert.AreEqual("SubClass2", simpleData2.AbstractValue);
            Assert.AreEqual("SubClass2", virtualValue);

            var simpleData3 = new SubClass3();
            Assert.DoesNotThrow(() => { simpleJson.Data = simpleData3; });
            Assert.DoesNotThrow(() => { simpleJson.BaseValue = "SubClass3"; });
            Assert.DoesNotThrow(() => { simpleJson.AbstractValue = "SubClass3"; });
            virtualValue = simpleJson.VirtualValue;

            Assert.AreEqual("SubClass3", simpleData3.BaseValue);
            Assert.AreEqual("SubClass3", simpleData3.AbstractValue);
            Assert.AreEqual("SubClass3", virtualValue);
        }

        private static TJson CreateSimplePersonTemplateWithDataBinding() {
            var personSchema = new TJson() { BindChildren = true };
            personSchema.Add<TString>("FirstName$"); // Bound to FirstName
            personSchema.Add<TString>("LastName", "Surname"); // Bound to Surname
            personSchema.Add<TLong>("_Age"); // Will not be bound
            personSchema.Add<TString>("Created");
            personSchema.Add<TString>("Updated");
            personSchema.Add<TString>("AbstractValue");
            personSchema.Add<TString>("VirtualValue");
            
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
}
