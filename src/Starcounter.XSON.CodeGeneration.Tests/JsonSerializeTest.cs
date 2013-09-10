using System;
using System.IO;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using NUnit.Framework;
using Starcounter.Internal.Application.CodeGeneration;
using Starcounter.Templates;
using Starcounter.Advanced.XSON;
using Modules;
using TJson = Starcounter.Templates.TObject;


namespace Starcounter.Internal.XSON.Serializer.Tests {
    /// <summary>
    /// 
    /// </summary>
    public static class JsonSerializeTest {
  //      private static NewtonsoftSerializer newtonSerializer;
        private static StandardJsonSerializer defaultSerializer;
		private static FasterThanJsonSerializer ftjSerializer;

        [TestFixtureSetUp]
        public static void InitializeTest() {
  //          newtonSerializer = new NewtonsoftSerializer();
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
		public static void TestFTJSerializer() {
			RunFTJSerializerTest("jsstyle.json", File.ReadAllText("jsstyle.json"), false);
			RunFTJSerializerTest("person.json", File.ReadAllText("person.json"), false);
			RunFTJSerializerTest("supersimple.json", File.ReadAllText("supersimple.json"), false);
			RunFTJSerializerTest("simple.json", File.ReadAllText("simple.json"), false);
			RunFTJSerializerTest("TestMessage.json", File.ReadAllText("TestMessage.json"), false);
		}

		//[Test]
		//public static void TestFTJCodegenSerializer() {
		//	RunFTJSerializerTest("jsstyle.json", File.ReadAllText("jsstyle.json"), true);
		//	RunFTJSerializerTest("person.json", File.ReadAllText("person.json"), true);
		//	RunFTJSerializerTest("supersimple.json", File.ReadAllText("supersimple.json"), true);
		//	RunFTJSerializerTest("simple.json", File.ReadAllText("simple.json"), true);
		//	RunFTJSerializerTest("TestMessage.json", File.ReadAllText("TestMessage.json"), true);
		//}

		[Test]
		public static void TestStandardSerializer() {
			RunStandardSerializerTest("jsstyle.json", File.ReadAllText("jsstyle.json"), false);
			RunStandardSerializerTest("person.json", File.ReadAllText("person.json"), false);
			RunStandardSerializerTest("supersimple.json", File.ReadAllText("supersimple.json"), false);
			RunStandardSerializerTest("simple.json", File.ReadAllText("simple.json"), false);
			RunStandardSerializerTest("TestMessage.json", File.ReadAllText("TestMessage.json"), false);
		}

		[Test]
		public static void TestStandardCodegenSerializer() {
			RunStandardSerializerTest("jsstyle.json", File.ReadAllText("jsstyle.json"), true);
			RunStandardSerializerTest("person.json", File.ReadAllText("person.json"), true);
			RunStandardSerializerTest("supersimple.json", File.ReadAllText("supersimple.json"), true);
			RunStandardSerializerTest("simple.json", File.ReadAllText("simple.json"), true);
			RunStandardSerializerTest("TestMessage.json", File.ReadAllText("TestMessage.json"), true);
		}

		private static void RunFTJSerializerTest(string name, string jsonStr, bool useCodegen) {
			byte[] ftj = null;
			int serializedSize = 0;
			int afterPopulateSize = 0;
			TObject tObj;
			Json original;
			Json newJson;

			TJson.UseCodegeneratedSerializer = useCodegen;
			TJson.DontCreateSerializerInBackground = true;

			tObj = CreateJsonTemplate(Path.GetFileNameWithoutExtension(name), jsonStr);
			original = (Json)tObj.CreateInstance();

			// using standard json serializer to populate object with values.
			original.PopulateFromJson(jsonStr);

			serializedSize = tObj.ToFasterThanJson(original, out ftj);

			unsafe {
				fixed (byte* p = ftj) {
					newJson = (Json)tObj.CreateInstance();
					afterPopulateSize = tObj.PopulateFromFasterThanJson(newJson, (IntPtr)p, serializedSize);
				}
			}

			Assert.AreEqual(serializedSize, afterPopulateSize);
			AssertAreEqual(original, newJson);
		}

		private static void RunStandardSerializerTest(string name, string jsonStr, bool useCodegen) {
			byte[] jsonArr = null;
			int serializedSize = 0;
			int afterPopulateSize = 0;
			TObject tObj;
			Json original;
			Json newJson;

			TJson.UseCodegeneratedSerializer = useCodegen;
			TJson.DontCreateSerializerInBackground = true;

			tObj = CreateJsonTemplate(Path.GetFileNameWithoutExtension(name), jsonStr);
			original = (Json)tObj.CreateInstance();

			// TODO:
			// Change to newtonsoft for verification.

			// using standard json serializer to populate object with values.
			original.PopulateFromJson(jsonStr);

			serializedSize = tObj.ToJsonUtf8(original, out jsonArr);

			unsafe {
				fixed (byte* p = jsonArr) {
					newJson = (Json)tObj.CreateInstance();
					afterPopulateSize = tObj.PopulateFromJson(newJson, (IntPtr)p, serializedSize);
				}
			}

			Assert.AreEqual(serializedSize, afterPopulateSize);
			AssertAreEqual(original, newJson);
		}

        [Test]
        public static void TestIncorrectInputJsonForDefaultSerializer() {
            TJson tObj = CreateJsonTemplateFromFile("supersimple.json");

            TJson.UseCodegeneratedSerializer = false;
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
            TJson tObj = CreateJsonTemplateFromFile("supersimple.json");

            TJson.UseCodegeneratedSerializer = true;
            TJson.DontCreateSerializerInBackground = true;
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

        private static TJson CreateJsonTemplate(string className, string json) {
            var tobj = TJson.CreateFromJson(json);
            tobj.ClassName = className;
            return tobj;
        }

		[Test]
		[Category("LongRunning")]
		public static void BenchmarkFTJSerializer() {
			int numberOfTimes = 100000;

			Console.WriteLine("Benchmarking ftj serializer, repeats: " + numberOfTimes);

			RunFTJBenchmark("jsstyle.json", File.ReadAllText("jsstyle.json"), numberOfTimes);
			RunFTJBenchmark("person.json", File.ReadAllText("person.json"), numberOfTimes);
			RunFTJBenchmark("supersimple.json", File.ReadAllText("supersimple.json"), numberOfTimes);
			RunFTJBenchmark("simple.json", File.ReadAllText("simple.json"), numberOfTimes);
			RunFTJBenchmark("TestMessage.json", File.ReadAllText("TestMessage.json"), numberOfTimes);
		}

		[Test]
		[Category("LongRunning")]
		public static void BenchmarkStandardJsonSerializer() {
			int numberOfTimes = 100000;

			Console.WriteLine("Benchmarking standard serializer, repeats: " + numberOfTimes);

			RunStandardJsonBenchmark("jsstyle.json", File.ReadAllText("jsstyle.json"), numberOfTimes, false);
			RunStandardJsonBenchmark("person.json", File.ReadAllText("person.json"), numberOfTimes, false);
			RunStandardJsonBenchmark("supersimple.json", File.ReadAllText("supersimple.json"), numberOfTimes, false);
			RunStandardJsonBenchmark("simple.json", File.ReadAllText("simple.json"), numberOfTimes, false);
			RunStandardJsonBenchmark("TestMessage.json", File.ReadAllText("TestMessage.json"), numberOfTimes, false);
		}

		[Test]
		[Category("LongRunning")]
		public static void BenchmarkStandardCodegenJsonSerializer() {
			int numberOfTimes = 100000;

			Console.WriteLine("Benchmarking standard serializer, repeats: " + numberOfTimes);

			RunStandardJsonBenchmark("jsstyle.json", File.ReadAllText("jsstyle.json"), numberOfTimes, true);
			RunStandardJsonBenchmark("person.json", File.ReadAllText("person.json"), numberOfTimes, true);
			RunStandardJsonBenchmark("supersimple.json", File.ReadAllText("supersimple.json"), numberOfTimes, true);
			RunStandardJsonBenchmark("simple.json", File.ReadAllText("simple.json"), numberOfTimes, true);
			RunStandardJsonBenchmark("TestMessage.json", File.ReadAllText("TestMessage.json"), numberOfTimes, true);
		}

		[Test]
		[Category("LongRunning")]
		public static void BenchmarkSerializers() {
			int numberOfTimes = 1000000;

			Console.WriteLine("Benchmarking serializers, repeats: " + numberOfTimes);

			RunFTJBenchmark("jsstyle.json", File.ReadAllText("jsstyle.json"), numberOfTimes);
			RunStandardJsonBenchmark("jsstyle.json", File.ReadAllText("jsstyle.json"), numberOfTimes, false);
			RunFTJBenchmark("person.json", File.ReadAllText("person.json"), numberOfTimes);
			RunStandardJsonBenchmark("person.json", File.ReadAllText("person.json"), numberOfTimes, false);
			RunFTJBenchmark("supersimple.json", File.ReadAllText("supersimple.json"), numberOfTimes);
			RunStandardJsonBenchmark("supersimple.json", File.ReadAllText("supersimple.json"), numberOfTimes, false);
			RunFTJBenchmark("simple.json", File.ReadAllText("simple.json"), numberOfTimes);
			RunStandardJsonBenchmark("simple.json", File.ReadAllText("simple.json"), numberOfTimes, false);
			RunFTJBenchmark("TestMessage.json", File.ReadAllText("TestMessage.json"), numberOfTimes);
			RunStandardJsonBenchmark("TestMessage.json", File.ReadAllText("TestMessage.json"), numberOfTimes, false);
		}

		private static void RunFTJBenchmark(string name, string json, int numberOfTimes) {
			byte[] ftj = null;
			int size = 0;
			TObject tObj;
			Json jsonInst;
			DateTime start;
			DateTime stop;

			TJson.UseCodegeneratedSerializer = false;

			Console.Write(AddSpaces(name, 20) + AddSpaces("FTJ", 10));
			tObj = CreateJsonTemplate(Path.GetFileNameWithoutExtension(name), json);
			jsonInst = (Json)tObj.CreateInstance();

			// using standard json serializer to populate object with values.
			jsonInst.PopulateFromJson(json);

			// Serializing to FTJ.
			start = DateTime.Now;
			for (int i = 0; i < numberOfTimes; i++) {
				size = tObj.ToFasterThanJson(jsonInst, out ftj);
			}
			stop = DateTime.Now;

			Console.Write(AddSpaces((stop - start).TotalMilliseconds.ToString("0.0000"), 12));

			// Deserializing from FTJ.
			unsafe {
				fixed (byte* p = ftj) {
					start = DateTime.Now;
					for (int i = 0; i < numberOfTimes; i++) {
						jsonInst = (Json)tObj.CreateInstance();
						size = tObj.PopulateFromFasterThanJson(jsonInst, (IntPtr)p, size);
					}
				}
			}
			stop = DateTime.Now;
			Console.WriteLine((stop - start).TotalMilliseconds.ToString("0.0000"));
		}

        private static void RunStandardJsonBenchmark(string name, string json, int numberOfTimes, bool useCodegen) {
			byte[] jsonArr = null;
			int size = 0;
			TObject tObj;
			Json jsonInst;
			DateTime start;
			DateTime stop;

			TJson.UseCodegeneratedSerializer = useCodegen;
			TJson.DontCreateSerializerInBackground = true;

			if (useCodegen) {
				Console.Write(AddSpaces(name, 20) + AddSpaces("STD-Codegen", 10));
			} else {
				Console.Write(AddSpaces(name, 20) + AddSpaces("STD", 10));
			}

			tObj = CreateJsonTemplate(Path.GetFileNameWithoutExtension(name), json);
			jsonInst = (Json)tObj.CreateInstance();

			// using standard json serializer to populate object with values.
			jsonInst.PopulateFromJson(json);

			// Serializing to standard json.
			start = DateTime.Now;
			for (int i = 0; i < numberOfTimes; i++) {
				size = tObj.ToJsonUtf8(jsonInst, out jsonArr);
			}
			stop = DateTime.Now;
			Console.Write(AddSpaces((stop - start).TotalMilliseconds.ToString("0.0000"), 12));

			// Deserializing from standard json.
			unsafe {
				fixed (byte* p = jsonArr) {
					start = DateTime.Now;
					for (int i = 0; i < numberOfTimes; i++) {
						size = tObj.PopulateFromJson(jsonInst, (IntPtr)p, size);
					}
				}
			}
			stop = DateTime.Now;
			Console.WriteLine((stop - start).TotalMilliseconds);
        }

		private static string AddSpaces(string str, int totalLength) {
			string after = str;
			for (int i = 0; i < (totalLength - str.Length); i++) {
				after += " ";
			}
			return after;
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
            byte[] expected;
            int count;
            int used;
            string decodedString;

            unsafe {
                fixed (byte* p = buffer) {
                    count = JsonHelper.WriteString((IntPtr)p, buffer.Length, value);
                    JsonHelper.ParseString((IntPtr)p, buffer.Length, out decodedString, out used);
                }
            }
            expected = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value));
            AssertAreEqual(expected, buffer, count);

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
        public static void GenerateSerializationParseTreeOverview() {
            TJson objTemplate;
            objTemplate = CreateJsonTemplateFromFile("person.json");
            ParseNode parseTree = ParseTreeGenerator.BuildParseTree(objTemplate);
            Console.WriteLine(parseTree.ToString());
        }

        [Test]
        public static void GenerateSerializationAstTreeOverview() {
            TJson objTemplate;
            objTemplate = CreateJsonTemplateFromFile("person.json");
            Console.WriteLine(AstTreeGenerator.BuildAstTree(objTemplate).ToString());
        }

        /// <summary>
        /// Creates a template from a JSON-by-example file
        /// </summary>
        /// <param name="filePath">The file to load</param>
        /// <returns>The newly created template</returns>
        private static TJson CreateJsonTemplateFromFile( string filePath ) {
            string json = File.ReadAllText(filePath);
            string className = Path.GetFileNameWithoutExtension(filePath);
            var tobj = TJson.CreateFromMarkup<Json, TJson>("json", json, className);
            tobj.ClassName = className;
            return tobj;
        }

        [Test]
        public static void GenerateSerializationCsCode() {
            TJson objTemplate;

            objTemplate = CreateJsonTemplateFromFile("supersimple.json");
            objTemplate.ClassName = "PreGenerated";
            Console.WriteLine(AstTreeGenerator.BuildAstTree(objTemplate, false).GenerateCsSourceCode());
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

        private static void AssertAreEqual(Json expected, Json actual) {
            TJson tExpected = (TJson)expected.Template;
            TJson tActual = (TJson)actual.Template;

            // We assume that the instances used the same Template.
            Assert.AreEqual(tExpected, tActual);
            foreach (Template child in tExpected.Properties) {
                if (child is TBool)
                    Assert.AreEqual(expected.Get((TBool)child), actual.Get((TBool)child));
                else if (child is Property<decimal>)
                    Assert.AreEqual(expected.Get((Property<decimal>)child), actual.Get((Property<decimal>)child));
                else if (child is Property<double>)
                    Assert.AreEqual(expected.Get((TDouble)child), actual.Get((TDouble)child));
                else if (child is Property<long>)
                    Assert.AreEqual(expected.Get((TLong)child), actual.Get((TLong)child));
                else if (child is Property<string>)
                    Assert.AreEqual(expected.Get((TString)child), actual.Get((TString)child));
                else if (child is TJson)
                    AssertAreEqual(expected.Get((TJson)child), actual.Get((TJson)child));
                else if (child is TObjArr) {
                    var arr1 = expected.Get((TArray<Json>)child);
                    var arr2 = actual.Get((TArray<Json>)child);
                    Assert.AreEqual(arr1.Count, arr2.Count);
                    for (int i = 0; i < arr1.Count; i++) {
                        AssertAreEqual(arr1[i], arr2[i]);
                    }
                } else
                    throw new NotSupportedException();
            }
        }

        private static void SetDefaultPersonValues(dynamic person) {
            person.FirstName = "Arne";
            person.LastName = "Anka";
            person.Age = 19;
            person.Stats = 39.4567m;
            person.ExtraInfo.Text = "hi ha ho he";

            var field = person.Fields.Add();
            field.Type = "Phone";
            field.Info.Text = "123-555-7890";

            field = person.Fields.Add();
            field.Type = "Email";
            field.Info.Text = "arneanka@gmail.com";
        }
    }
}
