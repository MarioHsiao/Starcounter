using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Starcounter.Advanced.XSON;
using Starcounter.Templates;
using Starcounter.XSON.Serializer;
using Starcounter.XSON.Serializer.Parsetree;
using XSONModule = Starcounter.Internal.XSON.Modules.Starcounter_XSON;

namespace Starcounter.Internal.XSON.Tests {
    /// <summary>
    /// 
    /// </summary>
    public static class JsonSerializeTests {
        private static StandardJsonSerializer defaultSerializer;
//		private static FasterThanJsonSerializer ftjSerializer;

        [TestFixtureSetUp]
        public static void InitializeTest() {
			defaultSerializer = new StandardJsonSerializer();
//			ftjSerializer = new FasterThanJsonSerializer();
        }

        [Test]
        public static void TestSerializeJsonString() {
            // needed size includes the quotations around the string.
            SerializeString("First!", 8);
            SerializeString("FirstWithSpecial\n", 20);
            SerializeString("\n\b\t", 8);
            SerializeString("\u001f", 8); 
        }

        private static void SerializeString(string value, int neededSize) {
            byte[] dest;
            int written;

            unsafe {
                // Assert that we dont write outside of the available space.
                dest = new byte[neededSize - 2];
                fixed (byte* pdest = dest) {
                    written = JsonHelper.WriteString((IntPtr)pdest, dest.Length, value);
                    Assert.AreEqual(-1, written);
                }

                // Assert that we write correct amount of bytes.
                dest = new byte[neededSize*2];
                fixed (byte* pdest = dest) {
                    written = JsonHelper.WriteString((IntPtr)pdest, dest.Length, value);
                    Assert.AreEqual(neededSize, written);
                }
            }
        }

		[Test]
		[Ignore("Requires fixing FTJ serializer")]
        public static void TestFTJSerializer() {
            RunFTJSerializerTest("jsstyle.json", File.ReadAllText("Json\\jsstyle.json"), false);
            RunFTJSerializerTest("person.json", File.ReadAllText("Json\\person.json"), false);
            RunFTJSerializerTest("supersimple.json", File.ReadAllText("Json\\supersimple.json"), false);
            RunFTJSerializerTest("simple.json", File.ReadAllText("Json\\simple.json"), false);
            RunFTJSerializerTest("TestMessage.json", File.ReadAllText("Json\\TestMessage.json"), false);
		}

        [Test]
        [Ignore("Requires fixing FTJ serializer")]
        public static void TestFTJSerializerWithCompiledJson() {
            RunFTJSerializerTest("jsstyle.json", jsstyle.DefaultTemplate, false);
            RunFTJSerializerTest("person.json", person.DefaultTemplate, false);
            RunFTJSerializerTest("supersimple.json", supersimple.DefaultTemplate, false);
            RunFTJSerializerTest("simple.json", simple.DefaultTemplate, false);
            RunFTJSerializerTest("TestMessage.json", TestMessage.DefaultTemplate, false);
        }

		[Test]
        [Ignore("Requires fixing FTJ serializer")]
		public static void TestFTJCodegenSerializer() {
            RunFTJSerializerTest("jsstyle.json", File.ReadAllText("Json\\jsstyle.json"), true);
            RunFTJSerializerTest("person.json", File.ReadAllText("Json\\person.json"), true);
            RunFTJSerializerTest("supersimple.json", File.ReadAllText("Json\\supersimple.json"), true);
            RunFTJSerializerTest("simple.json", File.ReadAllText("Json\\simple.json"), true);
            RunFTJSerializerTest("TestMessage.json", File.ReadAllText("Json\\TestMessage.json"), true);
		}

        [Test]
        [Ignore("Requires fixing FTJ serializer")]
        public static void TestFTJCodegenSerializerWithCompiledJson() {
            RunFTJSerializerTest("jsstyle.json", jsstyle.DefaultTemplate, true);
            RunFTJSerializerTest("person.json", person.DefaultTemplate, true);
            RunFTJSerializerTest("supersimple.json", supersimple.DefaultTemplate, true);
            RunFTJSerializerTest("simple.json", simple.DefaultTemplate, true);
            RunFTJSerializerTest("TestMessage.json", TestMessage.DefaultTemplate, true);
        }

		[Test]
		public static void TestStandardSerializer() {
            RunStandardSerializerTest("jsstyle.json", File.ReadAllText("Json\\jsstyle.json"), false);
            RunStandardSerializerTest("person.json", File.ReadAllText("Json\\person.json"), false);
            RunStandardSerializerTest("supersimple.json", File.ReadAllText("Json\\supersimple.json"), false);
            RunStandardSerializerTest("simple.json", File.ReadAllText("Json\\simple.json"), false);
            RunStandardSerializerTest("TestMessage.json", File.ReadAllText("Json\\TestMessage.json"), false);
            RunStandardSerializerTest("JsonWithFiller.json", File.ReadAllText("Json\\JsonWithFiller.json"), false);
            RunStandardSerializerTest("SingleValue.json", File.ReadAllText("Json\\SingleValue.json"), false);
            RunStandardSerializerTest("SingleArray.json", File.ReadAllText("Json\\SingleArray.json"), false);
		}

        [Test]
        public static void TestStandardSerializerWithCompiledJson() {
            RunStandardSerializerTest("jsstyle.json", jsstyle.DefaultTemplate, false);
            RunStandardSerializerTest("person.json", person.DefaultTemplate, false);
            RunStandardSerializerTest("supersimple.json", supersimple.DefaultTemplate, false);
            RunStandardSerializerTest("simple.json", simple.DefaultTemplate, false);
            RunStandardSerializerTest("TestMessage.json", TestMessage.DefaultTemplate, false);
            RunStandardSerializerTest("JsonWithFiller.json", JsonWithFiller.DefaultTemplate, false);
            RunStandardSerializerTest("SingleValue.json", SingleValue.DefaultTemplate, false);
            RunStandardSerializerTest("SingleValue.json", SingleArray.DefaultTemplate, false);
        }

		[Test]
		public static void TestStandardCodegenSerializer() {
			RunStandardSerializerTest("jsstyle.json", File.ReadAllText("Json\\jsstyle.json"), true);
            RunStandardSerializerTest("person.json", File.ReadAllText("Json\\person.json"), true);
            RunStandardSerializerTest("supersimple.json", File.ReadAllText("Json\\supersimple.json"), true);
            RunStandardSerializerTest("simple.json", File.ReadAllText("Json\\simple.json"), true);
            RunStandardSerializerTest("TestMessage.json", File.ReadAllText("Json\\TestMessage.json"), true);
            RunStandardSerializerTest("JsonWithFiller.json", File.ReadAllText("Json\\JsonWithFiller.json"), true);
            
            // TODO:
            // Codegen does not support single values, only objects currently.
//            RunStandardSerializerTest("SingleValue.json", File.ReadAllText("Json\\SingleValue.json"), true);
//            RunStandardSerializerTest("SingleArray.json", File.ReadAllText("Json\\SingleArray.json"), true);
		}

        [Test]
        public static void TestStandardCodegenSerializerWithCompiledJson() {
            RunStandardSerializerTest("jsstyle.json", jsstyle.DefaultTemplate, true);
            RunStandardSerializerTest("person.json", person.DefaultTemplate, true);
            RunStandardSerializerTest("supersimple.json", supersimple.DefaultTemplate, true);
            RunStandardSerializerTest("simple.json", simple.DefaultTemplate, true);
            RunStandardSerializerTest("TestMessage.json", TestMessage.DefaultTemplate, true);
            RunStandardSerializerTest("JsonWithFiller.json", JsonWithFiller.DefaultTemplate, true);

            // TODO:
            // Codegen does not support single values, only objects currently.
//            RunStandardSerializerTest("SingleValue.json", SingleValue.DefaultTemplate, true);
//            RunStandardSerializerTest("SingleValue.json", SingleArray.DefaultTemplate, true);
        }

        private static void RunFTJSerializerTest(string name, string jsonStr, bool useCodegen) {
            TValue tval = Helper.CreateJsonTemplateFromContent(Path.GetFileNameWithoutExtension(name), jsonStr);
            RunFTJSerializerTest(name, tval, useCodegen);
        }

		private static void RunFTJSerializerTest(string name, TValue tval, bool useCodegen) {
            //int serializedSize = 0;
            //int afterPopulateSize = 0;
            //Json original;
            //Json newJson;

            //XSONModule.UseCodegeneratedSerializer = false;

            //original = (Json)tObj.CreateInstance();

            //XSONModule.UseCodegeneratedSerializer = useCodegen;
            //XSONModule.DontCreateSerializerInBackground = true;

            //byte[] ftj = new byte[tObj.JsonSerializer.EstimateSizeBytes(original)];
            //serializedSize = tObj.ToFasterThanJson(original, ftj, 0);

            //unsafe {
            //    fixed (byte* p = ftj) {
            //        newJson = (Json)tObj.CreateInstance();
            //        afterPopulateSize = tObj.PopulateFromFasterThanJson(newJson, (IntPtr)p, serializedSize);
            //    }
            //}

            //Assert.AreEqual(serializedSize, afterPopulateSize);
            //Helper.AssertAreEqual(original, newJson);
		}

        private static void RunStandardSerializerTest(string name, string jsonStr, bool useCodegen) {
            TValue tval = Helper.CreateJsonTemplateFromContent(Path.GetFileNameWithoutExtension(name), jsonStr);
            RunStandardSerializerTest(name, tval, useCodegen);
        }

		private static void RunStandardSerializerTest(string name, TValue tval, bool useCodegen) {
			int serializedSize = 0;
			int afterPopulateSize = 0;
			Json original;
			Json newJson;

            XSONModule.UseCodegeneratedSerializer = false;
			original = (Json)tval.CreateInstance();

            XSONModule.UseCodegeneratedSerializer = useCodegen;
            XSONModule.DontCreateSerializerInBackground = true;

            byte[] jsonArr = new byte[tval.JsonSerializer.EstimateSizeBytes(original)];
			serializedSize = original.ToJsonUtf8(jsonArr, 0);

			unsafe {
				fixed (byte* p = jsonArr) {
					newJson = (Json)tval.CreateInstance();
					afterPopulateSize = newJson.PopulateFromJson((IntPtr)p, serializedSize);
				}
			}

			Assert.AreEqual(serializedSize, afterPopulateSize);
            Helper.AssertAreEqual(original, newJson);
		}

        [Test]
        public static void TestIncorrectInputJsonForDefaultSerializer() {
            TValue tObj = Helper.CreateJsonTemplateFromFile("PlayerAndAccounts.json");

            XSONModule.UseCodegeneratedSerializer = false;
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

 //       [Test]
        public static void TestIncorrectInputJsonForCodegenSerializer() {
            TValue tObj = Helper.CreateJsonTemplateFromFile("supersimple.json");

            XSONModule.UseCodegeneratedSerializer = true;
            XSONModule.DontCreateSerializerInBackground = true;
            var obj = (Json)tObj.CreateInstance();

            string invalidJson = "message";
            Assert.Catch(() => obj.PopulateFromJson(invalidJson));

            invalidJson = "PlayerId: \"Hey!\" }";
            Assert.Catch(() => obj.PopulateFromJson(invalidJson));

            invalidJson = "{ PlayerId: \"Hey!\" ";
            Assert.Catch(() => obj.PopulateFromJson(invalidJson));

            invalidJson = "{ PlayerId: Hey }";
            Assert.Catch(() => obj.PopulateFromJson(invalidJson));

            invalidJson = "{ PlayerId: 123";
            Assert.Catch(() => obj.PopulateFromJson(invalidJson));
        }

        [Test]
        public static void EncodeAndDecodeJsonStrings() {
            // "standard" special characters.
            EncodeDecodeString("1\b2\f3\n4\r5\t6\"7\\");

            // Low unicode characters.
            EncodeDecodeString("UnicodeChars:\u001a\u0006");

            // High unicode characters.
            EncodeDecodeString("UnicodeChars:\u2031");
        }

        private static void EncodeDecodeString(string value) {
            byte[] buffer = new byte[1024];
//            byte[] expected;
            int count;
            int used;
            string decodedString;

            unsafe {
                fixed (byte* p = buffer) {
                    count = JsonHelper.WriteString((IntPtr)p, buffer.Length, value);
                    JsonHelper.ParseString((IntPtr)p, buffer.Length, out decodedString, out used);
                }
            }
//            expected = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value));
//            AssertAreEqual(expected, buffer, count);

            Assert.AreEqual(count, used);
            Assert.AreEqual(value, decodedString);
        }

        private static void AssertAreEqual(byte[] expected, byte[] actual, int count) {
            Assert.AreEqual(expected.Length, count);
            for (int i = 0; i < count; i++) {
                if (expected[i] != actual[i])
                    throw new AssertionException("Expected '" + expected[i] + "' but found '" + actual[i] + "' at position " + i + ".");
            }
        }

        [Test]
        public static void GenerateStdSerializationParseTreeOverview() {
            TValue objTemplate;
            objTemplate = Helper.CreateJsonTemplateFromFile("person.json");
            ParseNode parseTree = ParseTreeGenerator.BuildParseTree(objTemplate);
            Helper.ConsoleWriteLine(parseTree.ToString());
        }

        [Test]
        public static void GenerateStdSerializationAstTreeOverview() {
            TObject objTemplate;
            objTemplate = (TObject)Helper.CreateJsonTemplateFromFile("person.json");

			StdDomGenerator domGenerator = new StdDomGenerator(objTemplate);
            Helper.ConsoleWriteLine(domGenerator.GenerateDomTree().ToString(true));
        }

		[Test]
		public static void GenerateFTJSerializationAstTreeOverview() {
            TObject objTemplate;
            objTemplate = (TObject)Helper.CreateJsonTemplateFromFile("person.json");

			FTJDomGenerator domGenerator = new FTJDomGenerator(objTemplate);
            Helper.ConsoleWriteLine(domGenerator.GenerateDomTree().ToString(true));
		}

		[Test]
		public static void GenerateStdSerializationCsCode() {
            TObject objTemplate;

            objTemplate = (TObject)Helper.CreateJsonTemplateFromFile("supersimple.json");
			objTemplate.CodegenInfo.ClassName = "PreGenerated";

			StdCSharpGenerator generator = new StdCSharpGenerator(new StdDomGenerator(objTemplate));
            Helper.ConsoleWriteLine(generator.GenerateCode());
		}

		[Test]
		public static void GenerateFTJSerializationCsCode() {
            TObject objTemplate;

            objTemplate = (TObject)Helper.CreateJsonTemplateFromFile("supersimple.json");
			objTemplate.CodegenInfo.ClassName = "PreGenerated";

			FTJCSharpGenerator generator = new FTJCSharpGenerator(new FTJDomGenerator(objTemplate));
            Helper.ConsoleWriteLine(generator.GenerateCode());
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

            String serString = o4.ToJson();

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

            var oldSettings = TypedJsonSerializer.DefaultSettings;
            try {
                TypedJsonSerializer.DefaultSettings = settings;

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
                TypedJsonSerializer.DefaultSettings = oldSettings;
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
        
        //[Test]
        //public static void DebugPregeneratedSerializationCode() {
        //	byte[] jsonArr;
        //	int size;
        //	int sizeAfterPopulate;
        //	string correctJson;
        //	string codegenJson;
        //	TJson tPerson;

        //	tPerson = CreateJsonTemplateFromFile("supersimple.json");
        //	var person = (Json)tPerson.CreateInstance();
        //	//SetDefaultPersonValues(person);

        //	TypedJsonSerializer serializer = new __starcountergenerated__.PreGeneratedSerializer();

        //	// First use fallback serializer to create a correct json string.
        //	TJson.UseCodegeneratedSerializer = false;
        //	TJson.FallbackSerializer = new NewtonsoftSerializer();
        //	person.PopulateFromJson(File.ReadAllText("supersimple.json"));
        //	correctJson = person.ToJson();

        //	// Then we do the same but use codegeneration. We use the pregenerated serializer here
        //	// to be able to debug it, but we will get the same result by enabling codegenerated serializer 
        //	// on the template.
        //	TJson.UseCodegeneratedSerializer = true;
        //	TJson.FallbackSerializer = DefaultSerializer.Instance;

        //	size = serializer.ToJsonUtf8(person, out jsonArr);
        //	codegenJson = Encoding.UTF8.GetString(jsonArr, 0, size);

        //	Helper.ConsoleWriteLine("Count: " + size);
        //	Helper.ConsoleWriteLine(codegenJson);

        //	AssertAreEqual(Encoding.UTF8.GetBytes(correctJson), jsonArr, size);
        //	Assert.AreEqual(correctJson, codegenJson);

        //	// Now we populate a new person instance with values from the serializer json.
        //	// And compare it to the original. All values should be identical.
        //	var person2 = (Json)tPerson.CreateInstance();
        //	sizeAfterPopulate = serializer.PopulateFromJson(person2, jsonArr, size);

        //	Assert.AreEqual(size, sizeAfterPopulate);
        //	AssertAreEqual(person, person2);
        //}
    }
}
