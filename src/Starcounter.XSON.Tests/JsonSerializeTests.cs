using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Starcounter.Advanced.XSON;
using Starcounter.Templates;
using Starcounter.XSON;
using Starcounter.XSON.Interfaces;
using XSONModule = Starcounter.Internal.XSON.Modules.Starcounter_XSON;

namespace Starcounter.Internal.XSON.Tests {
    /// <summary>
    /// 
    /// </summary>
    public static class JsonSerializeTests {
        private static ITypedJsonSerializer defaultSerializer;

        [TestFixtureSetUp]
        public static void InitializeTest() {
			defaultSerializer = new NewtonSoftSerializer();
        }

        [Test]
        public static void TestStandardSerializer() {
            RunStandardSerializerTest("jsstyle.json", File.ReadAllText("Json\\jsstyle.json"));
            RunStandardSerializerTest("person.json", File.ReadAllText("Json\\person.json"));
            RunStandardSerializerTest("supersimple.json", File.ReadAllText("Json\\supersimple.json"));
            RunStandardSerializerTest("simple.json", File.ReadAllText("Json\\simple.json"));
            RunStandardSerializerTest("TestMessage.json", File.ReadAllText("Json\\TestMessage.json"));
            RunStandardSerializerTest("JsonWithFiller.json", File.ReadAllText("Json\\JsonWithFiller.json"));
            RunStandardSerializerTest("SingleValue.json", File.ReadAllText("Json\\SingleValue.json"));
            RunStandardSerializerTest("SingleArray.json", File.ReadAllText("Json\\SingleArray.json"));
        }

        [Test]
        public static void TestStandardSerializerWithCompiledJson() {
            RunStandardSerializerTest("jsstyle.json", jsstyle.DefaultTemplate);
            RunStandardSerializerTest("person.json", person.DefaultTemplate);
            RunStandardSerializerTest("supersimple.json", supersimple.DefaultTemplate);
            RunStandardSerializerTest("simple.json", simple.DefaultTemplate);
            RunStandardSerializerTest("TestMessage.json", TestMessage.DefaultTemplate);
            RunStandardSerializerTest("JsonWithFiller.json", JsonWithFiller.DefaultTemplate);
            RunStandardSerializerTest("SingleValue.json", SingleValue.DefaultTemplate);
            RunStandardSerializerTest("SingleValue.json", SingleArray.DefaultTemplate);
        }

        private static void RunStandardSerializerTest(string name, string jsonStr) {
            TValue tval = Helper.CreateJsonTemplateFromContent(Path.GetFileNameWithoutExtension(name), jsonStr);
            RunStandardSerializerTest(name, tval);
        }

        private static void RunStandardSerializerTest(string name, TValue tval) {
            Json original;
            Json newJson;

            original = (Json)tval.CreateInstance();
            string jsonStr = original.ToJson();

            newJson = (Json)tval.CreateInstance();
            newJson.PopulateFromJson(jsonStr);

            Helper.AssertAreEqual(original, newJson);
        }

        [Test]
        public static void TestIncorrectInputJsonForDefaultSerializer() {
            TValue tObj = Helper.CreateJsonTemplateFromFile("PlayerAndAccounts.json");
            var obj = (Json)tObj.CreateInstance();

            var invalidJson = "PlayerId: \"Hey!\" }";
            Assert.Catch(() => obj.PopulateFromJson(invalidJson));

            invalidJson = "{ PlayerId: \"Hey!\" ";
            Assert.Catch(() => obj.PopulateFromJson(invalidJson));

            invalidJson = "{ PlayerId: Hey }";
            Assert.Catch(() => obj.PopulateFromJson(invalidJson));

            invalidJson = "{ PlayerId: 123";
            Assert.Catch(() => obj.PopulateFromJson(invalidJson));

            invalidJson = "{ Accounts: [ }";
            Assert.Catch(() => obj.PopulateFromJson(invalidJson));

            invalidJson = "{ Accounts: ] }";
            Assert.Catch(() => obj.PopulateFromJson(invalidJson));
        }

        private static void AssertAreEqual(byte[] expected, byte[] actual, int count) {
            Assert.AreEqual(expected.Length, count);
            for (int i = 0; i < count; i++) {
                if (expected[i] != actual[i])
                    throw new AssertionException("Expected '" + expected[i] + "' but found '" + actual[i] + "' at position " + i + ".");
            }
        }
        
        [Test]
        public static void TestJsonSerialization1() {
            dynamic o1 = new Json();
            o1.SomeString = "NewString!";
            o1.AnotherString = "AnotherString!";
            o1.SomeDecimal = (decimal)1.234567;
            o1.SomeLong = 1234567;

            dynamic o2 = new Json();
            o2.SomeString = "NewString!";
            o2.AnotherString = "AnotherString!";
            o2.SomeDecimal = (decimal)1.234567;
            o2.SomeLong = 1234567;

            List<Json> o3 = new List<Json>();
            o3.Add(o1);
            o3.Add(o1);
            o3.Add(o1);

            dynamic o4 = new Json();
            o4.SomeString = "SomeString!";
            o4.SomeBool = true;
            o4.SomeDecimal = (decimal)1.234567;
            o4.SomeDouble = (double)-1.234567;
            o4.SomeLong = 1234567;
            o4.SomeObject = o2;
            o4.SomeArray = o3;
            o4.SomeString2 = "SomeString2!";

            string serString = o4.ToJson();

            Assert.AreEqual(@"{""SomeString"":""SomeString!"",""SomeBool"":true,""SomeDecimal"":1.234567,""SomeDouble"":-1.234567,""SomeLong"":1234567,""SomeObject"":{""SomeString"":""NewString!"",""AnotherString"":""AnotherString!"",""SomeDecimal"":1.234567,""SomeLong"":1234567},""SomeArray"":[{""SomeString"":""NewString!"",""AnotherString"":""AnotherString!"",""SomeDecimal"":1.234567,""SomeLong"":1234567},{""SomeString"":""NewString!"",""AnotherString"":""AnotherString!"",""SomeDecimal"":1.234567,""SomeLong"":1234567},{""SomeString"":""NewString!"",""AnotherString"":""AnotherString!"",""SomeDecimal"":1.234567,""SomeLong"":1234567}],""SomeString2"":""SomeString2!""}", serString);
        }

        [Test]
        public static void TestJsonSerialization2() {
            dynamic o1 = new Json();
            o1.SomeString = "NewString!";
            o1.AnotherString = "AnotherString!";
            o1.SomeDecimal = (decimal)1.234567;
            o1.SomeLong = 1234567;

            List<Json> o2 = new List<Json>();
            o2.Add(o1);
            o2.Add(o1);
            o2.Add(o1);

            dynamic o3 = new Json();
            o3.SomeArray = o2;

            String serString = o3.ToJson();

            Assert.AreEqual(@"{""SomeArray"":[{""SomeString"":""NewString!"",""AnotherString"":""AnotherString!"",""SomeDecimal"":1.234567,""SomeLong"":1234567},{""SomeString"":""NewString!"",""AnotherString"":""AnotherString!"",""SomeDecimal"":1.234567,""SomeLong"":1234567},{""SomeString"":""NewString!"",""AnotherString"":""AnotherString!"",""SomeDecimal"":1.234567,""SomeLong"":1234567}]}", serString);
        }
        
        [Test]
        public static void TestJsonDeserializationWithMissingMembers_1() {
            // Testing custom settings object specified each time to ignore missing members.

            dynamic json = new Json();
            json.id = "abc";
            json.gender = "F";

            var settings = new JsonSerializerSettings();
            settings.MissingMemberHandling = MissingMemberHandling.Ignore;

            // Unknown number property
            string jsonSource = @" { ""id"":""1"", ""age"":19, ""gender"":""Female"" }";
            Assert.DoesNotThrow(() => {
                ((Json)json).PopulateFromJson(jsonSource, settings);
            });
            Assert.AreEqual("1", json.id);
            Assert.AreEqual("Female", json.gender);
            
            // Unknown string property
            jsonSource = @" { ""id"":""ab"", ""age"":""nineteen"", ""gender"":""Male"" }";
            Assert.DoesNotThrow(() => {
                ((Json)json).PopulateFromJson(jsonSource, settings);
            });
            Assert.AreEqual("ab", json.id);
            Assert.AreEqual("Male", json.gender);

            // Unknown object property
            jsonSource = @" { ""id"":""3"", ""age"": { ""innermember"":""nineteen"" }, ""gender"":""Unknown"" }";
            Assert.DoesNotThrow(() => {
                ((Json)json).PopulateFromJson(jsonSource, settings);
            });
            Assert.AreEqual("3", json.id);
            Assert.AreEqual("Unknown", json.gender);

            // Unknown array property
            jsonSource = @" { ""id"":""abc123"", ""age"": [ 19, 21, 32 ], ""gender"":""Ooops"" }";
            Assert.DoesNotThrow(() => {
                ((Json)json).PopulateFromJson(jsonSource, settings);
            });
            Assert.AreEqual("abc123", json.id);
            Assert.AreEqual("Ooops", json.gender);
        }

        [Test]
        public static void TestJsonDeserializationWithMissingMembers_2() {
            // Testing changing default settings object to ignore missing members.

            dynamic json = new Json();
            json.id = "abc";
            json.gender = "F";

            var settings = new JsonSerializerSettings();
            settings.MissingMemberHandling = MissingMemberHandling.Ignore;

            var oldSettings = JsonSerializerSettings.Default;
            try {
                JsonSerializerSettings.Default = settings;

                // Unknown number property
                string jsonSource = @" { ""id"":""1"", ""age"":19, ""gender"":""Female"" }";
                Assert.DoesNotThrow(() => {
                    ((Json)json).PopulateFromJson(jsonSource);
                });
                Assert.AreEqual("1", json.id);
                Assert.AreEqual("Female", json.gender);

                // Unknown string property
                jsonSource = @" { ""id"":""ab"", ""age"":""nineteen"", ""gender"":""Male"" }";
                Assert.DoesNotThrow(() => {
                    ((Json)json).PopulateFromJson(jsonSource);
                });
                Assert.AreEqual("ab", json.id);
                Assert.AreEqual("Male", json.gender);

                // Unknown object property
                jsonSource = @" { ""id"":""3"", ""age"": { ""innermember"":""nineteen"" }, ""gender"":""Unknown"" }";
                Assert.DoesNotThrow(() => {
                    ((Json)json).PopulateFromJson(jsonSource);
                });
                Assert.AreEqual("3", json.id);
                Assert.AreEqual("Unknown", json.gender);

                // Unknown array property
                jsonSource = @" { ""id"":""abc123"", ""age"": [ 19, 21, 32 ], ""gender"":""Ooops"" }";
                Assert.DoesNotThrow(() => {
                    ((Json)json).PopulateFromJson(jsonSource);
                });
                Assert.AreEqual("abc123", json.id);
                Assert.AreEqual("Ooops", json.gender);
            } finally {
                JsonSerializerSettings.Default = oldSettings;
            }
        }

        [Test]
        public static void TestJsonDeserializationWithMissingMembers_3() {
            // Testing default settings object to throw errors on missing members.

            Exception ex;
            uint errorCode;
            dynamic json = new Json();
            json.id = "abc";
            json.gender = "F";
            
            // Unknown number property
            string jsonSource = @" { ""id"":""1"", ""age"":19, ""gender"":""Female"" }";
            ex = Assert.Throws<Exception>(() => {
                ((Json)json).PopulateFromJson(jsonSource);
            });
            Assert.IsTrue(ErrorCode.TryGetCode(ex, out errorCode));
            Assert.AreEqual(Error.SCERRJSONPROPERTYNOTFOUND, errorCode);
            
            // Unknown string property
            jsonSource = @" { ""id"":""ab"", ""age"":""nineteen"", ""gender"":""Male"" }";
            ex = Assert.Throws<Exception>(() => {
                ((Json)json).PopulateFromJson(jsonSource);
            });
            Assert.IsTrue(ErrorCode.TryGetCode(ex, out errorCode));
            Assert.AreEqual(Error.SCERRJSONPROPERTYNOTFOUND, errorCode);

            // Unknown object property
            jsonSource = @" { ""id"":""3"", ""age"": { ""innermember"":""nineteen"" }, ""gender"":""Unknown"" }";
            ex = Assert.Throws<Exception>(() => {
                ((Json)json).PopulateFromJson(jsonSource);
            });
            Assert.IsTrue(ErrorCode.TryGetCode(ex, out errorCode));
            Assert.AreEqual(Error.SCERRJSONPROPERTYNOTFOUND, errorCode);

            // Unknown array property
            jsonSource = @" { ""id"":""abc123"", ""age"": [ 19, 21, 32 ], ""gender"":""Ooops"" }";
            ex = Assert.Throws<Exception>(() => {
                ((Json)json).PopulateFromJson(jsonSource);
            });
            Assert.IsTrue(ErrorCode.TryGetCode(ex, out errorCode));
            Assert.AreEqual(Error.SCERRJSONPROPERTYNOTFOUND, errorCode);
        }

        [Test]
        public static void TestDeserializeStringArray() {
            TObjArr schema = new TObjArr();
            schema.ElementType = new TString();

            string jsonStr = @"[""One"", ""Two""]";
            Json json = new Json() { Template = schema };

            json.PopulateFromJson(jsonStr);
            var list = (IList)json;

            Assert.AreEqual(2, list.Count);
            Assert.AreEqual("One", ((Json)list[0]).StringValue);
            Assert.AreEqual("Two", ((Json)list[1]).StringValue);
        }

        [Test]
        public static void TestDeserializeNullValues() {
            TObject schema = new TObject();
            TString tStr = schema.Add<TString>("MyStr");
            TDecimal tDec = schema.Add<TDecimal>("MyDec");
            TDouble tDbl = schema.Add<TDouble>("MyDbl");
            TLong tLong = schema.Add<TLong>("MyLong");
            TBool tBool = schema.Add<TBool>("MyBool");
            TObject tObj = schema.Add<TObject>("MyObj");
            TObjArr tArr = schema.Add<TObjArr>("MyArr");

            string jsonStr = @"{""MyStr"":null,""MyDec"":null,""MyDbl"":null,""MyLong"":null,""MyBool"":null,""MyObj"":null,""MyArr"":null}";
            Json json = new Json() { Template = schema };

            Assert.DoesNotThrow(() => {
                json.PopulateFromJson(jsonStr);
            });
            Assert.AreEqual("", tStr.Getter(json));
            Assert.AreEqual(default(decimal), tDec.Getter(json));
            Assert.AreEqual(default(double), tDbl.Getter(json));
            Assert.AreEqual(default(long), tLong.Getter(json));
            Assert.AreEqual(default(bool), tBool.Getter(json));
            Assert.IsNotNull(tObj.Getter(json));
            Assert.IsNotNull(tArr.Getter(json));
        }
    }
}
