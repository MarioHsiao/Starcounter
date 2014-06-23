using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using NUnit.Framework;
using Starcounter.Advanced.XSON;
using Starcounter.Templates;
using Starcounter.XSON.JsonPatch;

namespace Starcounter.Internal.XSON.Tests {
    [TestFixture]
    public class GenerateJsonPatchTests {
        /// <summary>
        /// Sets up the test.
        /// </summary>
        [TestFixtureSetUp]
        public static void Setup()
        {
            // Initializing global sessions.
            GlobalSessions.InitGlobalSessions(1);
            Json.DirtyCheckEnabled = true;
            StarcounterEnvironment.AppName = "Test";
        }

		[TearDown]
		public static void AfterTest() {
			// Making sure that we are ending the session even if the test failed.
			Session.End();
		}

        [Test]
        public static void TestPatchSizes() {
            byte[] patchArr;
            Change change;
            dynamic json;
            int patchSize;
            int pathSize;
            TObject schema;
            TValue property;
            string patch;
            string path;

            List<Change> changeList = new List<Change>(1);
            changeList.Add(new Change());
            
            // ["op":"replace","path":"/FirstName","value":"ApaPapa"]
            path = "/FirstName";
            patch = string.Format(Helper.PATCH, path, Helper.Jsonify("ApaPapa"));
            schema = new TObject();
            property = schema.Add<TString>("FirstName");
            json = new Json() { Template = schema };
            json.FirstName = "ApaPapa";
            change = Change.Update(json, property);
            patchSize = JsonPatch.CalculateSize(change, out pathSize);
            Assert.AreEqual(path.Length, pathSize);
            Assert.IsTrue(patchSize >= patch.Length); // size is estimated, but needs to be atleast size of patch
            changeList[0] = change;
            patchSize = JsonPatch.CreatePatches(changeList, out patchArr);
            Assert.AreEqual(patchSize, patchSize);

            // ["op":"replace","path":"/Focused/Age","value":19]
            path = "/Focused/Age";
            patch = string.Format(Helper.PATCH, path, 19);
            schema = new TObject();
            property = schema.Add<TObject>("Focused");
            property = ((TObject)property).Add<TLong>("Age");
            json = new Json() { Template = schema };
            json.Focused.Age = 19;
            change = Change.Update(json.Focused, property);
            patchSize = JsonPatch.CalculateSize(change, out pathSize);
            Assert.AreEqual(path.Length, pathSize);
            Assert.IsTrue(patchSize >= patch.Length); // size is estimated, but needs to be atleast size of patch
            changeList[0] = change;
            patchSize = JsonPatch.CreatePatches(changeList, out patchArr);
            Assert.AreEqual(patchSize, patchSize);

            // ["op":"replace","path":"/Items/0/Stats","value":23.5]
            path = "/Items/0/Stats";
            patch = string.Format(Helper.PATCH, path, 23.5d);
            schema = new TObject();
            var tarr = schema.Add<TArray<Json>>("Items");
            tarr.ElementType = new TObject();
            property = tarr.ElementType.Add<TDouble>("Stats");
            json = new Json() { Template = schema };
            json = json.Items.Add();
            json.Stats = 23.5d;
            change = Change.Update(json, property);
            patchSize = JsonPatch.CalculateSize(change, out pathSize);
            Assert.AreEqual(path.Length, pathSize);
            Assert.IsTrue(patchSize >= patch.Length); // size is estimated, but needs to be atleast size of patch
            changeList[0] = change;
            patchSize = JsonPatch.CreatePatches(changeList, out patchArr);
            Assert.AreEqual(patchSize, patchSize);


            // ["op":"replace","path":"/OtherApp/FirstName","value":"ApaPapa"]
            path = "/OtherApp/FirstName";
            patch = string.Format(Helper.PATCH, path, Helper.Jsonify("ApaPapa"));
            schema = new TObject();
            schema.Add<TLong>("Age");
            json = new Json() { Template = schema };
            json.Age = 19;
            var schema2 = new TObject();
            property = schema2.Add<TString>("FirstName");
            dynamic json2 = new Json() { Template = schema2 };
            json2.FirstName = "ApaPapa";
            JsonExtension.SetAppName(json2, "OtherApp");
            JsonExtension.AddStepSibling(json, json2);
            change = Change.Update(json2, property);
            patchSize = JsonPatch.CalculateSize(change, out pathSize);
            Assert.AreEqual(path.Length, pathSize);
            Assert.IsTrue(patchSize >= patch.Length); // size is estimated, but needs to be atleast size of patch
            changeList[0] = change;
            patchSize = JsonPatch.CreatePatches(changeList, out patchArr);
            Assert.AreEqual(patchSize, patchSize);

            // ["op":"replace","path":"/Focused/OtherApp/FirstName","value":"ApaPapa"]
            path = "/Focused/OtherApp/FirstName";
            patch = string.Format(Helper.PATCH, path, Helper.Jsonify("ApaPapa"));
            schema = new TObject();
            var focSchema = schema.Add<TObject>("Focused");
            focSchema.Add<TLong>("Age");
            json = new Json() { Template = schema };
            json.Focused.Age = 19;
            schema2 = new TObject();
            property = schema2.Add<TString>("FirstName");
            json2 = new Json() { Template = schema2 };
            json2.FirstName = "ApaPapa";
            JsonExtension.SetAppName(json2, "OtherApp");
            JsonExtension.AddStepSibling(json.Focused, json2);
            change = Change.Update(json2, property);
            patchSize = JsonPatch.CalculateSize(change, out pathSize);
            Assert.AreEqual(path.Length, pathSize);
            Assert.IsTrue(patchSize >= patch.Length); // size is estimated, but needs to be atleast size of patch
            changeList[0] = change;
            patchSize = JsonPatch.CreatePatches(changeList, out patchArr);
            Assert.AreEqual(patchSize, patchSize);

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
            string str = JsonPatch.CreateJsonPatch(Session.Current, true);

            j.Daughter = daughter;
            j.FirstName = "Timothy";
            j.LastName = "Wester";
            j.FirstName = "Charlie";

            var after = ((Json)j).DebugString;
            var result = JsonPatch.CreateJsonPatch(Session.Current, true);

            Write("Before",before);
            Write("After",after);
            Write("Changes",result);

            string facit = "[{\"op\":\"replace\",\"path\":\"/FirstName\",\"value\":\"Charlie\"},{\"op\":\"replace\",\"path\":\"/Daughter\",\"value\":{\"FirstName\":\"Kate\"}},{\"op\":\"replace\",\"path\":\"/LastName\",\"value\":\"Wester\"}]";
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

            JsonPatch.CreateJsonPatch(Session.Current, true);

            Console.WriteLine("Flushed");
            Console.WriteLine("=========");
            Console.WriteLine(((Json)j).DebugString);

            var str = JsonPatch.CreateJsonPatch(Session.Current, true);
            Assert.AreEqual("[]", str);

            j.Friends[1].FirstName = "Henke";
            j.Age = 43;
            dynamic kalle = new Json();
            kalle.FirstName = "Kalle";
            j.Friends.Add(kalle);

            Console.WriteLine("Changed");
            Console.WriteLine("=========");
            Console.WriteLine(((Json)j).DebugString);

            str = JsonPatch.CreateJsonPatch(Session.Current, true);

            Console.WriteLine("JSON-Patch");
            Console.WriteLine("==========");
            Console.WriteLine(str);

            Assert.AreEqual("[{\"op\":\"replace\",\"path\":\"/Age\",\"value\":43},{\"op\":\"add\",\"path\":\"/Friends/2\",\"value\":{\"FirstName\":\"Kalle\"}},{\"op\":\"replace\",\"path\":\"/Friends/1/FirstName\",\"value\":\"Henke\"}]",str);
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

            JsonPatch.CreateJsonPatch(Session.Current, true);

            Console.WriteLine("Flushed");
            Console.WriteLine("=========");
            Console.WriteLine(((Json)jockeJson).DebugString);

            var str = JsonPatch.CreateJsonPatch(Session.Current, true);
            Assert.AreEqual("[]", str);

            jockeJson.Friends[1].FirstName = "Henke";
            jockeJson.Age = 43;
            dynamic kalle = new Json();
            kalle.FirstName = "Kalle";
            jockeJson.Friends.Add(kalle);

            Console.WriteLine("Changed");
            Console.WriteLine("=========");
            Console.WriteLine(((Json)jockeJson).DebugString);

            str = JsonPatch.CreateJsonPatch(Session.Current, true);

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

            JsonPatch.CreateJsonPatch(Session.Current, true);

            j.FirstName = "Timothy";
            j.LastName = "Wester";
            nicke.FirstName = "Nicklas";
            nicke.LastName = "Hammarström";
            j.FirstName = "Charlie";
            j.Friends.Add().FirstName = "Henrik";

            var after = ((Json)j).DebugString;
            var result = JsonPatch.CreateJsonPatch(Session.Current, true);

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

            JsonPatch.CreateJsonPatch(Session.Current, true); // Flush
            var before = ((Json)j).DebugString;

            p.FirstName = "Douglas";

            var after = ((Json)j).DebugString;

            var patch = JsonPatch.CreateJsonPatch(Session.Current, true);

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

            j.FirstName = "Jack";
            nicke.FirstName = "Nicke";
            j.Friends = new List<Json>() { nicke };

            Console.WriteLine("Dirty status");
            Console.WriteLine("============");
            Console.WriteLine(j.DebugString);

            var patch = JsonPatch.CreateJsonPatch(Session.Current, true);

            Console.WriteLine("Changes:");
            Console.WriteLine("========");
            Console.WriteLine(patch);

            Assert.AreEqual(
                "[{\"op\":\"replace\",\"path\":\"/\",\"value\":{\"FirstName\":\"Jack\",\"Friends\":[{\"FirstName\":\"Nicke\"}]}}]", patch);
        }

        [Test]
        public static void TestPatchForTriggers() {
            var schema = new TObject();
            var save = schema.Add<TTrigger>("Save$");

            var json = new Json() { Template = schema };
            json.Session = new Session();

            var patch = JsonPatch.CreateJsonPatch(json.Session, true);
            json.MarkAsReplaced(save);
            patch = JsonPatch.CreateJsonPatch(json.Session, true);

            Console.WriteLine(patch);

            var expected = '[' + string.Format(Helper.PATCH, "/Save$", "null") + ']';
            Assert.AreEqual(expected, patch);
        }

        [Test]
        public static void TestPatchForSubItems() {
            dynamic item1 = new Json();
            dynamic item2 = new Json();
            dynamic subItem1 = new Json();
            dynamic subItem2 = new Json();
            dynamic root = new Json();

            root.Session = new Session();
            root.Header = "Root";
            item1.Text = "1";
            item2.Text = "2";
            root.Items = new List<Json>();
            root.Items.Add(item1);

            subItem1.Text = "S1";
            subItem2.Text = "S2";
            item2.SubItems = new List<Json>();
            
            var patch = JsonPatch.CreateJsonPatch(root.Session, true);
//            Console.WriteLine(patch);

            root.Items[0] = item2;
            Assert.IsNotNull(item2.Parent);

            item2.SubItems.Add(subItem1);
            item2.SubItems.Add(subItem2);

            patch = JsonPatch.CreateJsonPatch(root.Session, true);
            Console.WriteLine(patch);
            Console.WriteLine();

            var expected = '[' + string.Format(Helper.PATCH, "/Items/0",  @"{""Text"":""2"",""SubItems"":[{""Text"":""S1""},{""Text"":""S2""}]}") + ']';
            Assert.AreEqual(expected, patch);
        }

        [Test]
        public static void TestPatchWithDecimal() {
            dynamic root = new Json();
            root.Session = new Session();
            root.Number = 65.0m;

            var oldCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("sv-SE");

            try {
                var patch = JsonPatch.CreateJsonPatch(root.Session, true);
                var expected = '[' + string.Format(Helper.PATCH, "/", @"{""Number"":65.0}") + ']';
                Assert.AreEqual(expected, patch);

                root.Number = 99.5545m;
                patch = JsonPatch.CreateJsonPatch(root.Session, true);
                expected = '[' + string.Format(Helper.PATCH, "/Number", "99.5545") + ']';
                Assert.AreEqual(expected, patch);
            } finally {
                Thread.CurrentThread.CurrentCulture = oldCulture;
            }
        }

        [Test]
        public static void TestPatchWithDouble() {
            dynamic root = new Json();
            root.Session = new Session();
            root.Number = 65.0d;

             var oldCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("sv-SE");

            try {
                var patch = JsonPatch.CreateJsonPatch(root.Session, true);
                var expected = '[' + string.Format(Helper.PATCH, "/", @"{""Number"":65.0}") + ']';
                Assert.AreEqual(expected, patch);

                root.Number = 99.5545d;
                patch = JsonPatch.CreateJsonPatch(root.Session, true);
                expected = '[' + string.Format(Helper.PATCH, "/Number", "99.5545") + ']';
                Assert.AreEqual(expected, patch);
            } finally {
                Thread.CurrentThread.CurrentCulture = oldCulture;
            }
        }
    }
}
