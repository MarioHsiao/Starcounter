using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using Starcounter.Templates;
using Starcounter.XSON;

namespace Starcounter.Internal.XSON.Tests {
    class EvaluateJsonPatchTests : GenerateJsonPatchTests {
        //[TestFixtureSetUp]
        //public static void Setup() {
        //    // Initializing global sessions.
        //    GlobalSessions.InitGlobalSessions(1);
        //    StarcounterEnvironment.AppName = "Test";
        //}

        //[TearDown]
        //public static void AfterTest() {
        //    // Making sure that we are ending the session even if the test failed.
        //    Session.End();
        //}

        [Test]
        public static void TestEmptyPatch() {
            var json = new Json();

            int count;
            JsonPatchStatus status = jsonPatch.Apply(json, "[]", true, out count);
            Assert.AreEqual(JsonPatchStatus.Applied, status);
            Assert.AreEqual(0, count);
        }

        [Test]
        public static void TestSoftPatchRejection() {
            string patch;
            byte[] patchArr;
            int number;

            var schema = new TObject();
            var prop = schema.Add<TLong>("Total");
            prop.Editable = true;

            dynamic json = new Json() { Template = schema };
            var session = new Session();
            session.Data = json;
            jsonPatch.Generate(json, true, false);

            json.Total = 1L;
            patch = string.Format(Helper.PATCH_REPLACE, "/Total", "invalid");
            patchArr = System.Text.Encoding.UTF8.GetBytes(patch);
            number = jsonPatch.Apply(json, patchArr, false);
            Assert.AreEqual(number, 1);
            Assert.AreEqual(1L, json.Total);

            patch = jsonPatch.Generate(json, true, false);
            Assert.AreEqual(string.Format(Helper.ONE_PATCH_ARR, "/Total", 1), patch);
        }

        [Test]
        public static void TestIncomingPatchWithInteger() {
            string patch;
            byte[] patchArr;
            int number;

            var schema = new TObject();
            var prop = schema.Add<TLong>("Total");
            prop.Editable = true;

            dynamic json = new Json() { Template = schema };
            var session = new Session();
            session.Data = json;

            json.Total = 1L;
            patch = string.Format(Helper.PATCH_REPLACE, "/Total", "3");
            patchArr = System.Text.Encoding.UTF8.GetBytes(patch);
            number = jsonPatch.Apply(json, patchArr);
            Assert.AreEqual(number, 1);
            Assert.AreEqual(3L, json.Total);

            json.Total = 1L;
            patch = string.Format(Helper.PATCH_REPLACE, "/Total", Helper.Jsonify("3"));
            patchArr = System.Text.Encoding.UTF8.GetBytes(patch);
            number = jsonPatch.Apply(json, patchArr);
            Assert.AreEqual(number, 1);
            Assert.AreEqual(3L, json.Total);
        }

        [Test]
        public static void TestMultipleIncomingPatchesWithInteger() {
            string patch;
            byte[] patchArr;
            int number;

            var schema = new TObject();
            var prop = schema.Add<TLong>("Total");
            prop.Editable = true;

            dynamic json = new Json() { Template = schema };
            var session = new Session();
            session.Data = json;

            json.Total = 1L;
            patch = "[" 
                    + string.Format(Helper.PATCH_REPLACE, "/Total", Helper.Jsonify("3"))
                    + ","
                    + string.Format(Helper.PATCH_REPLACE, "/Total", Helper.Jsonify("66"))
                    + ","
                    + string.Format(Helper.PATCH_REPLACE, "/Total", Helper.Jsonify("666")) 
                    + "]";
            patchArr = System.Text.Encoding.UTF8.GetBytes(patch);
            number = jsonPatch.Apply(json, patchArr);
            Assert.AreEqual(number, 3);
            Assert.AreEqual(666L, json.Total);
        }

        [Test]
        public static void TestIncomingPatchWithDecimal() {
            string patch;
            byte[] patchArr;
            int number;

            var schema = new TObject();
            var prop = schema.Add<TDecimal>("Total");
            prop.Editable = true;

            dynamic json = new Json() { Template = schema };
            var session = new Session();
            session.Data = json;

            json.Total = 1.0m;
            patch = string.Format(Helper.PATCH_REPLACE, "/Total", "3.3");
            patchArr = System.Text.Encoding.UTF8.GetBytes(patch);
            number = jsonPatch.Apply(json, patchArr);
            Assert.AreEqual(number, 1);
            Assert.AreEqual(3.3m, json.Total);

            json.Total = 1.0m;
            patch = string.Format(Helper.PATCH_REPLACE, "/Total", Helper.Jsonify("3.3"));
            patchArr = System.Text.Encoding.UTF8.GetBytes(patch);
            number = jsonPatch.Apply(json, patchArr);
            Assert.AreEqual(number, 1);
            Assert.AreEqual(3.3m, json.Total);
        }

        [Test]
        public static void TestIncomingPatchWithDouble() {
            string patch;
            byte[] patchArr;
            int number;

            var schema = new TObject();
            var prop = schema.Add<TDouble>("Total");
            prop.Editable = true;

            dynamic json = new Json() { Template = schema };
            var session = new Session();
            session.Data = json;

            json.Total = 1.0d;
            patch = string.Format(Helper.PATCH_REPLACE, "/Total", "3.3");
            patchArr = System.Text.Encoding.UTF8.GetBytes(patch);
            number = jsonPatch.Apply(json, patchArr);
            Assert.AreEqual(number, 1);
            Assert.AreEqual(3.3d, json.Total);

            json.Total = 1.0d;
            patch = string.Format(Helper.PATCH_REPLACE, "/Total", Helper.Jsonify("3.3"));
            patchArr = System.Text.Encoding.UTF8.GetBytes(patch);
            number = jsonPatch.Apply(json, patchArr);
            Assert.AreEqual(number, 1);
            Assert.AreEqual(3.3d, json.Total);
        }

        [Test]
        public static void TestIncomingPatchWithBoolean() {
            string patch;
            byte[] patchArr;
            int number;

            var schema = new TObject();
            var prop = schema.Add<TBool>("IsTotal");
            prop.Editable = true;

            dynamic json = new Json() { Template = schema };
            var session = new Session();
            session.Data = json;

            json.IsTotal = false;
            patch = string.Format(Helper.PATCH_REPLACE, "/IsTotal", "true");
            patchArr = System.Text.Encoding.UTF8.GetBytes(patch);
            number = jsonPatch.Apply(json, patchArr);
            Assert.AreEqual(number, 1);
            Assert.AreEqual(true, json.IsTotal);

            json.IsTotal = false;
            patch = string.Format(Helper.PATCH_REPLACE, "/IsTotal", Helper.Jsonify("true"));
            patchArr = System.Text.Encoding.UTF8.GetBytes(patch);
            number = jsonPatch.Apply(json, patchArr);
            Assert.AreEqual(number, 1);
            Assert.AreEqual(true, json.IsTotal);
        }

        [Test]
        public static void TestIncomingPatches() {
            int patchCount;
            string patchStr;
            byte[] patchBytes;
            TObject schema;

            schema = (TObject)Template.CreateFromMarkup("json", File.ReadAllText("json\\simple.json"), "Simple");
            dynamic json = schema.CreateInstance();
            var session = new Session(SessionOptions.StrictPatchRejection);
            session.Data = json;

            // Setting a value on a editable property
            patchStr = string.Format(Helper.PATCH_REPLACE, "/VirtualValue$", Helper.Jsonify("Alpha"));
            patchBytes = Encoding.UTF8.GetBytes(patchStr);

            patchCount = jsonPatch.Apply(json, patchBytes);
            Assert.AreEqual(1, patchCount);
            Assert.AreEqual("Alpha", json.VirtualValue);

            // Setting a value on a readonly property
            patchStr = string.Format(Helper.PATCH_REPLACE, "/BaseValue", Helper.Jsonify("Beta"));
            patchBytes = Encoding.UTF8.GetBytes(patchStr);
            var ex = Assert.Throws<JsonPatchException>(() => {
                jsonPatch.Apply(json, patchBytes);
            });
            Helper.ConsoleWriteLine(ex.Message);

            // Setting values on three properties in one patch
            patchStr = "["
                       + string.Format(Helper.PATCH_REPLACE, "/VirtualValue$", Helper.Jsonify("Apa"))
                       + ","
                       + string.Format(Helper.PATCH_REPLACE, "/OtherValue$", 1395276000)
                       + ","
                       + string.Format(Helper.PATCH_REPLACE, "/AbstractValue$", Helper.Jsonify("Peta"))
                       + "]";
            patchBytes = Encoding.UTF8.GetBytes(patchStr);
            patchCount = jsonPatch.Apply(json, patchBytes);
            Assert.AreEqual(3, patchCount);
            Assert.AreEqual("Apa", json.VirtualValue);
            Assert.AreEqual("Peta", json.AbstractValue);
            Assert.AreEqual(1395276000, json.OtherValue);

            // Making sure all patches are correctly parsed.
            patchStr = "["
                       + string.Format(Helper.PATCH_REPLACE, "/Content/ApplicationPage/GanttData/ItemDropped/Date$", 1395276000)
                       + ","
                       + string.Format(Helper.PATCH_REPLACE, "/Content/ApplicationPage/GanttData/ItemDropped/TemplateId$", Helper.Jsonify("lm7"))
                       + "]";

            jsonPatch.SetPatchHandler((j, op, ptr, str) => { });
            patchBytes = Encoding.UTF8.GetBytes(patchStr);
            patchCount = jsonPatch.Apply(json, patchBytes);
            Assert.AreEqual(2, patchCount);

            jsonPatch.SetPatchHandler(DefaultPatchHandler.Handle);
        }

        [Test]
        public static void TestIncomingPatchesWithHandlers() {
            int patchCount;
            int handledCount;
            string patchStr;
            byte[] patchBytes;
            TObject schema;

            handledCount = 0;

            schema = (TObject)Template.CreateFromMarkup("json", File.ReadAllText("json\\simple.json"), "Simple");
            dynamic json = schema.CreateInstance();
            var session = new Session();
            session.Data = json;

            // Index (same order as declared in simple.json):
            // 0 - VirtualValue (string)
            // 1 - AbstractValue (string)
            // 2 - BaseValue (string)
            // 3 - OtherValue (long)
            ((TString)schema.Properties[0]).AddHandler(
                Helper.CreateInput<string>,
                (Json pup, Starcounter.Input<string> input) => {
                    Helper.ConsoleWriteLine("Handler for VirtualValue called.");
                    handledCount++;
                }   
            );
            ((TString)schema.Properties[1]).AddHandler(
                Helper.CreateInput<string>,
                (Json pup, Starcounter.Input<string> input) => {
                    Helper.ConsoleWriteLine("Handler for AbstractValue called.");
                    handledCount++;
                }
            );
            ((TLong)schema.Properties[3]).AddHandler(
                Helper.CreateInput<long>,
                (Json pup, Starcounter.Input<long> input) => {
                    Helper.ConsoleWriteLine("Handler for OtherValue called.");
                    handledCount++;
                }
            );

            handledCount = 0;

            // Setting a value on a editable property
            patchStr = string.Format(Helper.PATCH_REPLACE, "/VirtualValue$", Helper.Jsonify("Alpha"));
            patchBytes = Encoding.UTF8.GetBytes(patchStr);
            patchCount = jsonPatch.Apply(json, patchBytes);
            Assert.AreEqual(1, handledCount);
            Assert.AreEqual(1, patchCount);
           
            handledCount = 0;

            // Setting values on three properties in one patch
            patchStr = "["
                       + string.Format(Helper.PATCH_REPLACE, "/VirtualValue$", Helper.Jsonify("Apa"))
                       + ","
                       + string.Format(Helper.PATCH_REPLACE, "/OtherValue$", 1395276000)
                       + ","
                       + string.Format(Helper.PATCH_REPLACE, "/AbstractValue$", Helper.Jsonify("Peta"))
                       + "]";
            patchBytes = Encoding.UTF8.GetBytes(patchStr);
            patchCount = jsonPatch.Apply(json, patchBytes);
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

            session.Use(() => {
                JsonProperty aat = Helper.CreateSampleApp();
                dynamic app = aat.Json;

                JsonProperty obj = JsonProperty.Evaluate("/FirstName", app);
                String value = ((Property<string>)obj.Property).Getter(obj.Json);
                Assert.AreEqual(value, "Cliff");

                obj = JsonProperty.Evaluate("/LastName", app);
                value = ((Property<string>)obj.Property).Getter(obj.Json);
                Assert.AreEqual(value, "Barnes");

                obj = JsonProperty.Evaluate("/Items/0/Description", app);
                value = ((Property<string>)obj.Property).Getter(obj.Json);
                Assert.AreEqual(value, "Take a nap!");

                obj = JsonProperty.Evaluate("/Items/1/IsDone", app);
                bool b = ((TBool)obj.Property).Getter(obj.Json);
                Assert.AreEqual(b, true);

                obj = JsonProperty.Evaluate("/Items/1", app);
                //                Assert.IsInstanceOf<SampleApp.ItemsApp>(obj);

                var jpex = Assert.Throws<JsonPatchException>(() => { JsonProperty.Evaluate("/Nonono", app); });
                Assert.IsTrue(jpex.Message.Contains("Unknown property"));
            });
        }

        private static void PrintPointer(JsonPointer ptr, String originalStr) {
            ptr.Reset();
            Helper.ConsoleWrite("Tokens for \"" + originalStr + "\": ");
            while (ptr.MoveNext()) {
                Helper.ConsoleWrite("'");
                Helper.ConsoleWrite(ptr.Current);
                Helper.ConsoleWrite("' ");
            }
            Helper.ConsoleWriteLine("");
        }

        [Test]
        public static void TestPatchVersioning() {
            int evaluatedCount;
            string incomingPatch;
            string outgoingPatch;
            Session session;
            Json json;
            JsonPatchStatus status;
            TObject tJson;
            TString tValue;

            tJson = (TObject)Template.CreateFromMarkup("json", File.ReadAllText("json\\simple.json"), "Simple");
            tValue = (TString)tJson.Properties[1];
            json = (Json)tJson.CreateInstance();
            session = new Session(SessionOptions.PatchVersioning);
            session.Data = json;

            var version = json.ChangeLog.Version;

            json.ChangeLog.Checkpoint();
            Assert.AreEqual(0, version.RemoteVersion);
            Assert.AreEqual(0, version.LocalVersion);

            incomingPatch = GetVersioningPatch(version, 1, 0);
            status = jsonPatch.Apply(json, incomingPatch, true, out evaluatedCount);
            Assert.AreEqual(JsonPatchStatus.Applied, status);
            Assert.AreEqual(3, evaluatedCount);
            Assert.AreEqual(1, version.RemoteVersion);
            Assert.AreEqual(0, version.LocalVersion);
            Assert.AreEqual("Changed1", tValue.Getter(json));

            tValue.Setter(json, "qwerty");
            outgoingPatch = jsonPatch.Generate(json, true, false);
            Assert.AreEqual(1, version.LocalVersion);

            incomingPatch = GetVersioningPatch(version, 4, 1);
            status = jsonPatch.Apply(json, incomingPatch, true, out evaluatedCount);
            Assert.AreEqual(JsonPatchStatus.Applied, status);
            Assert.AreEqual(-1, evaluatedCount);
            Assert.AreEqual(1, version.RemoteVersion);
            Assert.AreEqual(1, version.LocalVersion);
            Assert.AreEqual("qwerty", tValue.Getter(json));

            incomingPatch = GetVersioningPatch(version, 3, 1);
            status = jsonPatch.Apply(json, incomingPatch, true, out evaluatedCount);
            Assert.AreEqual(JsonPatchStatus.Applied, status);
            Assert.AreEqual(-1, evaluatedCount);
            Assert.AreEqual(1, version.RemoteVersion);
            Assert.AreEqual(1, version.LocalVersion);
            Assert.AreEqual("qwerty", tValue.Getter(json));

            incomingPatch = GetVersioningPatch(version, 5, 1);
            status = jsonPatch.Apply(json, incomingPatch, true, out evaluatedCount);
            Assert.AreEqual(JsonPatchStatus.Applied, status);
            Assert.AreEqual(-1, evaluatedCount);
            Assert.AreEqual(1, version.RemoteVersion);
            Assert.AreEqual(1, version.LocalVersion);
            Assert.AreEqual("qwerty", tValue.Getter(json));

            incomingPatch = GetVersioningPatch(version, 2, 1);
            status = jsonPatch.Apply(json, incomingPatch, true, out evaluatedCount);
            Assert.AreEqual(JsonPatchStatus.Applied, status);
            Assert.AreEqual(12, evaluatedCount);
            Assert.AreEqual(5, version.RemoteVersion);
            Assert.AreEqual(1, version.LocalVersion);
            Assert.AreEqual("Changed1", tValue.Getter(json));
        }

        [Test]
        public static void TestAlreadyApplied() {
            // arrange
            var tJson = (TObject)Template.CreateFromMarkup("json", File.ReadAllText("json\\simple.json"), "Simple");
            var json = (Json)tJson.CreateInstance();
            new Session(SessionOptions.PatchVersioning) {Data = json}; // establishes itself as current session
            var version = json.ChangeLog.Version;
            int evaluatedCount;

            json.ChangeLog.Checkpoint();
            jsonPatch.Apply(json, GetVersioningPatch(version, 1, 0), true, out evaluatedCount);

            // act
            JsonPatchStatus status = jsonPatch.Apply(json, GetVersioningPatch(version, 1, 0), true, out evaluatedCount);

            // assert
            Assert.AreEqual(JsonPatchStatus.AlreadyApplied, status);
            Assert.AreEqual(1, version.RemoteVersion);
            Assert.AreEqual(0, version.LocalVersion);
        }

        [Test]
        public static void TestPatchOTForObject() {
            int evaluatedCount;
            string incomingPatch;
            string outgoingPatch;
            JsonPatchStatus status;
            Session session;
            TestOfOT json = new TestOfOT();

            session = new Session(SessionOptions.PatchVersioning | SessionOptions.StrictPatchRejection);
            session.Data = json;

            var version = json.ChangeLog.Version;

            json.ChangeLog.Checkpoint();
            Assert.AreEqual(0, version.RemoteVersion);
            Assert.AreEqual(0, version.LocalVersion);

            incomingPatch = GetVersioningPatch(version, 1, 0, string.Format(Helper.PATCH_REPLACE, "/Page/Description$", @"""Changed1"""));
            status = jsonPatch.Apply(json, incomingPatch, true, out evaluatedCount);
            Assert.AreEqual(JsonPatchStatus.Applied, status);
            Assert.AreEqual(3, evaluatedCount);
            Assert.AreEqual(1, version.RemoteVersion);
            Assert.AreEqual(0, version.LocalVersion);
            Assert.AreEqual("Changed1", json.Page.Description);

            json.Page.Description = "A";
            outgoingPatch = jsonPatch.Generate(json, true, false);
            Assert.AreEqual(1, version.LocalVersion);

            incomingPatch = GetVersioningPatch(version, 2, 0, string.Format(Helper.PATCH_REPLACE, "/Page/Description$", @"""Changed1"""));
            status = jsonPatch.Apply(json, incomingPatch, true, out evaluatedCount);
            Assert.AreEqual(JsonPatchStatus.Applied, status);
            Assert.AreEqual(3, evaluatedCount);
            Assert.AreEqual(2, version.RemoteVersion);
            Assert.AreEqual(1, version.LocalVersion);
            Assert.AreEqual("Changed1", json.Page.Description);

            json.Page = new TestOfOT.PageJson();

            incomingPatch = GetVersioningPatch(version, 3, 0, string.Format(Helper.PATCH_REPLACE, "/Page/Description$", @"""Changed1"""));

            Assert.Throws<JsonPatchException>(() => {
                status = jsonPatch.Apply(json, incomingPatch, true, out evaluatedCount);
            });
        }

        [Test]
        public static void TestUpdateServerVersion() {
            Session session = new Session(SessionOptions.PatchVersioning);
            TestOfOT json = new TestOfOT();

            session.Data = json;

            var version = json.ChangeLog.Version;

            Assert.AreEqual(0, version.LocalVersion);
            Assert.AreEqual(0, version.RemoteLocalVersion);
            Assert.AreEqual(0, version.RemoteVersion);

            jsonPatch.Generate(json, true, false);
            Assert.AreEqual(1, version.LocalVersion);

            jsonPatch.Generate(json, true, false);
            jsonPatch.Generate(json, true, false);
            jsonPatch.Generate(json, true, false);
            Assert.AreEqual(4, version.LocalVersion);
        }

        [Test]
        public static void TestUpdateClientVersion() {
            Session session = new Session(SessionOptions.PatchVersioning);
            TestOfOT json = new TestOfOT();
            String incomingPatch;

            session.Data = json;

            var version = json.ChangeLog.Version;

            jsonPatch.Generate(json, true, false);

            int patchCount;
            incomingPatch = GetVersioningPatch(version, 1, 1, string.Format(Helper.PATCH_REPLACE, "/Name$", @"""A:Change"""));
            JsonPatchStatus status = jsonPatch.Apply(json, incomingPatch, true, out patchCount);
            Assert.AreEqual(JsonPatchStatus.Applied, status);
            Assert.AreEqual(1, version.RemoteVersion);
            Assert.AreEqual(1, version.RemoteLocalVersion);
        }

        [Test]
        public static void TestSuccesfulTransformPendingServerChanges() {
            int evaluatedCount;
            string incomingPatch;
            string outgoingPatch;
            Session session;
            TestOfOT json = new TestOfOT();

            TestOfOT.ItemsElementJson itemA = json.Items.Add(); // 0
            itemA.Description = "A";
            TestOfOT.ItemsElementJson itemB = json.Items.Add(); // 1
            itemB.Description = "B";

            session = new Session(SessionOptions.PatchVersioning);
            session.Data = json;

            var version = json.ChangeLog.Version;

            outgoingPatch = jsonPatch.Generate(json, true, false); // ServerVersion: 1

            var itemC = new TestOfOT.ItemsElementJson();
            itemC.Description = "C";
            json.Items.Insert(0, itemC);

            // ServerVersion is still 1 since no patches have been created, but there are pending changes that needs to be considered.
            incomingPatch = GetVersioningPatch(version, 1, 1, string.Format(Helper.PATCH_REPLACE, "/Items/0/Description$", @"""A:Change"""));
            JsonPatchStatus status = jsonPatch.Apply(json, incomingPatch, true, out evaluatedCount);
            Assert.AreEqual(JsonPatchStatus.Applied, status);
            Assert.AreEqual(3, evaluatedCount);
            Assert.AreEqual(1, version.RemoteVersion);
            Assert.AreEqual(1, version.LocalVersion);
            Assert.AreEqual("A:Change", itemA.Description);
            Assert.AreEqual("C", itemC.Description);
        }

        [Test]
        public static void TestSuccesfulTransformOneVersionBehind() {
            int evaluatedCount;
            string incomingPatch;
            string outgoingPatch;
            Session session;
            TestOfOT json = new TestOfOT();

            TestOfOT.ItemsElementJson itemA = json.Items.Add(); // 0
            itemA.Description = "A";
            TestOfOT.ItemsElementJson itemB = json.Items.Add(); // 1
            itemB.Description = "B";

            session = new Session(SessionOptions.PatchVersioning);
            session.Data = json;

            var version = json.ChangeLog.Version;

            outgoingPatch = jsonPatch.Generate(json, true, false); // ServerVersion: 1
            
            var itemC = new TestOfOT.ItemsElementJson();
            itemC.Description = "C";
            json.Items.Insert(0, itemC);
            outgoingPatch = jsonPatch.Generate(json, true, false); // ServerVersion: 2
            
            // One version behind, index 0 should be 1
            incomingPatch = GetVersioningPatch(version, 1, 1, string.Format(Helper.PATCH_REPLACE, "/Items/0/Description$", @"""A:Change"""));
            JsonPatchStatus status = jsonPatch.Apply(json, incomingPatch, true, out evaluatedCount);
            Assert.AreEqual(JsonPatchStatus.Applied, status);
            Assert.AreEqual(3, evaluatedCount);
            Assert.AreEqual(1, version.RemoteVersion);
            Assert.AreEqual(2, version.LocalVersion);
            Assert.AreEqual("A:Change", itemA.Description);
            Assert.AreEqual("C", itemC.Description);
        }

        [Test]
        public static void TestSuccesfulTransformSeveralVersionsBehindWithAdds() {
            int evaluatedCount;
            string incomingPatch;
            string outgoingPatch;
            Session session;
            TestOfOT json = new TestOfOT();

            var itemA = new TestOfOT.ItemsElementJson();
            itemA.Description = "A";
            var itemB = new TestOfOT.ItemsElementJson();
            itemB.Description = "B";
            var itemC = new TestOfOT.ItemsElementJson();
            itemC.Description = "C";
            var itemDummy = new TestOfOT.ItemsElementJson();
            itemDummy.Description = "Dummy";

            session = new Session(SessionOptions.PatchVersioning);
            session.Data = json;

            var version = json.ChangeLog.Version;

            outgoingPatch = jsonPatch.Generate(json, true, false); // ServerVersion: 1
            json.Items.Add(itemDummy); // index 0
            json.Items.Insert(0, itemA);
            outgoingPatch = jsonPatch.Generate(json, true, false); // ServerVersion: 2

            json.Items.Insert(0, itemC); // itemA -> index 1
            outgoingPatch = jsonPatch.Generate(json, true, false); // ServerVersion: 3
            json.Items.Insert(0, itemB); // itemA -> index 2
            outgoingPatch = jsonPatch.Generate(json, true, false); // ServerVersion: 4

            // Several versions behind, index 0 should be 2
            incomingPatch = GetVersioningPatch(version, 1, 2, string.Format(Helper.PATCH_REPLACE, "/Items/0/Description$", @"""A:Change"""));
            JsonPatchStatus status = jsonPatch.Apply(json, incomingPatch, true, out evaluatedCount);
            Assert.AreEqual(JsonPatchStatus.Applied, status);
            Assert.AreEqual(3, evaluatedCount);
            Assert.AreEqual(1, version.RemoteVersion);
            Assert.AreEqual(4, version.LocalVersion);
            Assert.AreEqual("A:Change", itemA.Description);
            Assert.AreEqual("C", itemC.Description);
            Assert.AreEqual("Dummy", itemDummy.Description);

            outgoingPatch = jsonPatch.Generate(json, true, false); // ServerVersion: 5
        }

        [Test]
        public static void TestSuccesfulTransformSeveralVersionsBehindWithAddsAndRemoves() {
            int evaluatedCount;
            string incomingPatch;
            string outgoingPatch;
            Session session;
            TestOfOT json = new TestOfOT();

            var itemA = new TestOfOT.ItemsElementJson();
            itemA.Description = "A";
            var itemB = new TestOfOT.ItemsElementJson();
            itemB.Description = "B";
            var itemC = new TestOfOT.ItemsElementJson();
            itemC.Description = "C";
            var itemDummy = new TestOfOT.ItemsElementJson();
            itemDummy.Description = "Dummy";

            session = new Session(SessionOptions.PatchVersioning);
            session.Data = json;

            var version = json.ChangeLog.Version;

            outgoingPatch = jsonPatch.Generate(json, true, false); // ServerVersion: 1
            json.Items.Add(itemDummy); // index 0
            json.Items.Insert(0, itemA);
            outgoingPatch = jsonPatch.Generate(json, true, false); // ServerVersion: 2
            json.Items.Insert(0, itemC); // itemA -> index 1
            outgoingPatch = jsonPatch.Generate(json, true, false); // ServerVersion: 3
            json.Items.Insert(0, itemB); // itemA -> index 2
            json.Items.Remove(itemC); // itemA -> index 1
            outgoingPatch = jsonPatch.Generate(json, true, false); // ServerVersion: 4

            // Several versions behind, index 0 should be 1
            incomingPatch = GetVersioningPatch(version, 1, 2, string.Format(Helper.PATCH_REPLACE, "/Items/0/Description$", @"""A:Change"""));
            JsonPatchStatus status = jsonPatch.Apply(json, incomingPatch, true, out evaluatedCount);
            Assert.AreEqual(JsonPatchStatus.Applied, status);
            Assert.AreEqual(3, evaluatedCount);
            Assert.AreEqual(1, version.RemoteVersion);
            Assert.AreEqual(4, version.LocalVersion);
            Assert.AreEqual("A:Change", itemA.Description);
            Assert.AreEqual("C", itemC.Description);
            Assert.AreEqual("B", itemB.Description);
            Assert.AreEqual("Dummy", itemDummy.Description);
        }

        [Test]
        public static void TestUnsuccesfulTransformSeveralVersionsBehindWithAddsAndRemoves() {
            int evaluatedCount;
            string incomingPatch;
            string outgoingPatch;
            Session session;
            TestOfOT json = new TestOfOT();

            var itemA = new TestOfOT.ItemsElementJson();
            itemA.Description = "A";
            var itemB = new TestOfOT.ItemsElementJson();
            itemB.Description = "B";
            var itemC = new TestOfOT.ItemsElementJson();
            itemC.Description = "C";
            var itemDummy = new TestOfOT.ItemsElementJson();
            itemDummy.Description = "Dummy";

            session = new Session(SessionOptions.PatchVersioning);
            session.Data = json;

            var version = json.ChangeLog.Version;

            outgoingPatch = jsonPatch.Generate(json, true, false); // ServerVersion: 1
            json.Items.Add(itemDummy); // index 0
            outgoingPatch = jsonPatch.Generate(json, true, false); // ServerVersion: 2
            json.Items.Insert(0, itemA);
            outgoingPatch = jsonPatch.Generate(json, true, false); // ServerVersion: 3
            json.Items.Insert(0, itemC); // itemA -> index 1
            outgoingPatch = jsonPatch.Generate(json, true, false); // ServerVersion: 4
            json.Items.Insert(0, itemB); // itemA -> index 2
            json.Items.Remove(itemA); // itemA -> invalid
            outgoingPatch = jsonPatch.Generate(json, true, false); // ServerVersion: 5

            // Several versions behind, index 0 should be 1
            incomingPatch = GetVersioningPatch(version, 1, 3, string.Format(Helper.PATCH_REPLACE, "/Items/0/Description$", @"""A:Change"""));
            
            JsonPatchStatus status = jsonPatch.Apply(json, incomingPatch, false, out evaluatedCount);
            Assert.AreEqual(JsonPatchStatus.Applied, status);
            Assert.AreEqual(1, version.RemoteVersion);
            Assert.AreEqual(5, version.LocalVersion);
            Assert.AreEqual("A", itemA.Description);
            Assert.AreEqual("C", itemC.Description);
            Assert.AreEqual("B", itemB.Description);
            Assert.AreEqual("Dummy", itemDummy.Description);

            outgoingPatch = jsonPatch.Generate(json, true, false); // ServerVersion: 6
        }

        [Test]
        public static void TestEmptryStringAsNumberInPatch_3725() {
            dynamic json = new Json();
            var session = new Session(SessionOptions.StrictPatchRejection);
            var jsonPatch = new JsonPatch();
            string incomingPatch;
            string outgoingPatch;

            json.Session = session;
            json.NumberLong = 1L;
            json.Template.Properties[0].Editable = true;
            json.NumberDbl = 2.0d;
            json.Template.Properties[1].Editable = true;
            json.NumberDec = 3.0m;
            json.Template.Properties[2].Editable = true;

            jsonPatch.Generate(json, true, false); // Resetting dirtyflags.

            // Testing empty string for long value 
            incomingPatch = string.Format(Helper.ONE_PATCH_ARR, "/NumberLong", Helper.Jsonify(""));
            Assert.DoesNotThrow(() => { jsonPatch.Apply(json, incomingPatch, true); });
            Assert.AreEqual(0L, json.NumberLong);
            outgoingPatch = jsonPatch.Generate(json, true, false);
            Assert.AreEqual(string.Format(Helper.ONE_PATCH_ARR, "/NumberLong", "0"), outgoingPatch);

            // Testing empty string for double value 
            incomingPatch = string.Format(Helper.ONE_PATCH_ARR, "/NumberDbl", Helper.Jsonify(""));
            Assert.DoesNotThrow(() => { jsonPatch.Apply(json, incomingPatch, true); });
            Assert.AreEqual(default(double), json.NumberDbl);
            outgoingPatch = jsonPatch.Generate(json, true, false);
            Assert.AreEqual(string.Format(Helper.ONE_PATCH_ARR, "/NumberDbl", "0.0"), outgoingPatch);

            // Testing empty string for decimal value 
            incomingPatch = string.Format(Helper.ONE_PATCH_ARR, "/NumberDec", Helper.Jsonify(""));
            Assert.DoesNotThrow(() => { jsonPatch.Apply(json, incomingPatch, true); });
            Assert.AreEqual(default(decimal), json.NumberDbl);
            outgoingPatch = jsonPatch.Generate(json, true, false);
            Assert.AreEqual(string.Format(Helper.ONE_PATCH_ARR, "/NumberDec", "0.0"), outgoingPatch);
        }


        private static string GetVersioningPatch(ViewModelVersion version, long clientVersion, long serverVersion) {
            return
                "["
                + string.Format(Helper.PATCH_REPLACE, "/" + version.RemoteVersionPropertyName, clientVersion)
                + ","
                + string.Format(Helper.PATCH_TEST, "/" + version.LocalVersionPropertyName, serverVersion)
                + ","
                + string.Format(Helper.PATCH_REPLACE, "/AbstractValue$", @"""Changed1""")
                + "]";
        }

        private static string GetVersioningPatch(ViewModelVersion version, long clientVersion, long serverVersion, string valuePatch) {
            return
                "["
                + string.Format(Helper.PATCH_REPLACE, "/" + version.RemoteVersionPropertyName, clientVersion)
                + ","
                + string.Format(Helper.PATCH_TEST, "/" + version.LocalVersionPropertyName, serverVersion)
                + ","
                + valuePatch
                + "]";
        }
    }
}
