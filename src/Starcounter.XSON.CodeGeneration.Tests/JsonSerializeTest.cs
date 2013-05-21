using System;
using System.IO;
using System.Text;
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
        [TestFixtureSetUp]
        public static void InitializeTest() {
            Obj.Factory = new TypedJsonFactory();
        }

        [Test]
        public static void TestDefaultSerializer() {
            byte[] correctJson;
            byte[] defaultJson;
            TObj tPerson;

            var newtonSerializer = new NewtonsoftSerializer();
            var defaultSerializer = DefaultSerializer.Instance;

            tPerson = Obj.Factory.CreateJsonTemplateFromFile("person.json");
            tPerson.UseCodegeneratedSerializer = false;

            dynamic person = tPerson.CreateInstance();
            SetDefaultPersonValues(person);

            // We use the NewtonSoft implementation as a verifier for correct input and output.
            TObj.FallbackSerializer = newtonSerializer;
            correctJson = person.ToJsonUtf8();

            TObj.FallbackSerializer = defaultSerializer;
            int count = person.ToJsonUtf8(out defaultJson);

            AssertAreEqual(correctJson, defaultJson, count);

            Console.WriteLine(Encoding.UTF8.GetString(correctJson));
        }

        [Test]
        public static void BenchmarkSerializers() {
            string newtonJson;
            byte[] defaultJson;
            TObj tPerson;

            DateTime start;
            DateTime stop;
            int amount = 100000;

            var newtonSerializer = new NewtonsoftSerializer();
            var defaultSerializer = DefaultSerializer.Instance;

            tPerson = Obj.Factory.CreateJsonTemplateFromFile("person.json");
            tPerson.UseCodegeneratedSerializer = false;

            dynamic person = tPerson.CreateInstance();
            SetDefaultPersonValues(person);

            // We use the NewtonSoft implementation as a verifier for correct input and output.
            TObj.FallbackSerializer = newtonSerializer;

            // Running once to make sure everything is initialized.
            newtonJson = person.ToJson();
            Console.WriteLine("JSON: " + newtonJson);
            Console.WriteLine();

            start = DateTime.Now;
            for (int i = 0; i < amount; i++) {
                newtonJson = person.ToJson();
            }
            stop = DateTime.Now;

            Console.WriteLine("Serializing " + amount + " number of times.");
            Console.WriteLine("NewtonSoft:" + (stop-start).TotalMilliseconds + " ms.");

            TObj.FallbackSerializer = defaultSerializer;
            person.ToJsonUtf8(out defaultJson);

            start = DateTime.Now;
            for (int i = 0; i < amount; i++) {
                person.ToJsonUtf8(out defaultJson);
            }
            stop = DateTime.Now;

            Console.WriteLine("Default:" + (stop - start).TotalMilliseconds + " ms.");

            tPerson.UseCodegeneratedSerializer = true;
            person.ToJsonUtf8(out defaultJson);

            start = DateTime.Now;
            for (int i = 0; i < amount; i++) {
                person.ToJsonUtf8(out defaultJson);
            }
            stop = DateTime.Now;

            Console.WriteLine("Codegenerated:" + (stop - start).TotalMilliseconds + " ms.");


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

            objTemplate = (TObj)Obj.Factory.CreateJsonTemplate(File.ReadAllText("person.json"));
            objTemplate.ClassName = "PreGenerated";
            Console.WriteLine(AstTreeGenerator.BuildAstTree(objTemplate).GenerateCsSourceCode());
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
            dynamic person = (Json)tPerson.CreateInstance(null);
            SetDefaultPersonValues(person);

            byte[] buffer = new byte[4096];
            TypedJsonSerializer serializer = new __starcountergenerated__.PreGeneratedSerializer();

            // First use fallback serializer (Newtonsoft) to create a correct json string.
            tPerson.UseCodegeneratedSerializer = false;
            correctJson = person.ToJson();

            // Then we do the same but use codegeneration. We use the pregenerated serializer here
            // to be able to debug it, but we will get the same result by enabling codegenerated serializer 
            // on the template.
            tPerson.UseCodegeneratedSerializer = false;
            jsonArr = new byte[4096];

            unsafe {
                fixed (byte* p = jsonArr) {
                    size = serializer.ToJson(person, (IntPtr)p, jsonArr.Length);
//                    size = person.ToJson((IntPtr)p, jsonArr.Length);
                }
            }
            codegenJson = Encoding.UTF8.GetString(jsonArr, 0, size);
            Assert.AreEqual(correctJson, codegenJson);

            // Now we populate a new person instance with values from the serializer json.
            // And compare it to the original.
            // All values should be identical.
            //jsonArr = File.ReadAllBytes("person.json");
            dynamic person2 = tPerson.CreateInstance(null);
            unsafe {
                fixed (byte* p = jsonArr) {
                    sizeAfterPopulate = serializer.PopulateFromJson(person2, (IntPtr)p, size);
//                    sizeAfterPopulate = person2.PopulateFromJson((IntPtr)p, size);
                }
            }

            Assert.AreEqual(size, sizeAfterPopulate);
            AssertAreEqualPersons(person, person2);
        }

        private static void AssertAreEqualPersons(dynamic p1, dynamic p2) {
            Assert.AreEqual(p1.FirstName, p2.FirstName);
            Assert.AreEqual(p1.LastName, p2.LastName);
            Assert.AreEqual(p1.Age, p2.Age);
            Assert.AreEqual(p1.Stats, p2.Stats);
            Assert.AreEqual(p1.ExtraInfo.Text, p2.ExtraInfo.Text);

            Assert.AreEqual(p1.Fields.Count, p2.Fields.Count);
            Assert.AreEqual(p1.Fields[0].Type, p2.Fields[0].Type);
            Assert.AreEqual(p1.Fields[0].Info.Text, p2.Fields[0].Info.Text);

            Assert.AreEqual(p1.Fields[1].Type, p2.Fields[1].Type);
            Assert.AreEqual(p1.Fields[1].Info.Text, p2.Fields[1].Info.Text);
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
