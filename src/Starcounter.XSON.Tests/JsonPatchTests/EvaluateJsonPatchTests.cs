using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using Starcounter.Internal.JsonPatch;
using Starcounter.Templates;

namespace Starcounter.Internal.XSON.Tests {
    class EvaluateJsonPatchTests {
        [TestFixtureSetUp]
        public static void Setup() {
            // Initializing global sessions.
            GlobalSessions.InitGlobalSessions(1);
        }

        [TearDown]
        public static void AfterTest() {
            // Making sure that we are ending the session even if the test failed.
            Session.End();
        }

        [Test]
        public static void TestIncomingPatches() {
            int patchCount;
            string patchStr;
            byte[] patchBytes;
            TObject schema;

            schema = TObject.CreateFromMarkup<Json, TObject>("json", File.ReadAllText("json\\simple.json"), "Simple");
            dynamic json = schema.CreateInstance();

            // Setting a value on a editable property
            patchStr = string.Format(Helper.PATCH, "/VirtualValue$", Helper.Jsonify("Alpha"));
            patchBytes = Encoding.UTF8.GetBytes(patchStr);
            patchCount = JsonPatch.JsonPatch.EvaluatePatches(json, patchBytes);
            Assert.AreEqual(1, patchCount);
            Assert.AreEqual("Alpha", json.VirtualValue);

            // Setting a value on a readonly property
            patchStr = string.Format(Helper.PATCH, "/BaseValue", Helper.Jsonify("Beta"));
            patchBytes = Encoding.UTF8.GetBytes(patchStr);
            var ex = Assert.Throws<JsonPatch.JsonPatchException>(() => {
                JsonPatch.JsonPatch.EvaluatePatches(json, patchBytes);
            });
            Console.WriteLine(ex.Message);

            // Setting values on three properties in one patch
            patchStr = "["
                       + string.Format(Helper.PATCH, "/VirtualValue$", Helper.Jsonify("Apa"))
                       + ","
                       + string.Format(Helper.PATCH, "/OtherValue$", 1395276000)
                       + ","
                       + string.Format(Helper.PATCH, "/AbstractValue$", Helper.Jsonify("Peta"))
                       + "]";
            patchBytes = Encoding.UTF8.GetBytes(patchStr);
            patchCount = JsonPatch.JsonPatch.EvaluatePatches(json, patchBytes);
            Assert.AreEqual(3, patchCount);
            Assert.AreEqual("Apa", json.VirtualValue);
            Assert.AreEqual("Peta", json.AbstractValue);
            Assert.AreEqual(1395276000, json.OtherValue);

            // Making sure all patches are correctly parsed.
            patchStr = "["
                       + string.Format(Helper.PATCH, "/Content/ApplicationPage/GanttData/ItemDropped/Date$", 1395276000)
                       + ","
                       + string.Format(Helper.PATCH, "/Content/ApplicationPage/GanttData/ItemDropped/TemplateId$", Helper.Jsonify("lm7"))
                       + "]";
            patchBytes = Encoding.UTF8.GetBytes(patchStr);
            patchCount = JsonPatch.JsonPatch.EvaluatePatches(null, patchBytes);
            Assert.AreEqual(2, patchCount);

        }

        [Test]
        public static void TestIncomingPatchesWithHandlers() {
            int patchCount;
            int handledCount;
            string patchStr;
            byte[] patchBytes;
            TObject schema;

            handledCount = 0;

            schema = TObject.CreateFromMarkup<Json, TObject>("json", File.ReadAllText("json\\simple.json"), "Simple");
            dynamic json = schema.CreateInstance();

            // Index (same order as declared in simple.json):
            // 0 - VirtualValue (string)
            // 1 - AbstractValue (string)
            // 2 - BaseValue (string)
            // 3 - OtherValue (long)
            ((TString)schema.Properties[0]).AddHandler(
                Helper.CreateInput<string>,
                (Json pup, Starcounter.Input<string> input) => {
                    Console.WriteLine("Handler for VirtualValue called.");
                    handledCount++;
                }   
            );
            ((TString)schema.Properties[1]).AddHandler(
                Helper.CreateInput<string>,
                (Json pup, Starcounter.Input<string> input) => {
                    Console.WriteLine("Handler for AbstractValue called.");
                    handledCount++;
                }
            );
            ((TLong)schema.Properties[3]).AddHandler(
                Helper.CreateInput<long>,
                (Json pup, Starcounter.Input<long> input) => {
                    Console.WriteLine("Handler for OtherValue called.");
                    handledCount++;
                }
            );

            handledCount = 0;

            // Setting a value on a editable property
            patchStr = string.Format(Helper.PATCH, "/VirtualValue$", Helper.Jsonify("Alpha"));
            patchBytes = Encoding.UTF8.GetBytes(patchStr);
            patchCount = JsonPatch.JsonPatch.EvaluatePatches(json, patchBytes);
            Assert.AreEqual(1, handledCount);
            Assert.AreEqual(1, patchCount);
           
            handledCount = 0;

            // Setting values on three properties in one patch
            patchStr = "["
                       + string.Format(Helper.PATCH, "/VirtualValue$", Helper.Jsonify("Apa"))
                       + ","
                       + string.Format(Helper.PATCH, "/OtherValue$", 1395276000)
                       + ","
                       + string.Format(Helper.PATCH, "/AbstractValue$", Helper.Jsonify("Peta"))
                       + "]";
            patchBytes = Encoding.UTF8.GetBytes(patchStr);
            patchCount = JsonPatch.JsonPatch.EvaluatePatches(json, patchBytes);
            Assert.AreEqual(3, handledCount);
            Assert.AreEqual(3, patchCount);
        }

        /// <summary>
        /// Tests the JsonPointer implementation.
        /// </summary>
        [Test]
        public static void TestJsonPointer() {
            String strPtr = "";
            JsonPointer jsonPtr = new JsonPointer(strPtr);

            // TODO:
            // Empty string should contain one token, pointing
            // to the whole document.

            //Assert.IsTrue(jsonPtr.MoveNext());
            //Assert.AreEqual("", jsonPtr.Current);
            //PrintPointer(jsonPtr, strPtr);

            strPtr = "/master/login";
            jsonPtr = new JsonPointer(strPtr);
            Assert.IsTrue(jsonPtr.MoveNext());
            Assert.AreEqual("master", jsonPtr.Current);
            Assert.IsTrue(jsonPtr.MoveNext());
            Assert.AreEqual("login", jsonPtr.Current);
            Assert.IsFalse(jsonPtr.MoveNext());
            PrintPointer(jsonPtr, strPtr);

            strPtr = "/foo/2";
            jsonPtr = new JsonPointer(strPtr);
            Assert.IsTrue(jsonPtr.MoveNext());
            Assert.AreEqual("foo", jsonPtr.Current);
            Assert.IsTrue(jsonPtr.MoveNext());
            Assert.AreEqual(2, jsonPtr.CurrentAsInt);
            Assert.IsFalse(jsonPtr.MoveNext());
            PrintPointer(jsonPtr, strPtr);

            strPtr = "/a~1b/b~0r";
            jsonPtr = new JsonPointer(strPtr);
            Assert.IsTrue(jsonPtr.MoveNext());
            Assert.AreEqual("a/b", jsonPtr.Current);
            Assert.IsTrue(jsonPtr.MoveNext());
            Assert.AreEqual("b~r", jsonPtr.Current);
            Assert.IsFalse(jsonPtr.MoveNext());
            PrintPointer(jsonPtr, strPtr);

            strPtr = "/foo/b\'r";
            jsonPtr = new JsonPointer(strPtr);
            Assert.IsTrue(jsonPtr.MoveNext());
            Assert.AreEqual("foo", jsonPtr.Current);
            Assert.IsTrue(jsonPtr.MoveNext());
            Assert.AreEqual("b'r", jsonPtr.Current);
            Assert.IsFalse(jsonPtr.MoveNext());
            PrintPointer(jsonPtr, strPtr);

            strPtr = "/fo o/ ";
            jsonPtr = new JsonPointer(strPtr);
            Assert.IsTrue(jsonPtr.MoveNext());
            Assert.AreEqual("fo o", jsonPtr.Current);
            Assert.IsTrue(jsonPtr.MoveNext());
            Assert.AreEqual(" ", jsonPtr.Current);
            Assert.IsFalse(jsonPtr.MoveNext());
            PrintPointer(jsonPtr, strPtr);

            strPtr = "/value1/value2/3/value4";
            jsonPtr = new JsonPointer(strPtr);
            Assert.IsTrue(jsonPtr.MoveNext());
            Assert.AreEqual("value1", jsonPtr.Current);
            Assert.IsTrue(jsonPtr.MoveNext());
            Assert.AreEqual("value2", jsonPtr.Current);
            Assert.IsTrue(jsonPtr.MoveNext());
            Assert.AreEqual(3, jsonPtr.CurrentAsInt);
            Assert.IsTrue(jsonPtr.MoveNext());
            Assert.AreEqual("value4", jsonPtr.Current);
            Assert.IsFalse(jsonPtr.MoveNext());
            PrintPointer(jsonPtr, strPtr);
        }

        [Test]
        public static void TestEvaluateJsonPointer() {
            Session session = new Session();

            Session.Execute(session, () => {

                AppAndTemplate aat = Helper.CreateSampleApp();
                dynamic app = aat.App;

                AppAndTemplate obj = JsonPatch.JsonPatch.Evaluate(app, "/FirstName");
                String value = ((Property<string>)obj.Template).Getter(obj.App);
                Assert.AreEqual(value, "Cliff");

                obj = JsonPatch.JsonPatch.Evaluate(app, "/LastName");
                value = ((Property<string>)obj.Template).Getter(obj.App);
                Assert.AreEqual(value, "Barnes");

                obj = JsonPatch.JsonPatch.Evaluate(app, "/Items/0/Description");
                value = ((Property<string>)obj.Template).Getter(obj.App);
                Assert.AreEqual(value, "Take a nap!");

                obj = JsonPatch.JsonPatch.Evaluate(app, "/Items/1/IsDone");
                bool b = ((TBool)obj.Template).Getter(obj.App);
                Assert.AreEqual(b, true);

                obj = JsonPatch.JsonPatch.Evaluate(app, "/Items/1");
                //                Assert.IsInstanceOf<SampleApp.ItemsApp>(obj);

                var jpex = Assert.Throws<JsonPatchException>(() => { JsonPatch.JsonPatch.Evaluate(app, "/Nonono"); });
                Assert.IsTrue(jpex.Message.Contains("Unknown property"));
            });
        }

        private static void PrintPointer(JsonPointer ptr, String originalStr) {
            ptr.Reset();
            Console.Write("Tokens for \"" + originalStr + "\": ");
            while (ptr.MoveNext()) {
                Console.Write('\'');
                Console.Write(ptr.Current);
                Console.Write("' ");
            }
            Console.WriteLine();
        }
    }
}
