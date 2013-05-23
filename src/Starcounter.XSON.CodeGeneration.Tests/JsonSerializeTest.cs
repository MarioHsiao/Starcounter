using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using NUnit.Framework;
using Starcounter.Internal;
using Starcounter.Internal.Application.CodeGeneration;
using Starcounter.Templates;
using Starcounter.XSON.CodeGeneration;
using Starcounter.XSON.Serializers;

namespace Starcounter.Apps.CodeGeneration.Tests {
    /// <summary>
    /// 
    /// </summary>
    public static class JsonSerializeTest {
        private static NewtonsoftSerializer newtonSerializer;
        private static DefaultSerializer defaultSerializer;

        [TestFixtureSetUp]
        public static void InitializeTest() {
            Obj.Factory = new TypedJsonFactory();
            newtonSerializer = new NewtonsoftSerializer();
            defaultSerializer = new DefaultSerializer();
        }

        [Test]
        public static void DefaultSerializerTest() {
            TestSerializationFor("person.json");
            TestSerializationFor("supersimple.json");
            TestSerializationFor("TestMessage.json");
        }

        private static void TestSerializationFor(string jsonFilePath) {
            byte[] correctJson;
            byte[] defaultJson;
            int count;
            Obj correctObj;
            Obj actualObj;
            TObj tObj;

            tObj = Obj.Factory.CreateJsonTemplateFromFile(jsonFilePath);
            tObj.UseCodegeneratedSerializer = false;
            correctObj = (Obj)tObj.CreateInstance();

            // We use the NewtonSoft implementation as a verifier for correct input and output.
            TObj.FallbackSerializer = newtonSerializer;
            correctObj.PopulateFromJson(File.ReadAllText(jsonFilePath));
            correctJson = correctObj.ToJsonUtf8();

            TObj.FallbackSerializer = defaultSerializer;
            count = correctObj.ToJsonUtf8(out defaultJson);
            AssertAreEqual(correctJson, defaultJson, count);

            actualObj = (Obj)tObj.CreateInstance();
            count = actualObj.PopulateFromJson(correctJson, correctJson.Length);
            Assert.AreEqual(correctJson.Length, count);

            AssertAreEqual(correctObj, actualObj);
        }

        [Test]
        public static void BenchmarkSerializers() {
            int count;
            string newtonJson;
            byte[] defaultJson;
            TObj tPerson;
            double newtonTime;
            double defaultTime;
            double codegenTime;
            int nrOfTimes = 1000000;

            var newtonSerializer = new NewtonsoftSerializer();
            var defaultSerializer = DefaultSerializer.Instance;

            tPerson = Obj.Factory.CreateJsonTemplateFromFile("supersimple.json");
            tPerson.UseCodegeneratedSerializer = false;

            dynamic person = tPerson.CreateInstance();
            person.PlayerId = 35684;
//            var account = person.Accounts.Add();
//            account.AccountId = 35684;
//            SetDefaultPersonValues(person);

            TObj.FallbackSerializer = newtonSerializer;
            newtonJson = person.ToJson(); 
            newtonTime = BenchmarkSerializer(person, nrOfTimes);

            TObj.FallbackSerializer = defaultSerializer;
            count = person.ToJsonUtf8(out defaultJson);
            defaultTime = BenchmarkSerializer(person, nrOfTimes);

//            TObj.FallbackSerializer = new __starcountergenerated__.PreGeneratedSerializer();
            tPerson.UseCodegeneratedSerializer = true;
            count = person.ToJsonUtf8(out defaultJson); // Run once to start the codegen.
            Thread.Sleep(1000);
            count = person.ToJsonUtf8(out defaultJson); // And then again to make sure everything is initialized.
            codegenTime = BenchmarkSerializer(person, nrOfTimes);            

            Console.WriteLine("Serializing " + nrOfTimes + " number of times.");
            Console.WriteLine("NewtonSoft:" + newtonTime + " ms.");
            Console.WriteLine("Default:" + defaultTime + " ms.");
            Console.WriteLine("Codegenerated:" + codegenTime + " ms.");
            Console.WriteLine();

            Console.WriteLine("Count : " + count);
            Console.WriteLine(Encoding.UTF8.GetString(defaultJson, 0, count));

            tPerson.UseCodegeneratedSerializer = false;
            TObj.FallbackSerializer = newtonSerializer;
            person.PopulateFromJson(newtonJson);
            newtonTime = BenchmarkDeserializer(person, defaultJson, count, nrOfTimes);

            TObj.FallbackSerializer = defaultSerializer;
            person.PopulateFromJson(defaultJson, count);
            defaultTime = BenchmarkDeserializer(person, defaultJson, count, nrOfTimes);

            tPerson.UseCodegeneratedSerializer = true;
            person.PopulateFromJson(defaultJson, count);
            codegenTime = BenchmarkDeserializer(person, defaultJson, count, nrOfTimes);

            Console.WriteLine("Deserializing " + nrOfTimes + " number of times.");
            Console.WriteLine("NewtonSoft:" + newtonTime + " ms.");
            Console.WriteLine("Default:" + defaultTime + " ms.");
            Console.WriteLine("Codegenerated:" + codegenTime + " ms.");

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine(newtonJson);

        }

        private static double BenchmarkSerializer(Obj person, int nrOfTimes) {
            DateTime start;
            DateTime stop;

            start = DateTime.Now;
            for (int i = 0; i < nrOfTimes; i++) {
                var apa = person.ToJson();
            }
            stop = DateTime.Now;
            return (stop - start).TotalMilliseconds;
        }

        private static double BenchmarkDeserializer(Obj person, byte[] json, int jsonSize, int nrOfTimes) {
            DateTime start;
            DateTime stop;

            start = DateTime.Now;
            for (int i = 0; i < nrOfTimes; i++) {
                person.PopulateFromJson(json, jsonSize);
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
            TObj objTemplate;


            objTemplate = (TObj)Obj.Factory.CreateJsonTemplate(File.ReadAllText("person.json"));

            ParseNode parseTree = ParseTreeGenerator.BuildParseTree(objTemplate);
            Console.WriteLine(parseTree.ToString());
        }

        [Test]
        public static void GenerateSerializationAstTreeOverview() {
            TObj objTemplate;

            objTemplate = (TObj)Obj.Factory.CreateJsonTemplate(File.ReadAllText("person.json"));
            
            Console.WriteLine(AstTreeGenerator.BuildAstTree(objTemplate).ToString());
        }

        [Test]
        public static void GenerateSerializationCsCode() {
            TObj objTemplate;

            objTemplate = (TObj)Obj.Factory.CreateJsonTemplate(File.ReadAllText("supersimple.json"));
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
            TObj tPerson;

            tPerson = (TObj)Obj.Factory.CreateJsonTemplateFromFile("person.json");
            Obj person = (Obj)tPerson.CreateInstance();
            SetDefaultPersonValues(person);

            TypedJsonSerializer serializer = new __starcountergenerated__.PreGeneratedSerializer();

            // First use fallback serializer (Newtonsoft) to create a correct json string.
            tPerson.UseCodegeneratedSerializer = false;
            correctJson = person.ToJson();

            // Then we do the same but use codegeneration. We use the pregenerated serializer here
            // to be able to debug it, but we will get the same result by enabling codegenerated serializer 
            // on the template.
            tPerson.UseCodegeneratedSerializer = true;

            size = serializer.ToJsonUtf8(person, out jsonArr);
//            size = person.ToJsonUtf8(out jsonArr);

            codegenJson = Encoding.UTF8.GetString(jsonArr, 0, size);

            Console.WriteLine("Count: " + size);
            Console.WriteLine(codegenJson);

            AssertAreEqual(Encoding.UTF8.GetBytes(correctJson), jsonArr, size);
            Assert.AreEqual(correctJson, codegenJson);

            // Now we populate a new person instance with values from the serializer json.
            // And compare it to the original. All values should be identical.
            Obj person2 = (Obj)tPerson.CreateInstance();
            sizeAfterPopulate = person2.PopulateFromJson(jsonArr, size);

            Assert.AreEqual(size, sizeAfterPopulate);
            AssertAreEqual(person, person2);
        }

        private static void AssertAreEqual(Obj expected, Obj actual) {
            TObj tExpected = expected.Template;
            TObj tActual = actual.Template;

            // We assume that the instances used the same Template.
            Assert.AreEqual(tExpected, tActual);
            foreach (Template child in tExpected.Properties) {
                if (child is TBool)
                    Assert.AreEqual(expected.Get((TBool)child), actual.Get((TBool)child));
                else if (child is TDecimal)
                    Assert.AreEqual(expected.Get((TDecimal)child), actual.Get((TDecimal)child));
                else if (child is TDouble)
                    Assert.AreEqual(expected.Get((TDouble)child), actual.Get((TDouble)child));
                else if (child is TLong)
                    Assert.AreEqual(expected.Get((TLong)child), actual.Get((TLong)child));
                else if (child is TString)
                    Assert.AreEqual(expected.Get((TString)child), actual.Get((TString)child));
                else if (child is TObj)
                    AssertAreEqual(expected.Get((TObj)child), actual.Get((TObj)child));
                else if (child is TObjArr) {
                    var arr1 = expected.Get((TObjArr)child);
                    var arr2 = actual.Get((TObjArr)child);
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
