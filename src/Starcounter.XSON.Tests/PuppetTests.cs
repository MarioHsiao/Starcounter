using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Starcounter.Templates;

namespace Starcounter.Internal.XSON.Tests {
    [TestFixture]
    public class PuppetTests : GenerateJsonPatchTests{

        //[TestFixtureSetUp]
        //public static void Setup() {
        //    GlobalSessions.InitGlobalSessions(1);
        //    StarcounterEnvironment.AppName = "Test";
        //}

        //[TestFixtureTearDown]
        //public static void TearDown() {
        //    StarcounterEnvironment.AppName = null;
        //}

        [Test]
        public static void TestSingleSessionState() {
            Json root = new Json();
            Session session = new Session();
            Session session2 = new Session();

            session.CargoId = 1;
            session2.CargoId = 2;

            StarcounterEnvironment.AppName = "SingleApp";
            session.Data = root;

            Assert.IsTrue(session == root.Session);
            Assert.IsTrue(root == session.Data);

            StarcounterEnvironment.AppName = "SingleApp2";
            session2.Data = root;

            Assert.IsTrue(session.Data == null);
            Assert.IsTrue(session2 == root.Session);

            StarcounterEnvironment.AppName = "Test";
        }

        [Test]
        public static void TestMultiAppSessionState() {
            var root1 = new Json();
            var root2 = new Json();
            var root3 = new Json();
            var app1 = "App1";
            var app2 = "App2";
            var app3 = "App3";
            var session = new Session();

            StarcounterEnvironment.AppName = app1;
            session.Data = root1;

            StarcounterEnvironment.AppName = app2;
            session.Data = root2;

            StarcounterEnvironment.AppName = app3;
            session.Data = root3;

            StarcounterEnvironment.AppName = app2;
            Assert.IsTrue(root2 == session.Data);

            StarcounterEnvironment.AppName = app1;
            Assert.IsTrue(root1 == session.Data);

            StarcounterEnvironment.AppName = app3;
            Assert.IsTrue(root3 == session.Data);

            StarcounterEnvironment.AppName = "Test";
        }

        [Test]
        public static void TestIncorrectRootObjectForSession() {
            Exception ex;
            uint ec;
            dynamic root = new Json();
            Json child = new Json();
            Session session = new Session();

            root.Child = child;

            // Cannot set json that is not a root as sessionstate.
            ex = Assert.Throws<Exception>(() => { child.Session = session; });
            ErrorCode.TryGetCode(ex, out ec);
            Assert.AreEqual(Error.SCERRSESSIONJSONNOTROOT, ec);

            ex = Assert.Throws<Exception>(() => { session.Data = child; });
            ErrorCode.TryGetCode(ex, out ec);
            Assert.AreEqual(Error.SCERRSESSIONJSONNOTROOT, ec);
        }

        [Test]
        public static void TestDirtyCheckForSingleValue() {
            Person data = new Person();
            data.FirstName = "Hans";
            data.LastName = "Brix";

            dynamic json = new Json();
            json.Data = data;
            json.Session = new Session();

            // Needed to make sure the templates are autocreated before we test dirtycheck.
            object tmp = json.FirstName;
            tmp = json.LastName;

            // Resetting dirtyflags.
            string patch = jsonPatch.CreateJsonPatch(json.Session, true, false);

            Helper.ConsoleWriteLine(patch);
            Helper.ConsoleWriteLine("");

            data.FirstName = "Bengt";
            patch = jsonPatch.CreateJsonPatch(json.Session, true, false);

            Helper.ConsoleWriteLine(patch);
            Helper.ConsoleWriteLine("");

            var expected = string.Format(Helper.ONE_PATCH_ARR, "/FirstName", Helper.Jsonify("Bengt"));
            Assert.AreEqual(expected, patch);
        }

        [Test]
        public static void TestDirtyCheckForUnboundValues() {
            var schema = new TObject();
            var tname = schema.Add<TString>("Name", bind: null);
            var tpage = schema.Add<TObject>("Page", bind: null);
            var tarr = schema.Add<TArray<Json>>("Items", bind: null);

            Json json = new Json() { 
                Template = schema 
            };
            json.Session = new Session();
            json.Set(tname, "Hans Brix");
            
            // Resetting dirtyflags.
            json.Session.GenerateChangeLog();
            json.Session.CheckpointChangeLog();
            json.Session.ClearChangeLog();

            json.Set(tname, "Apa Papa");
            Assert.IsTrue(json.IsDirty(tname));

            json.Set(tpage, new Json());
            Assert.IsTrue(json.IsDirty(tpage));

            json.Set(tarr, new Arr<Json>(json, tarr));
            Assert.IsTrue(json.IsDirty(tarr));

            json.Session.GenerateChangeLog();
            Assert.AreEqual(3, json.Session.GetChanges().Count);
        }
       
        [Test]
        public static void TestDirtyCheckForUnboundValuesWithCustomAccessors() {
            var schema = new TObject();
            var tname = schema.Add<TString>("Name", bind: null);
            var tpage = schema.Add<TObject>("Page", bind: null);
            var tarr = schema.Add<TArray<Json>>("Items", bind: null);

            Json bf_page = null;
            string bf_name = null;
            Arr<Json> bf_arr = null;

            tname.SetCustomAccessors((p) => { return bf_name; }, (p, v) => { bf_name = v; });
            tpage.SetCustomAccessors((p) => { return bf_page; }, (p, v) => { bf_page = v; });
            tarr.SetCustomAccessors((p) => { return bf_arr; }, (p, v) => { bf_arr = v; });

            Json json = new Json() {
                Template = schema
            };
            json.Session = new Session();
            json.Set(tname, "Hans Brix");

            // Resetting dirtyflags.
            json.Session.GenerateChangeLog();
            json.Session.CheckpointChangeLog();
            json.Session.ClearChangeLog();

            json.Set(tname, "Apa Papa");
            Assert.IsTrue(json.IsDirty(tname));

            json.Set(tpage, new Json());
            Assert.IsTrue(json.IsDirty(tpage));

            json.Set(tarr, new Arr<Json>(json, tarr));
            Assert.IsTrue(json.IsDirty(tarr));

            json.Session.GenerateChangeLog();
            Assert.AreEqual(3, json.Session.GetChanges().Count);
        }

        [Test]
        public static void TestDirtyCheckForArrays() {
            Recursive data = new Recursive() { Name = "Root" };
            Recursive item = new Recursive() { Name = "Item" };
            Recursive subItem = new Recursive() { Name = "SubItem" };
            
            var mainTemplate = new TObject();

            var arrItemTemplate = new TObject();
            arrItemTemplate.Add<TString>("Name");

            var objArrTemplate = mainTemplate.Add<TObjArr>("Recursives");
            objArrTemplate.ElementType = arrItemTemplate;

            var arrSubItemTemplate = new TObject();
			arrSubItemTemplate.Add<TString>("Name");

            objArrTemplate = arrItemTemplate.Add<TObjArr>("Recursives");
            objArrTemplate.ElementType = arrSubItemTemplate;

            var json = new Json();
            json.Template = mainTemplate;
            json.Data = data;
            json.Session = new Session();

            var patch = jsonPatch.CreateJsonPatch(json.Session, true, false);
            Helper.ConsoleWriteLine(patch);
            Helper.ConsoleWriteLine("");

            item.Recursives.Add(subItem);
            data.Recursives.Add(item);

            patch = jsonPatch.CreateJsonPatch(json.Session, true, false);
            Helper.ConsoleWriteLine(patch);
            Helper.ConsoleWriteLine("");

            var expected = string.Format(Helper.ONE_ADD_PATCH_ARR, "/Recursives/0", @"{""Name"":""Item"",""Recursives"":[{""Name"":""SubItem""}]}");
            Assert.AreEqual(expected, patch);
            
            data.Recursives[0].Recursives.Add(subItem);
            patch = jsonPatch.CreateJsonPatch(json.Session, true, false);

            Helper.ConsoleWriteLine(patch);
            Helper.ConsoleWriteLine("");

            expected = string.Format(Helper.ONE_ADD_PATCH_ARR, "/Recursives/0/Recursives/1", @"{""Name"":""SubItem""}");
            Assert.AreEqual(expected, patch);
        }

        [Test]
        public static void TestSuppressingInputChange() {
            TObject schema;

            schema = TObject.CreateFromMarkup<Json, TObject>("json", File.ReadAllText("json\\simple.json"), "Simple");
            dynamic json = schema.CreateInstance();
            var session = new Session();
            session.Data = json;

            // Handler with no change of input value. i.e. change should not be sent back to client.
            TString tvalue1 = ((TString)schema.Properties[0]);
            tvalue1.AddHandler(
                Helper.CreateInput<string>,
                (Json pup, Starcounter.Input<string> input) => {
                    Helper.ConsoleWriteLine("Handler for VirtualValue called.");
                }
            );

            // Handler with change of input value. i.e. change should be sent back to client.
            TString tvalue2 = ((TString)schema.Properties[1]);
            tvalue2.AddHandler(
                Helper.CreateInput<string>,
                (Json pup, Starcounter.Input<string> input) => {
                    Helper.ConsoleWriteLine("Handler for AbstractValue called.");
                    input.Value = "Changed";
                }
            );

            // Flush all current changes.
            session.GenerateChangeLog();
            jsonPatch.CreateJsonPatch(session, true, false);

            // Call handler with no change of input value.
            tvalue1.ProcessInput(json, "Incoming");

            session.GenerateChangeLog();
            string patch = jsonPatch.CreateJsonPatch(session, true, false);

            Assert.AreEqual("Incoming", tvalue1.Getter(json));
            Assert.AreEqual("[]", patch);

            // Call handler that changes input value.
            tvalue2.ProcessInput(json, "Incoming");

            session.GenerateChangeLog();
            patch = jsonPatch.CreateJsonPatch(session, true, false);

            Assert.AreEqual("Changed", tvalue2.Getter(json));
            Assert.AreEqual(@"[{""op"":""replace"",""path"":""/AbstractValue$"",""value"":""Changed""}]", patch);
        }

        [Test]
        public static void TestCancellingInputChange() {
            TObject schema;

            schema = TObject.CreateFromMarkup<Json, TObject>("json", File.ReadAllText("json\\simple.json"), "Simple");
            dynamic json = schema.CreateInstance();
            var session = new Session();
            session.Data = json;

            TString tvalue1 = ((TString)schema.Properties[0]);
            tvalue1.AddHandler(
                Helper.CreateInput<string>,
                (Json pup, Starcounter.Input<string> input) => {
                    Helper.ConsoleWriteLine("Handler for VirtualValue called.");
                    input.Cancel();
                }
            );

            TString tvalue2 = ((TString)schema.Properties[1]);
            tvalue2.AddHandler(
                Helper.CreateInput<string>,
                (Json pup, Starcounter.Input<string> input) => {
                    Helper.ConsoleWriteLine("Handler for AbstractValue called.");
                    input.Cancel();
                }
            );

            // Set initial values and flush all current changes.
            tvalue1.Setter(json, "Value1");
            tvalue2.Setter(json, "Value2");
            session.GenerateChangeLog();
            jsonPatch.CreateJsonPatch(session, true, false);

            // Call handler with different incoming value as on the server, value should be sent back to client.
            tvalue1.ProcessInput(json, "Incoming");

            session.GenerateChangeLog();
            string patch = jsonPatch.CreateJsonPatch(session, true, false);

            Assert.AreEqual("Value1", tvalue1.Getter(json));
            Assert.AreEqual(@"[{""op"":""replace"",""path"":""/VirtualValue$"",""value"":""Value1""}]", patch);

            // Call handler with same input-value as on the server, value still should be sent back.
            tvalue2.ProcessInput(json, "Value2");

            session.GenerateChangeLog();
            patch = jsonPatch.CreateJsonPatch(session, true, false);

            Assert.AreEqual("Value2", tvalue2.Getter(json));
            Assert.AreEqual(@"[{""op"":""replace"",""path"":""/AbstractValue$"",""value"":""Value2""}]", patch);
        }

        [Test]
        public static void TestUpgradeToViewmodelAddToSession() {
            dynamic json = new Json();
            json.Value = "A";
            var tobj = (TObject)json.Template;

            // No dirtycheck enabled.
            Assert.IsFalse(json.IsDirty(tobj.Properties[0]));

            // Dirtycheck enabled when adding to session.
            Session session = new Session();
            session.Data = json;
            session.CheckpointChangeLog();

            json.Value = "B";
            Assert.IsTrue(json.IsDirty(tobj.Properties[0]));
        }

        [Test]
        public static void TestUpgradeToViewmodelAddChild() {
            dynamic json = new Json();
            dynamic page = new Json();
            page.Value = "A";
            var tobj = (TObject)page.Template;

            // Dirtycheck enabled when adding to session.
            Session session = new Session();
            session.Data = json;
            session.CheckpointChangeLog();

            // No dirtycheck enabled for page.
            Assert.IsFalse(page.IsDirty(tobj.Properties[0]));

            json.Page = page;
            session.CheckpointChangeLog();

            json.Page.Value = "B";
            Assert.IsTrue(page.IsDirty(tobj.Properties[0]));
        }

        [Test]
        public static void TestSingleObjectDisableCheckBoundProperties() {
            dynamic json = new Json();
            
            var person = new Person() {
                FirstName = "A",
                LastName = "B"
            };

            json.Data = person;
            var tJson = (TObject)json.Template;
           
            Session session = new Session();
            session.Data = json;
            session.CheckpointChangeLog();
            session.ClearChangeLog();

            Assert.AreEqual("A", json.FirstName);

            // First test that the autocheck works.
            person.FirstName = "Changed";
            session.GenerateChangeLog();
            var changes = session.GetChanges();
           
            Assert.AreEqual(1, changes.Count);
            Assert.AreEqual(tJson.Properties[0], changes[0].Property);

            session.CheckpointChangeLog();
            session.ClearChangeLog();

            // Then disable it and change value again.
            json.AutoRefreshBoundProperties = false;
            person.FirstName = "ChangedAgain";
            session.GenerateChangeLog();
            changes = session.GetChanges();
            
            Assert.AreEqual(0, changes.Count);

            session.CheckpointChangeLog();
            session.ClearChangeLog();

            // Last, manual refresh
            json.Refresh(tJson.Properties[0]);
            session.GenerateChangeLog();
            changes = session.GetChanges();
            
            Assert.AreEqual(1, changes.Count);
            Assert.AreEqual(tJson.Properties[0], changes[0].Property);

            session.CheckpointChangeLog();
            session.ClearChangeLog();
        }

        [Test]
        public static void TestArrayDisableCheckBoundProperties() {
            var schema = new TObject();
            var tarr = schema.Add<TArray<Json>>("Recursives");
            tarr.ElementType = new TObject();
            tarr.ElementType.Add<TString>("Name");

            dynamic json = new Json() { Template = schema };
            
            var recursive = new Recursive() { Name = "R1" };
            recursive.Recursives.Add(new Recursive() { Name = "R2" });

            json.Data = recursive;

            Session session = new Session();
            session.Data = json;
            session.CheckpointChangeLog();
            session.ClearChangeLog();

            Assert.AreEqual(recursive.Recursives.Count, json.Recursives.Count);
            recursive.Recursives.Add(new Recursive() { Name = "R3" });
            Assert.AreEqual(recursive.Recursives.Count, json.Recursives.Count);

            session.CheckpointChangeLog();
            session.ClearChangeLog();

            json.AutoRefreshBoundProperties = false;
            recursive.Recursives.Add(new Recursive() { Name = "R4" });

            Assert.AreNotEqual(recursive.Recursives.Count, json.Recursives.Count);

            session.GenerateChangeLog();
            var changes = session.GetChanges();

            Assert.AreEqual(0, changes.Count);

            session.CheckpointChangeLog();
            session.ClearChangeLog();

            json.Refresh(tarr);
            Assert.AreEqual(recursive.Recursives.Count, json.Recursives.Count);
        }
    }
}
