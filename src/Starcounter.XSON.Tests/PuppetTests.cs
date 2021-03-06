﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Starcounter.Templates;
using Starcounter.Internal.XSON.Tests.CompiledJson;
using Starcounter.XSON;

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

            StarcounterEnvironment.AppName = "SingleApp";
            session.Data = root;

            Assert.IsTrue(session == root.Session);
            Assert.IsTrue(root == session.Data);

            root.Session = null;
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
        public static void TestDirtyCheckForNewRootObject() {
            dynamic json = new Json();
            var session = new Session();
            var jsonPatch = new JsonPatch();

            session.Data = json;
            json.Header = "Test";

            Assert.IsFalse(json.HasBeenSent);
            Assert.IsTrue(json.dirty);
            Assert.IsTrue(json.ChangeLog.BrandNew);

            string patch = jsonPatch.Generate(json, true, false);

            Assert.IsTrue(json.HasBeenSent);
            Assert.IsFalse(json.dirty);
            Assert.IsFalse(json.ChangeLog.BrandNew);
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
            string patch = jsonPatch.Generate(json, true, false);

            Helper.ConsoleWriteLine(patch);
            Helper.ConsoleWriteLine("");

            data.FirstName = "Bengt";
            patch = jsonPatch.Generate(json, true, false);

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
            Change[] changeArr;

            Json json = new Json() { 
                Template = schema 
            };
            json.Session = new Session();
            json.Set(tname, "Hans Brix");
            
            // Resetting dirtyflags.
            Assert.IsTrue(json.ChangeLog.Generate(true, out changeArr));
            
            json.Set(tname, "Apa Papa");
            Assert.IsTrue(json.IsDirty(tname));

            json.Set(tpage, new Json());
            Assert.IsTrue(json.IsDirty(tpage));

            json.Set(tarr, new Arr<Json>(json, tarr));
            Assert.IsTrue(json.IsDirty(tarr));

            Assert.IsTrue(json.ChangeLog.Generate(true, out changeArr));
            Assert.AreEqual(3, changeArr.Length);
        }
       
        [Test]
        public static void TestDirtyCheckForUnboundValuesWithCustomAccessors() {
            var schema = new TObject();
            var tname = schema.Add<TString>("Name", bind: null);
            var tpage = schema.Add<TObject>("Page", bind: null);
            var tarr = schema.Add<TArray<Json>>("Items", bind: null);
            Change[] changeArr;

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
            Assert.IsTrue(json.ChangeLog.Generate(true, out changeArr));
            json.ChangeLog.Checkpoint();
            json.ChangeLog.Clear();

            json.Set(tname, "Apa Papa");
            Assert.IsTrue(json.IsDirty(tname));

            json.Set(tpage, new Json());
            Assert.IsTrue(json.IsDirty(tpage));

            json.Set(tarr, new Arr<Json>(json, tarr));
            Assert.IsTrue(json.IsDirty(tarr));

            Assert.IsTrue(json.ChangeLog.Generate(true, out changeArr));
            Assert.AreEqual(3, changeArr.Length);
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

            var patch = jsonPatch.Generate(json, true, false);
            Helper.ConsoleWriteLine(patch);
            Helper.ConsoleWriteLine("");

            item.Recursives.Add(subItem);
            data.Recursives.Add(item);

            patch = jsonPatch.Generate(json, true, false);
            Helper.ConsoleWriteLine(patch);
            Helper.ConsoleWriteLine("");

            var expected = string.Format(Helper.ONE_ADD_PATCH_ARR, "/Recursives/0", @"{""Name"":""Item"",""Recursives"":[{""Name"":""SubItem""}]}");
            Assert.AreEqual(expected, patch);
            
            data.Recursives[0].Recursives.Add(subItem);
            patch = jsonPatch.Generate(json, true, false);

            Helper.ConsoleWriteLine(patch);
            Helper.ConsoleWriteLine("");

            expected = string.Format(Helper.ONE_ADD_PATCH_ARR, "/Recursives/0/Recursives/1", @"{""Name"":""SubItem""}");
            Assert.AreEqual(expected, patch);
        }

        [Test]
        public static void TestDirtyCheckForBoundArrayWithoutTrackingChanges() {
            var json = new simplewithcodebehind();
            var items = json.Items;
            items = json.Items;
        }

        [Test]
        public static void TestSuppressingInputChange() {
            TObject schema;

            schema = (TObject)Template.CreateFromMarkup("json", File.ReadAllText("json\\simple.json"), "Simple");
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
            jsonPatch.Generate(json, true, false);

            // Call handler with no change of input value.
            tvalue1.ProcessInput(json, "Incoming");

            string patch = jsonPatch.Generate(json, true, false);

            Assert.AreEqual("Incoming", tvalue1.Getter(json));
            Assert.AreEqual("[]", patch);

            // Call handler that changes input value.
            tvalue2.ProcessInput(json, "Incoming");

            patch = jsonPatch.Generate(json, true, false);

            Assert.AreEqual("Changed", tvalue2.Getter(json));
            Assert.AreEqual(@"[{""op"":""replace"",""path"":""/AbstractValue$"",""value"":""Changed""}]", patch);
        }

        [Test]
        public static void TestSuppressingInputChangeSetterNotChangingValue() {
            var schema = new TObject();
            var tName = schema.Add<TString>("Name");
            var tName2 = schema.Add<TString>("Name2");

            tName.SetCustomAccessors(
                (parent) => { return "Static"; },
                (parent, value) => { }
            );

            tName2.SetCustomAccessors(
                (parent) => { return "Static2"; },
                (parent, value) => { }
            );

            tName2.AddHandler(
                Helper.CreateInput<string>,
                (Json pup, Starcounter.Input<string> input) => {
                    // Do nothing, simply accept value.                    
                }
            );
            
            Json json = (Json)schema.CreateInstance();
            var session = new Session();
            session.Data = json;

            Change[] changes;
            Assert.IsTrue(json.ChangeLog.Generate(true, out changes));

            // No inputhandler, value will simply be set.
            tName.ProcessInput(json, "ClientValue");
            Assert.IsTrue(json.ChangeLog.Generate(true, out changes));

            Assert.AreEqual(1, changes.Length);
            Assert.AreEqual(tName, changes[0].Property);

            // Have an inputhandler, will call that one first and then set value.
            tName2.ProcessInput(json, "ClientValue");
            Assert.IsTrue(json.ChangeLog.Generate(true, out changes));

            Assert.AreEqual(1, changes.Length);
            Assert.AreEqual(tName2, changes[0].Property);
        }

        [Test]
        public static void TestCancellingInputChange() {
            TObject schema;

            schema = (TObject)Template.CreateFromMarkup("json", File.ReadAllText("json\\simple.json"), "Simple");
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
            jsonPatch.Generate(json, true, false);

            // Call handler with different incoming value as on the server, value should be sent back to client.
            tvalue1.ProcessInput(json, "Incoming");

            string patch = jsonPatch.Generate(json, true, false);

            Assert.AreEqual("Value1", tvalue1.Getter(json));
            Assert.AreEqual(@"[{""op"":""replace"",""path"":""/VirtualValue$"",""value"":""Value1""}]", patch);

            // Call handler with same input-value as on the server, value still should be sent back.
            tvalue2.ProcessInput(json, "Value2");

            patch = jsonPatch.Generate(json, true, false);

            Assert.AreEqual("Value2", tvalue2.Getter(json));
            Assert.AreEqual(@"[{""op"":""replace"",""path"":""/AbstractValue$"",""value"":""Value2""}]", patch);
        }

        [Test]
        public static void TestProcessInputForNonStatefulJson() {
            TObject schema;

            schema = (TObject)TObject.CreateFromMarkup("json", File.ReadAllText("json\\simple.json"), "Simple");
            dynamic json = schema.CreateInstance();
            bool handlerCalled = false;

            TString tvalue1 = ((TString)schema.Properties[0]);
            tvalue1.AddHandler(
                Helper.CreateInput<string>,
                (Json pup, Starcounter.Input<string> input) => {
                    handlerCalled = true;
                }
            );
            TString tvalue2 = ((TString)schema.Properties[1]);
            tvalue2.AddHandler(
                Helper.CreateInput<string>,
                (Json pup, Starcounter.Input<string> input) => {
                    handlerCalled = true;
                    input.Cancel();
                }
            );

            TString tvalue3 = ((TString)schema.Properties[2]);
            tvalue3.AddHandler(
                Helper.CreateInput<string>,
                (Json pup, Starcounter.Input<string> input) => {
                    handlerCalled = true;
                    input.Value = "ChangedValue3";
                }
            );

            tvalue1.Setter(json, "Value1");
            tvalue2.Setter(json, "Value2");
            tvalue3.Setter(json, "Value3");

            handlerCalled = false;
            tvalue1.ProcessInput(json, "InputValue1");
            Assert.IsTrue(handlerCalled);
            Assert.AreEqual("InputValue1", tvalue1.Getter(json));

            handlerCalled = false;
            tvalue2.ProcessInput(json, "InputValue2");
            Assert.IsTrue(handlerCalled);
            Assert.AreEqual("Value2", tvalue2.Getter(json)); // handler cancelled.

            handlerCalled = false;
            tvalue3.ProcessInput(json, "InputValue3");
            Assert.IsTrue(handlerCalled);
            Assert.AreEqual("ChangedValue3", tvalue3.Getter(json)); // value changed in handler
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
            json.ChangeLog.Checkpoint();

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
            json.ChangeLog.Checkpoint();

            // No dirtycheck enabled for page.
            Assert.IsFalse(page.IsDirty(tobj.Properties[0]));

            json.Page = page;
            json.ChangeLog.Checkpoint();

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
            json.ChangeLog.Checkpoint();
            json.ChangeLog.Clear();

            Assert.AreEqual("A", json.FirstName);

            // First test that the autocheck works.
            person.FirstName = "Changed";
            Change[] changes;
            Assert.IsTrue(json.ChangeLog.Generate(true, out changes));
           
            Assert.AreEqual(1, changes.Length);
            Assert.AreEqual(tJson.Properties[0], changes[0].Property);

            json.ChangeLog.Checkpoint();
            

            // Then disable it and change value again.
            json.AutoRefreshBoundProperties = false;
            person.FirstName = "ChangedAgain";

            Assert.IsTrue(json.ChangeLog.Generate(true, out changes));
            Assert.AreEqual(0, changes.Length);
            json.ChangeLog.Checkpoint();
            

            // Last, manual refresh
            json.Refresh(tJson.Properties[0]);
            Assert.IsTrue(json.ChangeLog.Generate(true, out changes));
            
            Assert.AreEqual(1, changes.Length);
            Assert.AreEqual(tJson.Properties[0], changes[0].Property);

            json.ChangeLog.Checkpoint();
        }

        [Test]
        public static void TestArrayDisableCheckBoundProperties() {
            var schema = new TObject();
            var tarr = schema.Add<TArray<Json>>("Recursives");

            var elementType = new TObject();
            elementType.Add<TString>("Name");
            tarr.ElementType = elementType;

            dynamic json = new Json() { Template = schema };
            
            var recursive = new Recursive() { Name = "R1" };
            recursive.Recursives.Add(new Recursive() { Name = "R2" });

            json.Data = recursive;

            Session session = new Session();
            session.Data = json;
            json.ChangeLog.Checkpoint();
            json.ChangeLog.Clear();

            Assert.AreEqual(recursive.Recursives.Count, json.Recursives.Count);
            recursive.Recursives.Add(new Recursive() { Name = "R3" });
            Assert.AreEqual(recursive.Recursives.Count, json.Recursives.Count);

            json.ChangeLog.Checkpoint();
            json.ChangeLog.Clear();

            json.AutoRefreshBoundProperties = false;
            recursive.Recursives.Add(new Recursive() { Name = "R4" });

            Assert.AreNotEqual(recursive.Recursives.Count, json.Recursives.Count);

            Change[] changes;
            Assert.IsTrue(json.ChangeLog.Generate(true, out changes));
            Assert.AreEqual(0, changes.Length);
            json.ChangeLog.Checkpoint();

            json.Refresh(tarr);
            Assert.AreEqual(recursive.Recursives.Count, json.Recursives.Count);
        }

        [Test]
        public static void TestSerializeWithNamespacesDisabled() {
            person p = new person();
            p.appName = "MainApp";
            p.wrapInAppName = true;
            p.ExtraInfo.appName = "MainApp";
            p.ExtraInfo.wrapInAppName = true;

            supersimple ss = new supersimple();
            ss.appName = "PartialApp";
            ss.wrapInAppName = true;

            SiblingList stepSiblings = new SiblingList();
            stepSiblings.Add(p.ExtraInfo);
            stepSiblings.Add(ss);

            p.ExtraInfo.Siblings = stepSiblings;
            ss.Siblings = stepSiblings;

            var session = new Session(SessionOptions.IncludeNamespaces);
            p.Session = session;

            // No namespaces.
            var jsonStr = ss.ToJson();
            Assert.AreEqual(@"{""PlayerId"":123,""Name"":""Arne""}", jsonStr);

            jsonStr = p.ExtraInfo.ToJson();
            Assert.AreEqual(@"{""Text"":""1asf32""}", jsonStr);

            jsonStr = p.ToJson();
            Assert.AreEqual(
                @"{""FirstName$"":""Arne"",""LastName"":""Anka"",""Age"":19,""Stats"":23.987,""Fields"":[],""ExtraInfo"":{""Text"":""1asf32""}}",
                jsonStr
            );
        }

        [Test]
        public static void TestSerializeWithNamespacesEnabled() {
            person p = new person();
            p.appName = "MainApp";
            p.wrapInAppName = true;
            p.ExtraInfo.appName = "MainApp";
            p.ExtraInfo.wrapInAppName = true;

            supersimple ss = new supersimple();
            ss.appName = "PartialApp";
            ss.wrapInAppName = true;

            SiblingList stepSiblings = new SiblingList();
            stepSiblings.Add(p.ExtraInfo);
            stepSiblings.Add(ss);

            p.ExtraInfo.Siblings = stepSiblings;
            ss.Siblings = stepSiblings;

            var session = new Session(SessionOptions.IncludeNamespaces);
            session.enableNamespaces = true;
            try {
                p.Session = session;

                // No namespaces.
                var jsonStr = ss.ToJson();
                Assert.AreEqual(@"{""PartialApp"":{""PlayerId"":123,""Name"":""Arne""},""MainApp"":{""Text"":""1asf32""}}", jsonStr);

                jsonStr = p.ExtraInfo.ToJson();
                Assert.AreEqual(@"{""MainApp"":{""Text"":""1asf32""},""PartialApp"":{""PlayerId"":123,""Name"":""Arne""}}", jsonStr);

                jsonStr = p.ToJson();
                Assert.AreEqual(
                    @"{""FirstName$"":""Arne"",""LastName"":""Anka"",""Age"":19,""Stats"":23.987,""Fields"":[],""ExtraInfo"":{""MainApp"":{""Text"":""1asf32""},""PartialApp"":{""PlayerId"":123,""Name"":""Arne""}}}",
                    jsonStr
                );
            } finally {
                session.enableNamespaces = false;
            }
        }

        [Test]
        public static void TestDirtyCheckWithDataObjectsWithDifferentTypes() {
            var schema = new TObject();
            var tObjectNo = schema.Add<TLong>("ObjectNo");
            var tName = schema.Add<TString>("Name");
            var tStreet = schema.Add<TString>("Street");
            var tMisc = schema.Add<TString>("Misc");

            tStreet.DefaultValue = "MyStreet";
            tMisc.DefaultValue = "Misc";

            var dataObject1 = new Agent() {
                ObjectNo = 1,
                Name = "Agent"
            };

            var dataObject2 = new Address() {
                ObjectNo = 2,
                Street = "Street"
            };

            var json = new Json() { Template = schema };
            var session = new Session();

            session.Data = json;

            Change[] changes;
            
            session.Use(() => {
                Assert.IsTrue(json.ChangeLog.Generate(true, out changes));
                
                // Bound properties: ObjectNo, Name
                // Unbound properties: Street, Misc
                // Changes should be ObjectNo, Name
                json.Data = dataObject1;
                Assert.IsTrue(json.ChangeLog.Generate(true, out changes));
                json.ChangeLog.Checkpoint();
                
                Assert.AreEqual(2, changes.Length);
                Assert.AreEqual(tObjectNo, changes[0].Property);
                Assert.AreEqual(tName, changes[1].Property);

                // Bound properties: ObjectNo, Street
                // Unbound properties: Name, Misc
                // Changes should be ObjectNo, Name, Street
                json.Data = dataObject2;
                Assert.IsTrue(json.ChangeLog.Generate(true, out changes));
                json.ChangeLog.Checkpoint();

                Assert.AreEqual(3, changes.Length);
                Assert.AreEqual(tObjectNo, changes[0].Property);
                Assert.AreEqual(tName, changes[1].Property);
                Assert.AreEqual(tStreet, changes[2].Property);

                // Make sure values that are used for dirtychecking is resetted.
                //                tStreet.CheckAndSetBoundValue(json, false);

                // Bound properties: ObjectNo, Name
                // Unbound properties: Street, Misc
                // Changes should be ObjectNo, Name, Street, 
                json.Data = dataObject1;
                Assert.IsTrue(json.ChangeLog.Generate(true, out changes));
                json.ChangeLog.Checkpoint();

                Assert.AreEqual(3, changes.Length);
                Assert.AreEqual(tObjectNo, changes[0].Property);
                Assert.AreEqual(tName, changes[1].Property);
                Assert.AreEqual(tStreet, changes[2].Property);

                // Make sure values that are used for dirtychecking is resetted.
                //tName.CheckAndSetBoundValue(json, false);

                // Bound properties: 
                // Unbound properties: ObjectNo, Name, Street, Misc
                // Changes should be ObjectNo, Name
                json.Data = null;
                Assert.IsTrue(json.ChangeLog.Generate(true, out changes));
                json.ChangeLog.Checkpoint();

                Assert.AreEqual(2, changes.Length);
                Assert.AreEqual(tObjectNo, changes[0].Property);
                Assert.AreEqual(tName, changes[1].Property);
            });
        }

        [Test]
        public static void TestDirtyCheckForSiblings() {
            dynamic root = new Json();
            dynamic page = new Json();
            var session = new Session();

            page.Title = "Page";
            root.Page = page;
            session.Data = root;

            Change[] changes;
            Assert.IsTrue(root.ChangeLog.Generate(true, out changes));

            dynamic sibling = new Json();
            
            SiblingList siblings = new SiblingList();
            siblings.Add(page);
            siblings.Add(sibling);

            ((Json)page).Siblings = siblings;
            ((Json)sibling).Siblings = siblings;

            sibling.Name = "Sibling";
            
            Assert.IsTrue(sibling.dirty);
            Assert.IsFalse(sibling.HasBeenSent);
            Assert.IsFalse(sibling.IsDirty(((TObject)sibling.Template).Properties[0])); // Will be false since the parent is not sent
            
            Assert.IsTrue(root.ChangeLog.Generate(true, out changes));

            Assert.IsFalse(sibling.IsDirty(((TObject)sibling.Template).Properties[0]));
            Assert.IsFalse(sibling.dirty);
            Assert.IsTrue(sibling.HasBeenSent);
        }

        [Test]
        public static void TestReplaceSessionDataWithVersioning_3418() {
            Change[] changes;
            dynamic json = new Json();
            json.Name = "First";

            var session = new Session(SessionOptions.PatchVersioning);
            session.Data = json;

            ChangeLog changeLog = json.ChangeLog;

            changeLog.Generate(true, out changes);
            changeLog.Generate(true, out changes);

            Assert.AreEqual(2, changeLog.Version.LocalVersion);

            dynamic json2 = new Json();
            json2.Name = "Second";

            session.Data = json2;
            changeLog = json2.ChangeLog;

            Assert.AreEqual(2, changeLog.Version.LocalVersion);

            changeLog.Generate(true, out changes);

            Assert.AreEqual(3, changeLog.Version.LocalVersion);
            Assert.AreEqual(1, changes.Length);
            Assert.IsNull(changes[0].Property);
            Assert.AreEqual(json2, changes[0].Parent);
        }

        [Test]
        public static void TestDirtyCheckForReplacingStatefulSibling_3583() {
            // 20160816: Test modified after discussion and investigation which concluded that 
            // the implementation for #3583 was not correct. Replacing a stateful sibling
            // should lead to that the previous stateful sibling will be stateless...
            // https://github.com/Starcounter/Starcounter/issues/3786
            
            dynamic root = new Json();
            dynamic page = new Json();

            var session = new Session();

            page.Title = "Page";
            root.Page = page;
            session.Data = root;

            Change[] changes;
            Assert.IsTrue(root.ChangeLog.Generate(true, out changes));
            
            dynamic siblingRoot = new Json();
            dynamic sibling = new Json();
            siblingRoot.Current = sibling;

            SiblingList siblings = new SiblingList();
            siblings.Add(page);
            siblings.Add(sibling);

            ((Json)page).Siblings = siblings;
            ((Json)sibling).Siblings = siblings;

            sibling.Name = "Sibling";

            Assert.IsTrue(sibling.IsDirty());
            Assert.IsFalse(sibling.HasBeenSent);
            Assert.IsFalse(sibling.IsDirty(((TObject)sibling.Template).Properties[0])); // Will be false since the parent is not sent

            Assert.IsTrue(root.ChangeLog.Generate(true, out changes));

            // Replacing Current on siblingRoot should not lead to that the siblings are updated.
            sibling = new Json();
            sibling.Header = "New sibling";
            siblingRoot.Current = sibling;

            Assert.IsTrue(root.ChangeLog.Generate(true, out changes));

            Assert.AreEqual(0, changes.Length);
        }

        [Test]
        public void TestAccessToBoundPrimitiveProperty() {
            var schema = new TObject();
            var tName = schema.Add<TString>("Name");
            var data = new PropertyAccessCounter();

            Json json = (Json)schema.CreateInstance();
            json.Data = data;

            data.Name = "JohnDoe";
            data.ResetCount();

            // First test simple serialization. 
            string str = json.ToJson();

            // No caching should be enabled. Each property is accessed twice (estimate, serialize)
            Assert.AreEqual(1, data.GetNameCount);
            Assert.AreEqual(0, data.SetNameCount);

            json.Set(tName, "Nils Nilsson");
            Assert.AreEqual(1, data.GetNameCount);
            Assert.AreEqual(1, data.SetNameCount);
            Assert.AreEqual("Nils Nilsson", data.NameSkipCounter);

            data.ResetCount();
            
            var session = new Session();
            session.Data = json;

            str = json.ToJson();

            Assert.AreEqual(1, data.GetNameCount);
            Assert.AreEqual(0, data.SetNameCount);

            json.VerifyDirtyFlags();
            
            data.ResetCount();
            session.Use(() => {
                var jsonPatch = new JsonPatch();
                str = jsonPatch.Generate(json, true, false);
                json.Set(tName, "Arne Anka");
                
                data.ResetCount();
                str = jsonPatch.Generate(json, true, false);

                json.VerifyDirtyFlags();

                Assert.AreEqual(1, data.GetNameCount);
                Assert.AreEqual(0, data.SetNameCount);
                Assert.IsFalse(json.IsCached(tName));
         
                data.Name = "John Doe";
                data.ResetCount();

                str = jsonPatch.Generate(json, true, false);

                json.VerifyDirtyFlags();

                Assert.AreEqual(1, data.GetNameCount);
                Assert.AreEqual(0, data.SetNameCount);
                Assert.IsFalse(json.IsCached(tName));
            });
            
            // Verifying that name is cached in unbound storage.
            Assert.AreEqual(data.NameSkipCounter, tName.UnboundGetter(json));
        }

        [Test]
        public void TestAccessToBoundObjectProperty() {
            var schema = new TObject();
            var tAgent = schema.Add<TObject>("Agent");
            
            tAgent.Add<TString>("Name");
            var data = new PropertyAccessCounter();
            data.Agent = new Agent() { Name = "John Doe" };

            Json json = (Json)schema.CreateInstance();
            json.Data = data;
            
            data.ResetCount();

            // First test simple serialization. 
            string str = json.ToJson();
            
            Assert.AreEqual(1, data.GetAgentCount);
            Assert.AreEqual(0, data.SetAgentCount);

            data.ResetCount();

            var newAgent = new Agent() { Name = "Nils Nilsson" };
            json.Set(tAgent, newAgent);
            Assert.AreEqual(0, data.GetAgentCount);
            Assert.AreEqual(1, data.SetAgentCount);
            Assert.AreEqual(newAgent, data.AgentSkipCounter);

            data.ResetCount();

            // Adding a Session (i.e. json will be stateful and propertyaccess during serialization cached).
            var session = new Session();
            session.Data = json;

            str = json.ToJson();

            Assert.AreEqual(1, data.GetAgentCount);
            Assert.AreEqual(0, data.SetAgentCount);

            json.VerifyDirtyFlags();

            // Verifying that name is cached in unbound storage.
            Assert.AreSame(data.AgentSkipCounter, tAgent.UnboundGetter(json).Data);

            // Resetting cached value.
            tAgent.UnboundSetter(json, (Json)tAgent.CreateInstance());

            data.ResetCount();
            session.Use(() => {
                var jsonPatch = new JsonPatch();
                str = jsonPatch.Generate(json, true, false);
                json.Set(tAgent, new Agent() { Name = "Apa Papa" });

                data.ResetCount();
                str = jsonPatch.Generate(json, true, false);

                json.VerifyDirtyFlags();

                Assert.AreEqual(1, data.GetAgentCount);
                Assert.AreEqual(0, data.SetAgentCount);
                Assert.IsFalse(json.IsCached(tAgent));

                data.Agent = new Agent() { Name = "John Doe" };
                data.ResetCount();

                str = jsonPatch.Generate(json, true, false);

                json.VerifyDirtyFlags();

                Assert.AreEqual(1, data.GetAgentCount);
                Assert.AreEqual(0, data.SetAgentCount);
                Assert.IsFalse(json.IsCached(tAgent));
            });

            // Verifying that name is cached in unbound storage.
            Assert.AreEqual(data.AgentSkipCounter, tAgent.UnboundGetter(json).Data);
        }

        [Test]
        public void TestDirtyCheckForBoundArrayDataSetToNull() {
            Recursive data = new Recursive();
            data.Name = "Root";
            data.Recursives.Add(new Recursive() { Name = "1" });
            data.Recursives.Add(new Recursive() { Name = "2" });

            TObject arrItemSchema = new TObject();
            arrItemSchema.Add<TString>("Name", "Name");

            TObject schema = new TObject();
            var nameTemplate = schema.Add<TString>("Name", "Name");
            var arrSchema = schema.Add<TArray<Json>>("Recursives", "Recursives");
            arrSchema.ElementType = arrItemSchema;
            
            dynamic json = new Json() { Template = schema };
            json.Data = data;
            json.Session = new Session();

            Assert.AreEqual("Root", json.Name);
            Assert.AreEqual(2, json.Recursives.Count);
            json.ChangeLog.Checkpoint();

            json.Data = null;
            Assert.AreEqual("", json.Name);
            Assert.AreEqual(0, json.Recursives.Count);
            Assert.IsTrue(json.IsDirty());
            Assert.IsTrue(json.IsDirty(nameTemplate));
            Assert.IsTrue(json.IsDirty(arrSchema));
        }
    }
}