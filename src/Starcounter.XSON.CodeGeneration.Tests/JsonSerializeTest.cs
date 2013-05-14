using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using Starcounter.Internal;
using Starcounter.Internal.Application.CodeGeneration;
using Starcounter.Templates;
using Starcounter.XSON.CodeGeneration;

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
            tPerson.UseCodegeneratedSerializer = true;
            jsonArr = new byte[4096];
            unsafe {
                fixed (byte* p = jsonArr) {
                    size = serializer.Serialize((IntPtr)p, jsonArr.Length, person);
                }
            }
            codegenJson = Encoding.UTF8.GetString(jsonArr, 0, size);
            Assert.AreEqual(correctJson, codegenJson);

            // Now we populate a new person instance with values from the serializer json.
            // And compare it to the original.
            // All values should be identical.
            dynamic person2 = tPerson.CreateInstance(null);
            unsafe {
                fixed (byte* p = jsonArr) {
                    sizeAfterPopulate = serializer.PopulateFromJson((IntPtr)p, size, person2);
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
