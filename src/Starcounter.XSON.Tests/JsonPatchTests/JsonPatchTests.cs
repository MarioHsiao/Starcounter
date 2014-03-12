﻿using NUnit.Framework;
using Starcounter.Internal.XSON.Tests;
using Starcounter.Templates;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Starcounter.Internal.XSON.Tests {
    [TestFixture]
    class JsonPatchTests {
        /// <summary>
        /// Sets up the test.
        /// </summary>
        [TestFixtureSetUp]
        public static void Setup()
        {
            // Initializing global sessions.
            GlobalSessions.InitGlobalSessions(1);
        }

		[TearDown]
		public static void AfterTest() {
			// Making sure that we are ending the session even if the test failed.
			Session.End();
		}

        [Test]
        public static void TestSimpleJsonPatch() {
            dynamic j = new Json();
            dynamic nicke = new Json();
            dynamic daughter = new Json();

            daughter.FirstName = "Kate";
            nicke.FirstName = "Nicke";

            j.FirstName = "Joachim";
            j.Age = 43;
            j.Length = 184.7;

            Session.Current = new Session() { Data = j };

            j.FirstName = "Douglas";

            var before = ((Json)j).DebugString;
//            Session.Current.CheckpointChangeLog();
            string str = Session.Current.CreateJsonPatch(true);

            j.Daughter = daughter;
            j.FirstName = "Timothy";
            j.LastName = "Wester";
            j.FirstName = "Charlie";

            var after = ((Json)j).DebugString;
            var result = Session.Current.CreateJsonPatch(true);

            Write("Before",before);
            Write("After",after);
            Write("Changes",result);

            string facit = "[{\"op\":\"replace\",\"path\":\"/FirstName\",\"value\":\"Charlie\"},\n{\"op\":\"replace\",\"path\":\"/Daughter\",\"value\":{\"FirstName\":\"Kate\"}},\n{\"op\":\"replace\",\"path\":\"/LastName\",\"value\":\"Wester\"}]";
            Assert.AreEqual(facit, result);
        }

        protected static void Write(string title, string value) {
            Console.WriteLine();
            Console.WriteLine(title);
            Console.WriteLine(new String('=', title.Length));
            Console.WriteLine(value);
        }

        [Test]
        public static void TestDirtyFlagsWithoutBinding() {
            TObject.UseCodegeneratedSerializer = false;

            dynamic j = new Json();
            dynamic nicke = new Json();
            nicke.FirstName = "Nicke";
            dynamic henrik = new Json();
            henrik.FirstName = "Henrik";

            j.FirstName = "Joachim";
            j.Age = 42;
            j.Length = 184.7;
            j.Friends = new List<Json>() { nicke };

            Session.Current = new Session() { Data = j };

            j.Friends.Add(henrik);

            Write("New stuff",((Json)j).DebugString);

//            Session.Current.CheckpointChangeLog();            
            Session.Current.CreateJsonPatch(true);

            Console.WriteLine("Flushed");
            Console.WriteLine("=========");
            Console.WriteLine(((Json)j).DebugString);

            var str = Session.Current.CreateJsonPatch(true);
            Assert.AreEqual("[]", str);

            j.Friends[1].FirstName = "Henke";
            j.Age = 43;
            dynamic kalle = new Json();
            kalle.FirstName = "Kalle";
            j.Friends.Add(kalle);

            Console.WriteLine("Changed");
            Console.WriteLine("=========");
            Console.WriteLine(((Json)j).DebugString);

            str = Session.Current.CreateJsonPatch(true);

            Console.WriteLine("JSON-Patch");
            Console.WriteLine("==========");
            Console.WriteLine(str);

            Assert.AreEqual("[{\"op\":\"replace\",\"path\":\"/Age\",\"value\":43},\n{\"op\":\"replace\",\"path\":\"/Friends/2\",\"value\":{\"FirstName\":\"Kalle\"}},\n{\"op\":\"replace\",\"path\":\"/Friends/1/FirstName\",\"value\":\"Henke\"}]",str);
        }

      //  [Test]
        public static void TestDirtyFlagsWithBinding() {
            TObject.UseCodegeneratedSerializer = false;

            Person nickeDb = new Person();
            Person jockeDb = new Person();
            Person henrikDb = new Person();

            dynamic jockeJson = new Json();
            jockeJson.Data = jockeDb;
            dynamic nickeJson = new Json();
            nickeJson.Data = nickeDb;

            nickeDb.FirstName = "Nicke";

            dynamic henrikJson = new Json();
            henrikJson.Data = henrikDb;
            henrikJson.FirstName = "Henrik";

            jockeJson.FirstName = "Joachim";
            jockeJson.Age = 42;
            jockeJson.Length = 184.7;
            jockeJson.Friends = new List<Json>() { nickeJson };

            Session.Current = new Session() { Data = jockeJson };

            jockeJson.Friends.Add(henrikJson);

            Console.WriteLine("New stuff");
            Console.WriteLine("=========");
            Console.WriteLine(((Json)jockeJson).DebugString);

            //Session.Current.CheckpointChangeLog();
            Session.Current.CreateJsonPatch(true);

            Console.WriteLine("Flushed");
            Console.WriteLine("=========");
            Console.WriteLine(((Json)jockeJson).DebugString);

            var str = Session.Current.CreateJsonPatch(true);
            Assert.AreEqual("[]", str);

            jockeJson.Friends[1].FirstName = "Henke";
            jockeJson.Age = 43;
            dynamic kalle = new Json();
            kalle.FirstName = "Kalle";
            jockeJson.Friends.Add(kalle);

            Console.WriteLine("Changed");
            Console.WriteLine("=========");
            Console.WriteLine(((Json)jockeJson).DebugString);

            str = Session.Current.CreateJsonPatch(true);

            Console.WriteLine("JSON-Patch");
            Console.WriteLine("==========");
            Console.WriteLine(str);

            Assert.AreEqual("[{\"op\":\"replace\",\"path\":\"/Age\",\"value\":43},\n{\"op\":\"add\",\"path\":\"/Friends\",\"value\":{\"FirstName\":\"Kalle\"}},\n{\"op\":\"replace\",\"path\":\"/Friends/1/FirstName\",\"value\":\"Henke\"}]", str);
        }

     //   [Test]
        public static void TestJsonPatchSimpleMix() {
            TObject.UseCodegeneratedSerializer = false;

            dynamic j = new Json();
            dynamic nicke = new Json();
            nicke.FirstName = "Nicke";
            nicke.Age = 43;

            j.FirstName = "Joachim";
            j.Age = 43;
            j.Length = 184.7;
            j.Friends = new List<Json>() { nicke };

            Session.Current = new Session() { Data = j };

            var before = ((Json)j).DebugString;
//            Session.Current.CheckpointChangeLog();
            Session.Current.CreateJsonPatch(true);

            //Session.Current.LogChanges = true;

//            Session.Data.LogChanges = true;
//            nicke.LogChanges = true;
//            ChangeLog.CurrentOnThread = new ChangeLog();

            j.FirstName = "Timothy";
            j.LastName = "Wester";
            nicke.FirstName = "Nicklas";
            nicke.LastName = "Hammarström";
            j.FirstName = "Charlie";
            j.Friends.Add().FirstName = "Henrik";

            var after = ((Json)j).DebugString;
            var result = Session.Current.CreateJsonPatch(true);

            Console.WriteLine("Before");
            Console.WriteLine("=====");
            Console.WriteLine(before);
            Console.WriteLine("");
            Console.WriteLine("After");
            Console.WriteLine("=====");
            Console.WriteLine(after);
            Console.WriteLine("");
            Console.WriteLine("Changes");
            Console.WriteLine("=====");
            Console.WriteLine(result);
            Console.WriteLine("");

            string facit = 
@"[{""op"":""replace"",""path"":""/FirstName"",""value"":""Charlie""},
{""op"":""add"",""path"":""/Friends"",""value"":{""FirstName"":""Henrik""}},
{""op"":""replace"",""path"":""/FirstName"",""value"":""Timothy""},
{""op"":""replace"",""path"":""/LastName"",""value"":""Wester""},
{""op"":""replace"",""path"":""/Friends/0/LastName"",""value"":""Hammarström""}}],
";
Assert.AreEqual(facit, result );
        }

        /// <summary>
        /// Database changes makes generating patches extra challenging obviously as there
        /// is no direct way to observe them. Especially as data object properties can be code
        /// properties (getters)
        /// </summary>
        [Test]
        public static void CreateSimpleDataBoundPatches() {
            var p = new Person();
            p.FirstName = "Joachim";
            p.LastName = "Wester";

            dynamic j = new Json();
            var json = (Json)j;

            Session.Current = new Session() { Data = j };
            
            var start = ((Json)j).DebugString;

            Assert.AreEqual("{}", json.ToJson()); // The data is not bound so the JSON should still be an empty object

            var t = new TObject();
            var fname = t.Add<TString>("FirstName"); 
            var lname = t.Add<TString>("LastName");
            j.Template = t;
            j.Data = p;

            Assert.IsTrue(!json.HasBeenSent);
            Assert.AreEqual("{\"FirstName\":\"Joachim\",\"LastName\":\"Wester\"}", ((Json)j).ToJson());

            Session.Current.CreateJsonPatch(true); // Flush
            var before = ((Json)j).DebugString;

            p.FirstName = "Douglas";

            var after = ((Json)j).DebugString;

            var patch = Session.Current.CreateJsonPatch(true);

            Console.WriteLine("Start");
            Console.WriteLine("=====");
            Console.WriteLine(start);
            Console.WriteLine("");
            Console.WriteLine("Before Change");
            Console.WriteLine("=============");
            Console.WriteLine(before);
            Console.WriteLine("");
            Console.WriteLine("After Change");
            Console.WriteLine("============");
            Console.WriteLine(after);
            Console.WriteLine("");
            Console.WriteLine("JSON-Patch");
            Console.WriteLine("==========");
            Console.WriteLine(patch);
            Console.WriteLine("");

            Assert.AreEqual("{\"FirstName\":\"Douglas\",\"LastName\":\"Wester\"}", ((Json)j).ToJson());
            Assert.AreEqual("[{\"op\":\"replace\",\"path\":\"/FirstName\",\"value\":\"Douglas\"}]",patch);
        }


        [Test]
        public static void TestPatchForBrandNewRoot() {
            dynamic j = new Json();
            dynamic nicke = new Json();

            Session.Current = new Session() { Data = j };

            Assert.NotNull(Session.Current);

            //Session.Data.LogChanges = true;
            //var cl = ChangeLog.CurrentOnThread = new ChangeLog();

            j.FirstName = "Jack";
            nicke.FirstName = "Nicke";
            //((Json)j).LogChanges = true;

            // Session.Current.LogChanges = true;

            j.Friends = new List<Json>() { nicke };

            Console.WriteLine("Dirty status");
            Console.WriteLine("============");
            Console.WriteLine(j.DebugString);


            var patch = Session.Current.CreateJsonPatch(true);

            Console.WriteLine("Changes:");
            Console.WriteLine("========");
            Console.WriteLine(patch);

            Assert.AreEqual(
                "[{\"op\":\"replace\",\"path\":\"/\",\"value\":{\"FirstName\":\"Jack\",\"Friends\":[{\"FirstName\":\"Nicke\"}]}}]", patch);
        }

        [Test]
        public static void TestIncomingPatches()
        {
            int patchCount;
            string patchSkel;
            string patchStr;
            byte[] patchBytes;
            TObject schema;
            

            patchSkel = "{{\"op\":\"replace\", \"path\":\"{0}\", \"value\":\"{1}\"}}";

            schema = TObject.CreateFromMarkup<Json, TObject>("json", File.ReadAllText("simple.json"), "Simple");
            dynamic json = schema.CreateInstance();

            // Setting a value on a editable property
            patchStr = string.Format(patchSkel, "/VirtualValue$", "Alpha");
            patchBytes = Encoding.UTF8.GetBytes(patchStr);
            patchCount = JsonPatch.JsonPatch.EvaluatePatches(json, patchBytes);
            Assert.AreEqual(1, patchCount);
            Assert.AreEqual("Alpha", json.VirtualValue);

            // Setting a value on a readonly property
            patchStr = string.Format(patchSkel, "/BaseValue", "Beta");
            patchBytes = Encoding.UTF8.GetBytes(patchStr);
            var ex = Assert.Throws<JsonPatch.JsonPatchException>(() => {
                JsonPatch.JsonPatch.EvaluatePatches(json, patchBytes);
            });
            Console.WriteLine(ex.Message);

            // Setting values on three properties in one patch
            patchStr = "["
                       + string.Format(patchSkel, "/VirtualValue$", "Apa")
                       + ","
                       + string.Format(patchSkel, "/OtherValue$", 1395276000)
                       + ","
                       + string.Format(patchSkel, "/AbstractValue$", "Peta")
                       + "]";
            patchBytes = Encoding.UTF8.GetBytes(patchStr);
            patchCount = JsonPatch.JsonPatch.EvaluatePatches(json, patchBytes);
            Assert.AreEqual(3, patchCount);
            Assert.AreEqual("Apa", json.VirtualValue);
            Assert.AreEqual("Peta", json.AbstractValue);
            Assert.AreEqual(1395276000, json.OtherValue);

             // Making sure all patches are correctly parsed.
            patchStr = "["
                       + string.Format(patchSkel, "/Content/ApplicationPage/GanttData/ItemDropped/Date$", 1395276000)
                       + ","
                       + string.Format(patchSkel, "/Content/ApplicationPage/GanttData/ItemDropped/TemplateId$", "lm7")
                       + "]";
            patchBytes = Encoding.UTF8.GetBytes(patchStr);
            patchCount = JsonPatch.JsonPatch.EvaluatePatches(null, patchBytes);
            Assert.AreEqual(2, patchCount);
        }
    }
}
