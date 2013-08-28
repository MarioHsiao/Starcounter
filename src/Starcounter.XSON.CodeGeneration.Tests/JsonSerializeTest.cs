﻿using System;
using System.IO;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using NUnit.Framework;
using Starcounter.Internal.Application.CodeGeneration;
using Starcounter.Templates;
using Starcounter.Advanced.XSON;
using Modules;
using TJson = Starcounter.Templates.Schema<Starcounter.Json<object>>;


namespace Starcounter.Internal.XSON.Serializer.Tests {
    /// <summary>
    /// 
    /// </summary>
    public static class JsonSerializeTest {
        private static NewtonsoftSerializer newtonSerializer;
        private static DefaultSerializer defaultSerializer;

        [TestFixtureSetUp]
        public static void InitializeTest() {

            newtonSerializer = new NewtonsoftSerializer();
            defaultSerializer = new DefaultSerializer();
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
        public static void TestAllSerializers() {
            TestDefaultSerializer();
            TestCodegenSerializer();
        }

        [Test]
        public static void TestDefaultSerializer() {
            TestSerializationFor("jsstyle.json", File.ReadAllText("jsstyle.json"));
            TestSerializationFor("person.json", File.ReadAllText("person.json"));
            TestSerializationFor("supersimple.json", File.ReadAllText("supersimple.json"));
            TestSerializationFor("simple.json", File.ReadAllText("simple.json"));
            TestSerializationFor("TestMessage.json", File.ReadAllText("TestMessage.json"));
        }

        [Test]
        public static void TestCodegenSerializer() {
            TestSerializationFor("jsstyle.json", File.ReadAllText("jsstyle.json"), true);
            TestSerializationFor("person.json", File.ReadAllText("person.json"), true);
            TestSerializationFor("supersimple.json", File.ReadAllText("supersimple.json"), true);
            TestSerializationFor("simple.json", File.ReadAllText("simple.json"), true);
            TestSerializationFor("TestMessage.json", File.ReadAllText("TestMessage.json"), true);
        }

        [Test]
        public static void TestIncorrectInputJsonForDefaultSerializer() {
            TJson tObj = CreateJsonTemplateFromFile("supersimple.json");

            TJson.UseCodegeneratedSerializer = false;
            var obj = (Json<object>)tObj.CreateInstance();

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
        public static void TestIncorrectInputJsonForCodegenSerializer() {
            TJson tObj = CreateJsonTemplateFromFile("supersimple.json");

            TJson.UseCodegeneratedSerializer = true;
            TJson.DontCreateSerializerInBackground = true;
            var obj = (Json<object>)tObj.CreateInstance();

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

        private static void TestSerializationFor(string name, string json, bool useCodegen = false) {
            byte[] correctJson;
            byte[] defaultJson;
            int count;
            Json<object> correctObj;
            Json<object> actualObj;
            TJson tObj;
            string serializerName = "default serializer";
            if (useCodegen)
                serializerName = "codegenerated serializer";

            Console.WriteLine("Testing serialization/deserialization for '" + name + "' with " + serializerName);

            tObj = CreateJsonTemplate(Path.GetFileNameWithoutExtension(name), json);
            TJson.UseCodegeneratedSerializer = false;
            correctObj = (Json<object>)tObj.CreateInstance();

            // We use the NewtonSoft implementation as a verifier for correct input and output.
            TJson.FallbackSerializer = newtonSerializer;
            correctObj.PopulateFromJson(json);
            correctJson = correctObj.ToJsonUtf8();

            TJson.FallbackSerializer = defaultSerializer;
            TJson.UseCodegeneratedSerializer = useCodegen;
            TJson.DontCreateSerializerInBackground = true;
            count = correctObj.ToJsonUtf8(out defaultJson);
            AssertAreEqual(correctJson, defaultJson, count);

            actualObj = (Json<object>)tObj.CreateInstance();

//            count = actualObj.PopulateFromJson(correctJson, correctJson.Length);
            actualObj.PopulateFromJson(json);

            Assert.AreEqual(correctJson.Length, count);

            AssertAreEqual(correctObj, actualObj);

            Console.WriteLine("Done.");
        }

        [Test]
        [Category("LongRunning")]
        public static void BenchmarkDefaultSerializer() {
            BenchmarkSerializers(File.ReadAllText("jsstyle.json"), true, true, false);
            BenchmarkSerializers(File.ReadAllText("supersimple.json"), true, true, false);
        }

        [Test]
        [Category("LongRunning")]
        public static void BenchmarkCodegenSerializer() {
            BenchmarkSerializers(File.ReadAllText("jsstyle.json"), true, false, true);
            BenchmarkSerializers(File.ReadAllText("supersimple.json"), true, false, true);
        }

        [Test]
        [Category("LongRunning")]
        public static void BenchmarkAllSerializers() {
            BenchmarkSerializers(File.ReadAllText("jsstyle.json"), true, true, true);
            BenchmarkSerializers(File.ReadAllText("supersimple.json"), true, true, true);
        }

        private static void BenchmarkSerializers(string json, bool testNewton, bool testDefault, bool testCodegen) {
            int count = 0;
            string newtonJson;
            byte[] jsonUtf8;
            byte[] outJsonArr;
            TJson tObj;
            double newtonTime;
            double defaultTime;
            double codegenTime;
            int nrOfTimes = 10000;

            Console.WriteLine(json);
            Console.WriteLine();

            var newtonSerializer = new NewtonsoftSerializer();
            var defaultSerializer = DefaultSerializer.Instance;

            tObj = CreateJsonTemplate(null, json);
            TJson.UseCodegeneratedSerializer = false;

            dynamic obj = tObj.CreateInstance();
            TJson.FallbackSerializer = newtonSerializer;
            obj.PopulateFromJson(json);

            jsonUtf8 = System.Text.Encoding.UTF8.GetBytes(json);
            count = jsonUtf8.Length;
            
            Console.WriteLine("Serializing " + nrOfTimes + " number of times.");
            if (testNewton) {
                TJson.FallbackSerializer = newtonSerializer;
                newtonJson = obj.ToJson();
                newtonTime = BenchmarkSerializer(obj, nrOfTimes);
                Console.WriteLine("NewtonSoft:" + newtonTime + " ms.");
            }

            if (testDefault) {
                TJson.FallbackSerializer = defaultSerializer;
                count = obj.ToJsonUtf8(out outJsonArr);
                defaultTime = BenchmarkSerializer(obj, nrOfTimes);
                Console.WriteLine("Default:" + defaultTime + " ms.");
            }

            if (testCodegen) {
                //            TJson<Json<object>>.FallbackSerializer = new __starcountergenerated__.PreGeneratedSerializer();
                TJson.UseCodegeneratedSerializer = true;
                count = obj.ToJsonUtf8(out outJsonArr); // Run once to start the codegen.
                Thread.Sleep(1000);
                count = obj.ToJsonUtf8(out outJsonArr); // And then again to make sure everything is initialized.
                codegenTime = BenchmarkSerializer(obj, nrOfTimes);
                Console.WriteLine("Codegenerated:" + codegenTime + " ms.");
            }
            Console.WriteLine();

            Console.WriteLine("Deserializing " + nrOfTimes + " number of times.");

            if (testNewton) {
                TJson.UseCodegeneratedSerializer = false;
                TJson.FallbackSerializer = newtonSerializer;
                obj.PopulateFromJson(json);
                newtonTime = BenchmarkDeserializer(obj, jsonUtf8, jsonUtf8.Length, nrOfTimes);
                Console.WriteLine("NewtonSoft:" + newtonTime + " ms.");
            }

            if (testDefault) {
                TJson.FallbackSerializer = defaultSerializer;
                obj.PopulateFromJson(jsonUtf8, jsonUtf8.Length);
                defaultTime = BenchmarkDeserializer(obj, jsonUtf8, jsonUtf8.Length, nrOfTimes);
                Console.WriteLine("Default:" + defaultTime + " ms.");
            }

            if (testCodegen) {
                TJson.UseCodegeneratedSerializer = true;
                obj.PopulateFromJson(jsonUtf8, jsonUtf8.Length);
                codegenTime = BenchmarkDeserializer(obj, jsonUtf8, jsonUtf8.Length, nrOfTimes);
                Console.WriteLine("Codegenerated:" + codegenTime + " ms.");
            }

            Console.WriteLine();
        }

        private static double BenchmarkSerializer(Json<object> person, int nrOfTimes) {
            DateTime start;
            DateTime stop;

            start = DateTime.Now;
            for (int i = 0; i < nrOfTimes; i++) {
                var apa = person.ToJson();
            }
            stop = DateTime.Now;
            return (stop - start).TotalMilliseconds;
        }

        private static double BenchmarkDeserializer(Json<object> obj, byte[] json, int jsonSize, int nrOfTimes) {
            DateTime start;
            DateTime stop;

            start = DateTime.Now;
            for (int i = 0; i < nrOfTimes; i++) {
                obj.PopulateFromJson(json, jsonSize);
            }
            stop = DateTime.Now;
            return (stop - start).TotalMilliseconds;
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
            var tobj = TJson.CreateFromMarkup<Json<object>, TJson>("json", json, className);
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

        [Test]
        public static void DebugPregeneratedSerializationCode() {
            byte[] jsonArr;
            int size;
            int sizeAfterPopulate;
            string correctJson;
            string codegenJson;
            TJson tPerson;

            tPerson = CreateJsonTemplateFromFile("supersimple.json");
            var person = (Json<object>)tPerson.CreateInstance();
            //SetDefaultPersonValues(person);

            TypedJsonSerializer serializer = new __starcountergenerated__.PreGeneratedSerializer();

            // First use fallback serializer to create a correct json string.
            TJson.UseCodegeneratedSerializer = false;
            TJson.FallbackSerializer = new NewtonsoftSerializer();
            person.PopulateFromJson(File.ReadAllText("supersimple.json"));
            correctJson = person.ToJson();

            // Then we do the same but use codegeneration. We use the pregenerated serializer here
            // to be able to debug it, but we will get the same result by enabling codegenerated serializer 
            // on the template.
            TJson.UseCodegeneratedSerializer = true;
            TJson.FallbackSerializer = DefaultSerializer.Instance;

            size = serializer.ToJsonUtf8(person, out jsonArr);
            codegenJson = Encoding.UTF8.GetString(jsonArr, 0, size);

            Console.WriteLine("Count: " + size);
            Console.WriteLine(codegenJson);

            AssertAreEqual(Encoding.UTF8.GetBytes(correctJson), jsonArr, size);
            Assert.AreEqual(correctJson, codegenJson);

            // Now we populate a new person instance with values from the serializer json.
            // And compare it to the original. All values should be identical.
            var person2 = (Json<object>)tPerson.CreateInstance();
            sizeAfterPopulate = serializer.PopulateFromJson(person2, jsonArr, size);

            Assert.AreEqual(size, sizeAfterPopulate);
            AssertAreEqual(person, person2);
        }

        private static void AssertAreEqual(Json<object> expected, Json<object> actual) {
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
                    var arr1 = expected.Get((ArrSchema<Json<object>>)child);
                    var arr2 = actual.Get((ArrSchema<Json<object>>)child);
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
