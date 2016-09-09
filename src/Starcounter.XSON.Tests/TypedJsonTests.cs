// ***********************************************************************
// <copyright file="TestDynamicApps.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using NUnit.Framework;
using Starcounter.Advanced;
using Starcounter.Templates;
using Starcounter.XSON.Templates.Factory;
using Starcounter.XSON.Tests;
using CJ = Starcounter.Internal.XSON.Tests.CompiledJson;

namespace Starcounter.Internal.XSON.Tests {
    [TestFixture]
    public static class TypedJsonTests {
        [TestFixtureSetUp]
        public static void Setup() {
            StarcounterBase._DB = new FakeDbImpl();
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
            // First, let's create the schema (template)
            var personSchema = new TObject();
            var firstName = personSchema.Add<TString>("FirstName$");
            var lastName = personSchema.Add<TString>("LastName");
            var age = personSchema.Add<TLong>("Age");

            var phoneNumber = new TObject();
            var phoneNumbers = personSchema.Add<TArray<Json>>("Phonenumbers", phoneNumber);
            var number = phoneNumber.Add<TString>("Number");

            Assert.AreEqual("FirstName$", firstName.TemplateName);
            Assert.AreEqual("FirstName", firstName.PropertyName);

            // Now let's create instances using that schema
            var jocke = new Json() { Template = personSchema };
            var tim = new Json() { Template = personSchema };

            firstName.Setter(jocke, "Joachim");
            lastName.Setter(jocke, "Wester");
            age.Setter(jocke, 30);

            firstName.Setter(tim, "Timothy");
            lastName.Setter(tim, "Wester");
            age.Setter(tim, 16);

            Assert.AreEqual(0, firstName.TemplateIndex);
            Assert.AreEqual(1, lastName.TemplateIndex);
            Assert.AreEqual(2, age.TemplateIndex);
            Assert.AreEqual(3, phoneNumbers.TemplateIndex);
            Assert.AreEqual(0, number.TemplateIndex);

            Assert.AreEqual("Joachim", firstName.Getter(jocke));
            Assert.AreEqual("Wester", lastName.Getter(jocke));
            Assert.AreEqual("Timothy", firstName.Getter(tim));
            Assert.AreEqual("Wester", lastName.Getter(tim));
        }

        [Test]
        public static void TestJsonWithSingleValue() {
            string jsonStr = @"19";
            TLong template = Template.CreateFromMarkup(jsonStr) as TLong;

            Assert.IsNotNull(template);
            var json = (Json)template.CreateInstance();
            Assert.AreEqual(19, json.Get<long>(template));

            json.Set<long>(template, 666);
            Assert.AreEqual(666, json.Get<long>(template));

            jsonStr = json.ToJson();
        }

        [Test]
        public static void TestJsonWithSingleArray() {
            string jsonStr = @"[{""Apa"":""Papa""}]";
            TObjArr template = Template.CreateFromMarkup(jsonStr) as TObjArr;

            Assert.IsNotNull(template);
            var json = (Json)template.CreateInstance();

            jsonStr = json.ToJson();
        }

        [Test]
        public static void TestJsonWithSingleUntypedArray() {
            string jsonStr = @"[]";
            TObjArr template = Template.CreateFromMarkup(jsonStr) as TObjArr;

            Assert.IsNotNull(template);
            var json = (Json)template.CreateInstance();

            jsonStr = json.ToJson();
        }

        /// <summary>
        /// Tests dynamic.
        /// </summary>
        [Test]
        public static void TestRuntimeCreatedTemplate() {
            // First, let's create the schema (template)
            var personSchema = new TObject();
            var firstName = personSchema.Add<TString>("FirstName$");
            var lastName = personSchema.Add<TString>("LastName");
            var age = personSchema.Add<TLong>("Age");

            var phoneNumber = new TObject();
            var phoneNumbers = personSchema.Add<TArray<Json>>("Phonenumbers", phoneNumber);
            var number = phoneNumber.Add<TString>("Number");

            Assert.AreEqual("FirstName$", firstName.TemplateName);
            Assert.AreEqual("FirstName", firstName.PropertyName);

            // Now let's create instances using that schema
            dynamic jocke = new Json() { Template = personSchema };
            dynamic tim = new Json() { Template = personSchema };

            jocke.FirstName = "Joachim";
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

        [Test, Timeout(5000)]
        public static void BenchmarkDynamicJson() {
            dynamic json;
            int repeats = 100000;
            long value = 19;
            DateTime start;
            DateTime stop;

            TObject template = new TObject();
            template.Add<TLong>("Value");

            start = DateTime.Now;
            for (int i = 0; i < repeats; i++) {
                json = new Json() { Template = template };
                json.Value = value;
                value = json.Value;
            }
            stop = DateTime.Now;

            Helper.ConsoleWriteLine("Dynamic json get/set with " + repeats + " repeats in: " + (stop - start).TotalMilliseconds + " ms");
        }

        

        [Test]
        public static void TestNegativeNumberParsing() {
            var jsonStr = @"{""NegInt"":-2,""PosInt"":13,""NegDbl"":-2e2,""PosDbl"":1e5,""NegDec"":-3.5,""PosDec"":3.456}";
            var schema = TObject.CreateFromJson(jsonStr);

            dynamic json = schema.CreateInstance();

            Assert.AreEqual(-2L, json.NegInt);
            Assert.AreEqual(13L, json.PosInt);
            Assert.AreEqual(-2e2d, json.NegDbl);
            Assert.AreEqual(1e5d, json.PosDbl);
            Assert.AreEqual(-3.5m, json.NegDec);
            Assert.AreEqual(3.456m, json.PosDec);
        }
        
        [Test]
        public static void TestParentAssignmentWithDefaultSetter() {
            dynamic json = new Json();
            dynamic newPage = new Json();
            dynamic oldPage = new Json();

            json.Page = oldPage;
            Assert.IsNotNull(oldPage.Parent);
            Assert.AreNotEqual(-1, ((Json)oldPage).cacheIndexInArr);

            json.Page = newPage;
            Assert.IsNotNull(newPage.Parent);
            Assert.AreNotEqual(-1, ((Json)newPage).cacheIndexInArr);
            Assert.IsNull(oldPage.Parent);
            Assert.AreEqual(-1, ((Json)oldPage).cacheIndexInArr);
        }

        [Test]
        public static void TestParentAssignmentWithUnboundSetterForObject() {
            var schema = new TObject();
            var tpage = schema.Add<TObject>("Page");
            
            dynamic json = new Json() { Template = schema };
            dynamic newPage = new Json();
            dynamic oldPage = new Json();

            tpage.UnboundSetter(json, oldPage);
            Assert.IsNotNull(oldPage.Parent);
            Assert.AreNotEqual(-1, ((Json)oldPage).cacheIndexInArr);

            tpage.UnboundSetter(json, newPage);
            Assert.IsNotNull(newPage.Parent);
            Assert.AreNotEqual(-1, ((Json)newPage).cacheIndexInArr);
            Assert.IsNull(oldPage.Parent);
            Assert.AreEqual(-1, ((Json)oldPage).cacheIndexInArr);
        }

        [Test]
        public static void TestParentAssignmentWithUnboundSetterForArr() {
            var schema = new TObject();
            var tpages = schema.Add<TObjArr>("Pages");

            dynamic json = new Json() { Template = schema };
            dynamic newPage = new Arr<Json>(null, null);
            dynamic oldPage = new Arr<Json>(null, null);

            tpages.UnboundSetter(json, oldPage);
            Assert.IsNotNull(oldPage.Parent);
            Assert.AreNotEqual(-1, ((Json)oldPage).cacheIndexInArr);

            tpages.UnboundSetter(json, newPage);
            Assert.IsNotNull(newPage.Parent);
            Assert.AreNotEqual(-1, ((Json)newPage).cacheIndexInArr);
            Assert.IsNull(oldPage.Parent);
            Assert.AreEqual(-1, ((Json)oldPage).cacheIndexInArr);
        }

        [Test]
        public static void TestParentAssignmentWithUnboundSetterForTypedArr() {
            var schema = new TObject();
            var tpages = schema.Add<TArray<Json>>("Pages");

            dynamic json = new Json() { Template = schema };
            dynamic newPage = new Arr<Json>(null, null);
            dynamic oldPage = new Arr<Json>(null, null);

            tpages.UnboundSetter(json, oldPage);
            Assert.IsNotNull(oldPage.Parent);
            Assert.AreNotEqual(-1, ((Json)oldPage).cacheIndexInArr);

            tpages.UnboundSetter(json, newPage);
            Assert.IsNotNull(newPage.Parent);
            Assert.AreNotEqual(-1, ((Json)newPage).cacheIndexInArr);
            Assert.IsNull(oldPage.Parent);
            Assert.AreEqual(-1, ((Json)oldPage).cacheIndexInArr);
        }

        [Test]
        public static void TestParentAssignmentWithCustomSetterForObject() {
            Json backingValue = null;
            var schema = new TObject();
            var tpage = schema.Add<TObject>("Page");
            tpage.BindingStrategy = BindingStrategy.Unbound;
            tpage.SetCustomAccessors(
                (parent) => { return backingValue; },
                (parent, value) => { backingValue = value; }
            );
            
            dynamic json = new Json() { Template = schema };
            dynamic newPage = new Json();
            dynamic oldPage = new Json();

            json.Page = oldPage;
            Assert.IsNotNull(oldPage.Parent);
            Assert.AreNotEqual(-1, ((Json)oldPage).cacheIndexInArr);
            
            json.Page = newPage;
            Assert.IsNotNull(newPage.Parent);
            Assert.AreNotEqual(-1, ((Json)newPage).cacheIndexInArr);
            Assert.IsNull(oldPage.Parent);
            Assert.AreEqual(-1, ((Json)oldPage).cacheIndexInArr);
        }

        [Test]
        public static void TestParentAssignmentWithCustomSetterForArr() {
            Json backingValue = null;
            var schema = new TObject();
            var tpages = schema.Add<TObjArr>("Pages");
            tpages.BindingStrategy = BindingStrategy.Unbound;
            tpages.SetCustomAccessors(
                (parent) => { return backingValue; },
                (parent, value) => { backingValue = value; }
            );

            dynamic json = new Json() { Template = schema };
            dynamic newPage = new Arr<Json>(null, null);
            dynamic oldPage = new Arr<Json>(null, null);

            tpages.Setter(json, oldPage);
            Assert.IsNotNull(oldPage.Parent);
            Assert.AreNotEqual(-1, ((Json)oldPage).cacheIndexInArr);

            tpages.Setter(json, newPage);
            Assert.IsNotNull(newPage.Parent);
            Assert.AreNotEqual(-1, ((Json)newPage).cacheIndexInArr);
            Assert.IsNull(oldPage.Parent);
            Assert.AreEqual(-1, ((Json)oldPage).cacheIndexInArr);
        }

        [Test]
        public static void TestParentAssignmentWithCustomSetterForTypedArr() {
            Arr<Json> backingValue = null;
            var schema = new TObject();
            var tpages = schema.Add<TArray<Json>>("Pages");
            tpages.BindingStrategy = BindingStrategy.Unbound;
            tpages.SetCustomAccessors(
                (parent) => { return backingValue; },
                (parent, value) => { backingValue = value; }
            );

            dynamic json = new Json() { Template = schema };
            dynamic newPage = new Arr<Json>(null, null);
            dynamic oldPage = new Arr<Json>(null, null);

            tpages.Setter(json, oldPage);
            Assert.IsNotNull(oldPage.Parent);
            Assert.AreNotEqual(-1, ((Json)oldPage).cacheIndexInArr);

            tpages.Setter(json, newPage);
            Assert.IsNotNull(newPage.Parent);
            Assert.AreNotEqual(-1, ((Json)newPage).cacheIndexInArr);
            Assert.IsNull(oldPage.Parent);
            Assert.AreEqual(-1, ((Json)oldPage).cacheIndexInArr);
        }


        /// <summary>
        /// Tests TestCorrectJsonInstances.
        /// </summary>
        [Test]
        public static void TestCorrectJsonInstances() {
            TObject personSchema = Helper.CreateComplexPersonTemplate();

            dynamic p1 = personSchema.CreateInstance();
            dynamic n1 = p1.Fields.Add();
            dynamic n2 = p1.Fields.Add();

            Assert.IsInstanceOf<Json>(p1);
            Assert.IsInstanceOf<MyFieldMessage>(n1);
            Assert.IsInstanceOf<MyFieldMessage>(n2);
        }

        [Test]
        public static void TestInheritanceForArraysAndObjects() {
            var baseJson = new CJ.BaseJson();
            var inheritedJson = new CJ.InheritedJson();

            Assert.AreEqual("Base", baseJson.SimpleValue);
            Assert.IsNotNull(baseJson.ArrValue);
            Assert.IsNotNull(baseJson.ObjValue);

            Assert.AreEqual("Inherited", inheritedJson.SimpleValue);
            Assert.IsNotNull(inheritedJson.ArrValue, "Inherited array is null");
            Assert.IsNotNull(inheritedJson.ObjValue, "Inherited object is null");
        }

        [Test]
        public static void TestExistingPropertyNames() {
            string json;
            Exception ex;

            // Parent is a public property in Json.
            json = @"{""Parent"": 2}";
            ex = Assert.Catch<Exception>(() => { Helper.CreateJsonTemplateFromContent("Test", json); });
            Assert.IsTrue(ex.Message.Contains(" already contains a definition for 'Parent'"));

            // Data is a public property in Json.
            json = @"{""Data"": 2}";
            ex = Assert.Catch<Exception>(() => { Helper.CreateJsonTemplateFromContent("Test", json); });
            Assert.IsTrue(ex.Message.Contains(" already contains a definition for 'Data'"));

            // Should be fine even if there is a Remove method in Json, but it does not collide.
            json = @"{""Remove"": 2}";
            Assert.DoesNotThrow(() => { Helper.CreateJsonTemplateFromContent("Test", json); });
        }

        [Test]
        public static void TestChangeBoundObject() {
            var template = new TObject();
            var pageTemplate = template.Add<TObject>("Number");
            pageTemplate.Add<TString>("Number");

            dynamic msg = new Json { Template = template };

            var myDataObj = new PersonObject();
            var numberObj = new PhoneNumberObject() { Number = "123-555-7890" };
            myDataObj.Number = numberObj;
            msg.Data = myDataObj;

            string json = msg.ToJson();
            Assert.AreEqual(@"{""Number"":{""Number"":""123-555-7890""}}", json);

            var oldNumberJson = msg.Number;

            msg.Number = new Json();
            json = msg.ToJson();
            Assert.AreEqual(@"{""Number"":{}}", json);

            myDataObj.Number = numberObj;

            msg.Number = null;
            json = msg.ToJson();
            Assert.AreEqual(@"{""Number"":{}}", json);

            msg.Number = oldNumberJson;
            msg.Number.Data = numberObj;
            json = msg.ToJson();
            Assert.AreEqual(@"{""Number"":{""Number"":""123-555-7890""}}", json);

            msg.Number.Data = null;
            json = msg.ToJson();
            Assert.AreEqual(@"{""Number"":{""Number"":""""}}", json);
        }

        /// <summary>
        /// Tests TestDataBinding.
        /// </summary>
        [Test]
        public static void TestDataBinding() {
            dynamic msg = new Json { Template = Helper.CreateSimplePersonTemplateWithDataBinding() };

            var myDataObj = new PersonObject() { FirstName = "Kalle", Surname = "Kula", Age = 21, Misc = "Lorem Ipsum" };
            myDataObj.Number = new PhoneNumberObject() { Number = "123-555-7890" };
            msg.Data = myDataObj;

            msg.Updated = "2013-04-25 13:15:12";

            // Reading bound values.
            Assert.AreEqual("Kalle", msg.FirstName);
            Assert.AreEqual("Kula", msg.LastName);
            Assert.AreEqual(0, msg.Age); // Since Age shouldn't be bound we should get a zero back and not 21.
            Assert.IsNotNullOrEmpty(msg.Created);
            Assert.IsNullOrEmpty(msg.Misc); // Same as above. Not bound so no value should be retrieved.
            Assert.AreEqual("123-555-7890", msg.PhoneNumber.Number);

            // Setting bound values.
            msg.FirstName = "Allan";
            msg.LastName = "Ballan";
            msg.Age = 109L;
            msg.PhoneNumber.Number = "666";
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
            // Bound to SimpleBase datatype.
            TValue tSimple = Helper.CreateJsonTemplateFromFile("simple.json");
            dynamic json = tSimple.CreateInstance();

            var o = new SubClass1();
            json.Data = o;
            json.BaseValue = "SubClass1";
            json.AbstractValue = "SubClass1";
            string virtualValue = json.VirtualValue;

            Assert.AreEqual("SubClass1", o.BaseValue);
            Assert.AreEqual("SubClass1", o.AbstractValue);
            Assert.AreEqual("SubClass1", virtualValue);

            var simpleData2 = new SubClass2();
            Assert.DoesNotThrow(() => { json.Data = simpleData2; });
            Assert.DoesNotThrow(() => { json.BaseValue = "SubClass2"; });
            Assert.DoesNotThrow(() => { json.AbstractValue = "SubClass2"; });
            virtualValue = json.VirtualValue;

            Assert.AreEqual("SubClass2", simpleData2.BaseValue);
            Assert.AreEqual("SubClass2", simpleData2.AbstractValue);
            Assert.AreEqual("SubClass2", virtualValue);

            var simpleData3 = new SubClass3();
            Assert.DoesNotThrow(() => { json.Data = simpleData3; });
            Assert.DoesNotThrow(() => { json.BaseValue = "SubClass3"; });
            Assert.DoesNotThrow(() => { json.AbstractValue = "SubClass3"; });
            virtualValue = json.VirtualValue;

            Assert.AreEqual("SubClass3", simpleData3.BaseValue);
            Assert.AreEqual("SubClass3", simpleData3.AbstractValue);
            Assert.AreEqual("SubClass3", virtualValue);
        }

        [Test]
        public static void TestUntypedObjectArray() {
            var schema = new TObject();
            schema.Add<TArray<Json>>("Items");
            schema.Add<TArray<Json>>("Items2");

            dynamic json = (Json)schema.CreateInstance();
            dynamic item = new Json();
            item.Header = "Apa papa";
            json.Items.Add(item);

            item = new Json();
            item.Name = "La la la";
            json.Items.Add(item);

            item = json.Items.Add();
            item.AProp = 19;

            Person p = new Person();
            item = json.Items.Add();
            item.Data = p;

            var persons = new List<Person>();
            persons.Add(new Person() { FirstName = "Apa" });
            persons.Add(new Person() { FirstName = "Papa" });
            persons.Add(new Person() { FirstName = "Qwerty" });
            json.Items2 = persons;

            item = new Json();
            item.Value = 27;
            json.Items2.Add(item);

            string str = json.ToJson();
            Helper.ConsoleWriteLine(str);
        }

        [Test]
        public static void TestTemplateGettersAndSetters() {
            bool bound;
            TObject tJson = Helper.CreateSimplePersonTemplateWithDataBinding();
            Json json = (Json)tJson.CreateInstance();

            var person = new PersonObject();
            person.FirstName = "Ture";
            person.Surname = "ApaPapa";
            person.Age = 19;

            person.Number = new PhoneNumberObject();
            person.Number.Number = "12345678";

            json.Data = person;

            // FirstName$
            TString property = tJson.Properties[0] as TString;
            Assert.IsNotNull(property);

            bound = (property.Bind != null);
            if (bound)
                property.GenerateBoundGetterAndSetter(json);
#if DEBUG
            Helper.PrintTemplateDebugInfo<String>(property);
#endif

            property.UnboundSetter(json, "Test!");
            Assert.AreEqual("Test!", property.UnboundGetter(json));

            if (bound) {
                Assert.AreEqual(person.FirstName, property.BoundGetter(json));
                property.BoundSetter(json, "NotTure");
                Assert.AreEqual("NotTure", person.FirstName);
            }

            // The property is bound so getter should return the bound value.
            Assert.AreEqual("NotTure", property.Getter(json));
            property.Setter(json, "SomeOther");
            Assert.AreEqual("SomeOther", person.FirstName);

            // LastName
            property = tJson.Properties[1] as TString;
            Assert.IsNotNull(property);

            bound = (property.Bind != null);
            if (bound)
                property.GenerateBoundGetterAndSetter(json);
#if DEBUG
            Helper.PrintTemplateDebugInfo<String>(property);
#endif

            property.UnboundSetter(json, "Test!");
            Assert.AreEqual("Test!", property.UnboundGetter(json));

            if (bound) {
                Assert.AreEqual(person.Surname, property.BoundGetter(json));
                property.BoundSetter(json, "NotApapapa");
                Assert.AreEqual("NotApapapa", person.Surname);
            }

            // The property is bound so getter should return the bound value.
            Assert.AreEqual("NotApapapa", property.Getter(json));
            property.Setter(json, "SomeOther");
            Assert.AreEqual("SomeOther", person.Surname);


            // Age
            TLong ageProperty = tJson.Properties[2] as TLong;
            Assert.IsNotNull(ageProperty);

            bound = (ageProperty.Bind != null);
            if (bound)
                ageProperty.GenerateBoundGetterAndSetter(json);
#if DEBUG
            Helper.PrintTemplateDebugInfo<long>(ageProperty);
#endif

            ageProperty.UnboundSetter(json, 199);
            Assert.AreEqual(199, ageProperty.UnboundGetter(json));

            if (bound) {
                Assert.AreEqual(person.Age, ageProperty.BoundGetter(json));
                ageProperty.BoundSetter(json, 213);
                Assert.AreEqual(213, person.Age);
            }

            // The property is not bound so getter should return the unbound value.
            Assert.AreEqual(199, ageProperty.Getter(json));
            ageProperty.Setter(json, 226);
            Assert.AreNotEqual(226, person.Age); // not bound, data value should not be changed.


            // PhoneNumber
            TObject pnProperty = tJson.Properties[8] as TObject;
            Assert.IsNotNull(pnProperty);

            bound = (pnProperty.Bind != null);
            if (bound)
                pnProperty.GenerateBoundGetterAndSetter(json);
#if DEBUG
            Helper.PrintTemplateDebugInfo(pnProperty);
#endif

            var pn = new PhoneNumberObject();
            Json pnJson = (Json)pnProperty.CreateInstance();
            pnJson.Data = pn;
            pnProperty.UnboundSetter(json, pnJson);
            Assert.AreEqual(pn, pnProperty.UnboundGetter(json).Data);

            if (bound) {
                Assert.AreEqual(person.Number, pnProperty.BoundGetter(json));
                pnProperty.BoundSetter(json, pn);
                Assert.AreEqual(pn, person.Number);
            }
        }

        [Test]
        public static void TestAddAndRemoveOnArray() {
            dynamic item1 = new Json();
            dynamic item2 = new Json();
            dynamic item3 = new Json();

            dynamic root = new Json();

            var items = new List<Json>();
            items.Add(item1);
            root.Items = items;

            root.Items.Add(item2);
            root.Items.Add(item3);

            Assert.AreEqual(3, root.Items.Count);
            Assert.NotNull(item1.Parent);
            Assert.NotNull(item2.Parent);
            Assert.NotNull(item3.Parent);

            bool b = root.Items.Remove(item2);
            Assert.IsTrue(b);
            Assert.AreEqual(2, root.Items.Count);
            Assert.IsNull(item2.Parent);

            root.Items.Clear();

            Assert.AreEqual(0, root.Items.Count);
            Assert.IsNull(item1.Parent);
            Assert.IsNull(item3.Parent);

            root.Items.Add(item1);
            root.Items[0] = item2;

            Assert.AreEqual(1, root.Items.Count);
            Assert.IsTrue(null == item1.Parent);
            Assert.IsNotNull(item2.Parent);
        }

        [Test]
        public static void TestInsertAndRemoveAtOnArray() {
            dynamic root = new Json();
            dynamic item;

            root.Items = new List<Json>();

            item = new Json();
            item.Number = 1;
            root.Items.Add(item);
            Assert.IsNotNull(item.Parent);
            Assert.AreEqual(1, root.Items.Count);

            item = new Json();
            item.Number = 2;
            root.Items.Add(item);
            Assert.IsNotNull(item.Parent);
            Assert.AreEqual(2, root.Items.Count);

            item = new Json();
            item.Number = 3;
            root.Items.Add(item);
            Assert.IsNotNull(item.Parent);
            Assert.AreEqual(3, root.Items.Count);

            item = new Json();
            item.Number = 99;
            root.Items.Insert(1, item);
            Assert.IsNotNull(item.Parent);
            Assert.AreEqual(4, root.Items.Count);

            item = root.Items[2];
            root.Items.RemoveAt(2);
            Assert.IsNull(item.Parent);
            Assert.AreEqual(3, root.Items.Count);
        }

        [Test]
        public static void TestDynamicStringArray() {
            var str1 = "One";
            var str2 = "Two";
            var str3 = "Three";
            dynamic json = new Json();

            json.Sections = new string[] { str1, str2, str3 };
            json.Sections2 = new List<string>() { str3, str1 };

            string jsonStr = json.ToJson();
            Helper.ConsoleWriteLine(jsonStr);

            
            Assert.AreEqual(3, json.Sections.Count);
            Assert.AreEqual(str1, json.Sections[0].StringValue);
            Assert.AreEqual(str2, json.Sections[1].StringValue);
            Assert.AreEqual(str3, json.Sections[2].StringValue);

            Assert.AreEqual(2, json.Sections2.Count);
            Assert.AreEqual(str3, json.Sections2[0].StringValue);
            Assert.AreEqual(str1, json.Sections2[1].StringValue);
        }

        [Test]
        public static void TestStringArrayAsData() {
            var str1 = "One";
            var str2 = "Two";
            var str3 = "Three";
            dynamic json = new Json();

            json.Sections = new List<string>(); // Added just to create the property.

            var data = new string[] { str1, str2, str3 };
            json.Sections.Data = data;

            string jsonStr = json.ToJson();
            Helper.ConsoleWriteLine(jsonStr);

            Assert.AreEqual(3, json.Sections.Count);
            Assert.AreEqual(str1, json.Sections[0].StringValue);
            Assert.AreEqual(str2, json.Sections[1].StringValue);
            Assert.AreEqual(str3, json.Sections[2].StringValue);
        }

        [Test]
        public static void TestDynamicMixedArray() {
            var value1 = "One";
            var value2 = 39L;
            dynamic value3 = new Json();
            dynamic json = new Json();

            value3.Title = "JSON!";
            json.MixedValues = new object[] { value1, value2, value3 };
            
            string jsonStr = json.ToJson();
            Helper.ConsoleWriteLine(jsonStr);

            Assert.AreEqual(3, json.MixedValues.Count);
            Assert.AreEqual(value1, json.MixedValues[0].StringValue);
            Assert.AreEqual(value2, json.MixedValues[1].IntegerValue);
            Assert.IsInstanceOfType(typeof(Json), json.MixedValues[2]);
            Assert.AreEqual(value3, json.MixedValues[2]);
            Assert.AreEqual("JSON!", json.MixedValues[2].Title);
        }

        [Test]
        public static void TestInvalidCharactersinJsonPropertyName() {
            TValue template;

            string json = @"{""Name with space"":1}";
            var ex = Assert.Throws<TemplateFactoryException>(
                () => { template = Helper.CreateJsonTemplateFromContent("Test", json); }
            );
            Helper.ConsoleWriteLine(ex.Message);

            json = @"{""7Name--with .dea@"":1}";
            ex = Assert.Throws<TemplateFactoryException>(
                () => { template = Helper.CreateJsonTemplateFromContent("Test", json); }
            );
            Helper.ConsoleWriteLine(ex.Message);

            json = @"{""£blhaha"":1}";
            ex = Assert.Throws<TemplateFactoryException>(
                () => { template = Helper.CreateJsonTemplateFromContent("Test", json); }
            );
            Helper.ConsoleWriteLine(ex.Message);

            json = @"{""blhaha@1"":1}";
            ex = Assert.Throws<TemplateFactoryException>(
                () => { template = Helper.CreateJsonTemplateFromContent("Test", json); }
            );
            Helper.ConsoleWriteLine(ex.Message);

            json = @"{""bla345sa234"":1}";
            Assert.DoesNotThrow(
                () => { template = Helper.CreateJsonTemplateFromContent("Test", json); }
            );   
        }

        [Test]
        public static void TestSettingDataOnUnboundArray_3548() {
            dynamic json = new Json();
            dynamic data = new object[] {
                new { Name = "One" },
                new { Name = "Two" }
            };

            json.Items = new List<Json>();
            json.Items.Data = data;

            Assert.AreEqual(2, json.Items.Count);
            Assert.AreEqual(data[0].Name, json.Items[0].Name);
            Assert.AreEqual(data[1].Name, json.Items[1].Name);

            data = new object[] {
                new { Name = "Three" },
                new { Name = "Four" },
                new { Name = "Five" }
            };
            json.Items.Data = data;

            Assert.AreEqual(3, json.Items.Count);
            Assert.AreEqual(data[0].Name, json.Items[0].Name);
            Assert.AreEqual(data[1].Name, json.Items[1].Name);
            Assert.AreEqual(data[2].Name, json.Items[2].Name);
        }

        [Test]
        public static void TestArrayInArray_3554() {
            var jsonStr = @"{""Items"": [ [ 3 ] ] }";

            TObject schema = (TObject)TObject.CreateFromMarkup(jsonStr);

            Assert.AreEqual(1, schema.Properties.Count);
            Assert.IsInstanceOf(typeof(TObjArr), schema.Properties[0]);
            TObjArr tArr = (TObjArr)schema.Properties[0];
            Assert.IsInstanceOf(typeof(TObjArr), tArr.ElementType);
            TObjArr tSubArr = (TObjArr)tArr.ElementType;
            Assert.IsInstanceOf(typeof(TLong), tSubArr.ElementType);
        }
    }
}
