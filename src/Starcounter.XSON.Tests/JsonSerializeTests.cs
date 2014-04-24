using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Starcounter.Advanced.XSON;
using Starcounter.Templates;
using Starcounter.XSON.Serializer;
using Starcounter.XSON.Serializer.Parsetree;

namespace Starcounter.Internal.XSON.Tests {
    /// <summary>
    /// 
    /// </summary>
    public static class JsonSerializeTests {
        private static StandardJsonSerializer defaultSerializer;
		private static FasterThanJsonSerializer ftjSerializer;

        [TestFixtureSetUp]
        public static void InitializeTest() {
			defaultSerializer = new StandardJsonSerializer();
			ftjSerializer = new FasterThanJsonSerializer();
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
            RunFTJSerializerTest("jsstyle.json", File.ReadAllText("Input\\jsstyle.json"), false);
            RunFTJSerializerTest("person.json", File.ReadAllText("Input\\person.json"), false);
            RunFTJSerializerTest("supersimple.json", File.ReadAllText("Input\\supersimple.json"), false);
            RunFTJSerializerTest("simple.json", File.ReadAllText("Input\\simple.json"), false);
            RunFTJSerializerTest("TestMessage.json", File.ReadAllText("Input\\TestMessage.json"), false);
		}

		[Test]
        [Ignore("Requires fixing FTJ serializer")]
		public static void TestFTJCodegenSerializer() {
            RunFTJSerializerTest("jsstyle.json", File.ReadAllText("Input\\jsstyle.json"), true);
            RunFTJSerializerTest("person.json", File.ReadAllText("Input\\person.json"), true);
            RunFTJSerializerTest("supersimple.json", File.ReadAllText("Input\\supersimple.json"), true);
            RunFTJSerializerTest("simple.json", File.ReadAllText("Input\\simple.json"), true);
            RunFTJSerializerTest("TestMessage.json", File.ReadAllText("Input\\TestMessage.json"), true);
		}

		[Test]
		public static void TestStandardSerializer() {
            RunStandardSerializerTest("jsstyle.json", File.ReadAllText("Input\\jsstyle.json"), false);
            RunStandardSerializerTest("person.json", File.ReadAllText("Input\\person.json"), false);
            RunStandardSerializerTest("supersimple.json", File.ReadAllText("Input\\supersimple.json"), false);
            RunStandardSerializerTest("simple.json", File.ReadAllText("Input\\simple.json"), false);
            RunStandardSerializerTest("TestMessage.json", File.ReadAllText("Input\\TestMessage.json"), false);
            RunStandardSerializerTest("JsonWithFiller.json", File.ReadAllText("Input\\JsonWithFiller.json"), false);
		}

		[Test]
		public static void TestStandardCodegenSerializer() {
			RunStandardSerializerTest("jsstyle.json", File.ReadAllText("Input\\jsstyle.json"), true);
            RunStandardSerializerTest("person.json", File.ReadAllText("Input\\person.json"), true);
            RunStandardSerializerTest("supersimple.json", File.ReadAllText("Input\\supersimple.json"), true);
            RunStandardSerializerTest("simple.json", File.ReadAllText("Input\\simple.json"), true);
            RunStandardSerializerTest("TestMessage.json", File.ReadAllText("Input\\TestMessage.json"), true);
            RunStandardSerializerTest("JsonWithFiller.json", File.ReadAllText("Input\\JsonWithFiller.json"), true);
		}

		private static void RunFTJSerializerTest(string name, string jsonStr, bool useCodegen) {
			int serializedSize = 0;
			int afterPopulateSize = 0;
			TObject tObj;
			Json original;
			Json newJson;

			TObject.UseCodegeneratedSerializer = false;
			
			tObj = Helper.CreateJsonTemplateFromContent(Path.GetFileNameWithoutExtension(name), jsonStr);
			original = (Json)tObj.CreateInstance();

			// using standard json serializer to populate object with values.
			original.PopulateFromJson(jsonStr);

			TObject.UseCodegeneratedSerializer = useCodegen;
			TObject.DontCreateSerializerInBackground = true;

            byte[] ftj = new byte[tObj.JsonSerializer.EstimateSizeBytes(original)];
			serializedSize = tObj.ToFasterThanJson(original, ftj, 0);

			unsafe {
				fixed (byte* p = ftj) {
					newJson = (Json)tObj.CreateInstance();
					afterPopulateSize = tObj.PopulateFromFasterThanJson(newJson, (IntPtr)p, serializedSize);
				}
			}

			Assert.AreEqual(serializedSize, afterPopulateSize);
			Helper.AssertAreEqual(original, newJson);
		}

		private static void RunStandardSerializerTest(string name, string jsonStr, bool useCodegen) {
			int serializedSize = 0;
			int afterPopulateSize = 0;
			TObject tObj;
			Json original;
			Json newJson;

            TObject.UseCodegeneratedSerializer = false;
			
			tObj = Helper.CreateJsonTemplateFromContent(Path.GetFileNameWithoutExtension(name), jsonStr);
			original = (Json)tObj.CreateInstance();

			// TODO:
			// Change to newtonsoft for verification.

			// using standard json serializer to populate object with values.
			original.PopulateFromJson(jsonStr);

            TObject.UseCodegeneratedSerializer = useCodegen;
            TObject.DontCreateSerializerInBackground = true;

            byte[] jsonArr = new byte[tObj.JsonSerializer.EstimateSizeBytes(original)];
			serializedSize = tObj.ToJsonUtf8(original, jsonArr, 0);

			unsafe {
				fixed (byte* p = jsonArr) {
					newJson = (Json)tObj.CreateInstance();
					afterPopulateSize = tObj.PopulateFromJson(newJson, (IntPtr)p, serializedSize);
				}
			}

			Assert.AreEqual(serializedSize, afterPopulateSize);
            Helper.AssertAreEqual(original, newJson);
		}

        [Test]
        public static void TestIncorrectInputJsonForDefaultSerializer() {
            TObject tObj = Helper.CreateJsonTemplateFromFile("supersimple.json");

            TObject.UseCodegeneratedSerializer = false;
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

 //       [Test]
        public static void TestIncorrectInputJsonForCodegenSerializer() {
            TObject tObj = Helper.CreateJsonTemplateFromFile("supersimple.json");

            TObject.UseCodegeneratedSerializer = true;
            TObject.DontCreateSerializerInBackground = true;
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
            TObject objTemplate;
            objTemplate = Helper.CreateJsonTemplateFromFile("person.json");
            ParseNode parseTree = ParseTreeGenerator.BuildParseTree(objTemplate);
            Console.WriteLine(parseTree.ToString());
        }

        [Test]
        public static void GenerateStdSerializationAstTreeOverview() {
            TObject objTemplate;
            objTemplate = Helper.CreateJsonTemplateFromFile("person.json");

			StdDomGenerator domGenerator = new StdDomGenerator(objTemplate);
            Console.WriteLine(domGenerator.GenerateDomTree().ToString(true));
        }

		[Test]
		public static void GenerateFTJSerializationAstTreeOverview() {
            TObject objTemplate;
            objTemplate = Helper.CreateJsonTemplateFromFile("person.json");

			FTJDomGenerator domGenerator = new FTJDomGenerator(objTemplate);
			Console.WriteLine(domGenerator.GenerateDomTree().ToString(true));
		}

		[Test]
		public static void GenerateStdSerializationCsCode() {
            TObject objTemplate;

            objTemplate = Helper.CreateJsonTemplateFromFile("supersimple.json");
			objTemplate.ClassName = "PreGenerated";

			StdCSharpGenerator generator = new StdCSharpGenerator(new StdDomGenerator(objTemplate));
			Console.WriteLine(generator.GenerateCode());
		}

		[Test]
		public static void GenerateFTJSerializationCsCode() {
            TObject objTemplate;

            objTemplate = Helper.CreateJsonTemplateFromFile("supersimple.json");
			objTemplate.ClassName = "PreGenerated";

			FTJCSharpGenerator generator = new FTJCSharpGenerator(new FTJDomGenerator(objTemplate));
			Console.WriteLine(generator.GenerateCode());
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

		//	Console.WriteLine("Count: " + size);
		//	Console.WriteLine(codegenJson);

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
