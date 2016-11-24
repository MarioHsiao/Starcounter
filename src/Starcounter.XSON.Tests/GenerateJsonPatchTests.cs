using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using NUnit.Framework;
using Starcounter.XSON;
using Starcounter.Advanced.XSON;
using Starcounter.Templates;
using XSONModule = Starcounter.Internal.XSON.Modules.Starcounter_XSON;

namespace Starcounter.Internal.XSON.Tests {
    [TestFixture]
    public class GenerateJsonPatchTests {
        internal static JsonPatch jsonPatch = new JsonPatch();
        private static string oldAppName = null;

        /// <summary>
        /// Sets up the test.
        /// </summary>
        [TestFixtureSetUp]
        public static void Setup() {
            // Initializing global sessions.
            GlobalSessions.InitGlobalSessions(1);
        }

        [SetUp]
        public static void SetupEachTest() {
            oldAppName = StarcounterEnvironment.AppName;
            StarcounterEnvironment.AppName = "Test";
        }

        [TearDown]
        public static void AfterEachTest() {
            StarcounterEnvironment.AppName = oldAppName;
            Session.Current = null;
        }

        //[Test]
        //public static void TestPatchSizes() {
        //    byte[] patchArr;
        //    Change change;
        //    dynamic json;
        //    int expectedSize;
        //    int patchSize;
        //    TObject schema;
        //    TValue property;
        //    string patch;
        //    string path;
        //    Session session = new Session();
        //    session.Use(() => {
        //        // ["op":"replace","path":"","value":"ApaPapa"]
        //        path = "";
        //        patch = string.Format(Helper.PATCH_REPLACE, path, @"{""FirstName"":""ApaPapa""}");
        //        schema = new TObject();
        //        property = schema.Add<TString>("FirstName");
        //        json = new Json() { Template = schema };
        //        Session.Current.Data = json;
        //        json.FirstName = "ApaPapa";
        //        change = Change.Update(json, null);
        //        patchSize = JsonPatch.EstimateSizeOfPatch(change, false);
        //        Assert.IsTrue(patchSize >= patch.Length); // size is estimated, but needs to be atleast size of patch
        //        json.ChangeLog.Checkpoint();
        //        json.ChangeLog.Add(change);
        //        patchSize = jsonPatch.Generate(json, true, false, out patchArr);
        //        expectedSize = patch.Length + 2;
        //        Assert.AreEqual(expectedSize, patchSize);

        //        // ["op":"replace","path":"/FirstName","value":"ApaPapa"]
        //        path = "/FirstName";
        //        patch = string.Format(Helper.PATCH_REPLACE, path, Helper.Jsonify("ApaPapa"));
        //        schema = new TObject();
        //        property = schema.Add<TString>("FirstName");
        //        json = new Json() { Template = schema };
        //        Session.Current.Data = json;
        //        json.FirstName = "ApaPapa";
        //        change = Change.Update(json, property);
        //        patchSize = JsonPatch.EstimateSizeOfPatch(change, false);
        //        Assert.IsTrue(patchSize >= patch.Length); // size is estimated, but needs to be atleast size of patch
        //        json.ChangeLog.Checkpoint();
        //        json.ChangeLog.Add(change);
        //        patchSize = jsonPatch.Generate(json, true, false, out patchArr);
        //        expectedSize = patch.Length + 2;
        //        Assert.AreEqual(expectedSize, patchSize);

        //        // ["op":"replace","path":"/Focused/Age","value":19]
        //        path = "/Focused/Age";
        //        patch = string.Format(Helper.PATCH_REPLACE, path, 19);
        //        schema = new TObject();
        //        property = schema.Add<TObject>("Focused");
        //        property = ((TObject)property).Add<TLong>("Age");
        //        json = new Json() { Template = schema };
        //        Session.Current.Data = json;
        //        json.Focused.Age = 19;
        //        change = Change.Update(json.Focused, property);
        //        patchSize = JsonPatch.EstimateSizeOfPatch(change, false);
        //        Assert.IsTrue(patchSize >= patch.Length); // size is estimated, but needs to be atleast size of patch
        //        json.ChangeLog.Checkpoint();
        //        json.ChangeLog.Add(change);
        //        patchSize = jsonPatch.Generate(json, true, false, out patchArr);
        //        expectedSize = patch.Length + 2;
        //        Assert.AreEqual(expectedSize, patchSize);

        //        // ["op":"replace","path":"/Items/0/Stats","value":23.5]
        //        path = "/Items/0/Stats";
        //        patch = string.Format(Helper.PATCH_REPLACE, path, 23.5d);
        //        schema = new TObject();
        //        var tarr = schema.Add<TArray<Json>>("Items");
        //        tarr.ElementType = new TObject();
        //        property = ((TObject)tarr.ElementType).Add<TDouble>("Stats");
        //        json = new Json() { Template = schema };
        //        Session.Current.Data = json;
        //        json = json.Items.Add();
        //        json.Stats = 23.5d;
        //        change = Change.Update(json, property);
        //        patchSize = JsonPatch.EstimateSizeOfPatch(change, false);
        //        Assert.IsTrue(patchSize >= patch.Length); // size is estimated, but needs to be atleast size of patch
        //        json.ChangeLog.Checkpoint();
        //        json.ChangeLog.Add(change);
        //        patchSize = jsonPatch.Generate(json, true, false, out patchArr);
        //        expectedSize = patch.Length + 2;
        //        Assert.AreEqual(expectedSize, patchSize);

        //        // ["op":"replace","path":"/OtherApp/FirstName","value":"ApaPapa"]
        //        path = "/Page/OtherApp/FirstName";
        //        patch = string.Format(Helper.PATCH_REPLACE, path, Helper.Jsonify("ApaPapa"));
        //        schema = new TObject();
        //        schema.Add<TLong>("Age");
        //        schema.Add<TObject>("Page");
        //        json = new Json() { Template = schema };
        //        Session.Current.Data = json;
        //        json.Age = 19;
        //        var schema2 = new TObject();
        //        property = schema2.Add<TString>("FirstName");
        //        dynamic json2 = new Json() { Template = schema2 };
        //        json2.FirstName = "ApaPapa";

        //        Json hack = json2;
        //        hack.appName = "OtherApp";

        //        SiblingList stepSiblings = new SiblingList();
        //        stepSiblings.Add(json.Page);
        //        stepSiblings.Add(json2);
        //        Json real = json.Page;
        //        real.wrapInAppName = true;
        //        real.Siblings = stepSiblings;
        //        real = json2;
        //        real.wrapInAppName = true;
        //        real.Siblings = stepSiblings;
        //        change = Change.Update(json2, property);
        //        patchSize = JsonPatch.EstimateSizeOfPatch(change, true);
        //        Assert.IsTrue(patchSize >= patch.Length); // size is estimated, but needs to be atleast size of patch

        //        json.ChangeLog.Checkpoint();
        //        json.ChangeLog.Add(change);
        //        patchSize = jsonPatch.Generate(json, true, true, out patchArr);
        //        expectedSize = patch.Length + 2;
        //        Assert.AreEqual(expectedSize, patchSize);

        //        // ["op":"replace","path":"/Focused/OtherApp/FirstName","value":"ApaPapa"]
        //        path = "/Focused/OtherApp/FirstName";
        //        patch = string.Format(Helper.PATCH_REPLACE, path, Helper.Jsonify("ApaPapa"));
        //        schema = new TObject();
        //        var focSchema = schema.Add<TObject>("Focused");
        //        focSchema.Add<TLong>("Age");
        //        json = new Json() { Template = schema };
        //        Session.Current.Data = json;
        //        json.Focused.Age = 19;
        //        schema2 = new TObject();
        //        property = schema2.Add<TString>("FirstName");
        //        json2 = new Json() { Template = schema2 };
        //        json2.FirstName = "ApaPapa";

        //        hack = json2;
        //        hack.appName = "OtherApp";
        //        stepSiblings = new SiblingList();
        //        stepSiblings.Add(json.Focused);
        //        stepSiblings.Add(json2);
        //        real = json.Focused;
        //        real.wrapInAppName = true;
        //        real.Siblings = stepSiblings;
        //        real = json2;
        //        real.wrapInAppName = true;
        //        real.Siblings = stepSiblings;
        //        change = Change.Update(json2, property);
        //        patchSize = JsonPatch.EstimateSizeOfPatch(change, true);
        //        Assert.IsTrue(patchSize >= patch.Length); // size is estimated, but needs to be atleast size of patch

        //        json.ChangeLog.Checkpoint();
        //        json.ChangeLog.Add(change);
        //        patchSize = jsonPatch.Generate(json, true, true, out patchArr);
        //        expectedSize = patch.Length + 2;
        //        Assert.AreEqual(expectedSize, patchSize);
        //    });
        //}

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

            var session = new Session();
            j.Session = session;

            session.Use(() => {
                j.FirstName = "Douglas";

                var before = JsonDebugHelper.ToFullString((Json)j);
                //            Session.Current.CheckpointChangeLog();
                string str = jsonPatch.Generate(j, true, false);

                j.Daughter = daughter;
                j.FirstName = "Timothy";
                j.LastName = "Wester";
                j.FirstName = "Charlie";

                var after = JsonDebugHelper.ToFullString((Json)j);
                var result = jsonPatch.Generate(j, true, false);

                Write("Before", before);
                Write("After", after);
                Write("Changes", result);

                string facit = "[{\"op\":\"replace\",\"path\":\"/FirstName\",\"value\":\"Charlie\"},{\"op\":\"replace\",\"path\":\"/Daughter\",\"value\":{\"FirstName\":\"Kate\"}},{\"op\":\"replace\",\"path\":\"/LastName\",\"value\":\"Wester\"}]";
                Assert.AreEqual(facit, result);
            });
        }

        protected static void Write(string title, string value) {
            Helper.ConsoleWriteLine("");
            Helper.ConsoleWriteLine(title);
            Helper.ConsoleWriteLine(new String('=', title.Length));
            Helper.ConsoleWriteLine(value);
        }

        [Test]
        public static void TestDirtyFlagsWithoutBinding() {
            dynamic j = new Json();
            dynamic nicke = new Json();
            nicke.FirstName = "Nicke";
            dynamic henrik = new Json();
            henrik.FirstName = "Henrik";

            j.FirstName = "Joachim";
            j.Age = 42;
            j.Length = 184.7;
            j.Friends = new List<Json>() { nicke };

            var session = new Session();
            j.Session = session;

            session.Use(() => {
                j.Friends.Add(henrik);

                Write("New stuff", JsonDebugHelper.ToFullString((Json)j));

                jsonPatch.Generate(j, true, false);

                Helper.ConsoleWriteLine("Flushed");
                Helper.ConsoleWriteLine("=========");
                Helper.ConsoleWriteLine(JsonDebugHelper.ToFullString((Json)j));

                string str = jsonPatch.Generate(j, true, false);
                Assert.AreEqual("[]", str);

                j.Friends[1].FirstName = "Henke";
                j.Age = 43;
                dynamic kalle = new Json();
                kalle.FirstName = "Kalle";
                j.Friends.Add(kalle);

                Helper.ConsoleWriteLine("Changed");
                Helper.ConsoleWriteLine("=========");
                Helper.ConsoleWriteLine(JsonDebugHelper.ToFullString((Json)j));

                str = jsonPatch.Generate(j, true, false);

                Helper.ConsoleWriteLine("JSON-Patch");
                Helper.ConsoleWriteLine("==========");
                Helper.ConsoleWriteLine(str);

                Assert.AreEqual("[{\"op\":\"replace\",\"path\":\"/Age\",\"value\":43},{\"op\":\"add\",\"path\":\"/Friends/2\",\"value\":{\"FirstName\":\"Kalle\"}},{\"op\":\"replace\",\"path\":\"/Friends/1/FirstName\",\"value\":\"Henke\"}]", str);
            });
        }

        //  [Test]
        public static void TestDirtyFlagsWithBinding() {
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

            var session = new Session();
            jockeJson.Session = session;

            session.Use(() => {
                jockeJson.Friends.Add(henrikJson);

                Helper.ConsoleWriteLine("New stuff");
                Helper.ConsoleWriteLine("=========");
                Helper.ConsoleWriteLine(JsonDebugHelper.ToFullString((Json)jockeJson));

                jsonPatch.Generate(jockeJson, true, false);

                Helper.ConsoleWriteLine("Flushed");
                Helper.ConsoleWriteLine("=========");
                Helper.ConsoleWriteLine(JsonDebugHelper.ToFullString((Json)jockeJson));

                string str = jsonPatch.Generate(jockeJson, true, false);
                Assert.AreEqual("[]", str);

                jockeJson.Friends[1].FirstName = "Henke";
                jockeJson.Age = 43;
                dynamic kalle = new Json();
                kalle.FirstName = "Kalle";
                jockeJson.Friends.Add(kalle);

                Helper.ConsoleWriteLine("Changed");
                Helper.ConsoleWriteLine("=========");
                Helper.ConsoleWriteLine(JsonDebugHelper.ToFullString((Json)jockeJson));

                str = jsonPatch.Generate(jockeJson, true, false);

                Helper.ConsoleWriteLine("JSON-Patch");
                Helper.ConsoleWriteLine("==========");
                Helper.ConsoleWriteLine(str);

                Assert.AreEqual("[{\"op\":\"replace\",\"path\":\"/Age\",\"value\":43},\n{\"op\":\"add\",\"path\":\"/Friends\",\"value\":{\"FirstName\":\"Kalle\"}},\n{\"op\":\"replace\",\"path\":\"/Friends/1/FirstName\",\"value\":\"Henke\"}]", str);
            });
        }

        //   [Test]
        public static void TestJsonPatchSimpleMix() {
            dynamic j = new Json();
            dynamic nicke = new Json();
            nicke.FirstName = "Nicke";
            nicke.Age = 43;

            j.FirstName = "Joachim";
            j.Age = 43;
            j.Length = 184.7;
            j.Friends = new List<Json>() { nicke };

            var session = new Session();
            j.Session = session;

            session.Use(() => {
                var before = JsonDebugHelper.ToFullString((Json)j);

                jsonPatch.Generate(j, true, false);

                j.FirstName = "Timothy";
                j.LastName = "Wester";
                nicke.FirstName = "Nicklas";
                nicke.LastName = "Hammarström";
                j.FirstName = "Charlie";
                j.Friends.Add().FirstName = "Henrik";

                var after = JsonDebugHelper.ToFullString((Json)j);
                string result = jsonPatch.Generate(j, true, false);

                Helper.ConsoleWriteLine("Before");
                Helper.ConsoleWriteLine("=====");
                Helper.ConsoleWriteLine(before);
                Helper.ConsoleWriteLine("");
                Helper.ConsoleWriteLine("After");
                Helper.ConsoleWriteLine("=====");
                Helper.ConsoleWriteLine(after);
                Helper.ConsoleWriteLine("");
                Helper.ConsoleWriteLine("Changes");
                Helper.ConsoleWriteLine("=====");
                Helper.ConsoleWriteLine(result);
                Helper.ConsoleWriteLine("");

                string facit =
                    @"[{""op"":""replace"",""path"":""/FirstName"",""value"":""Charlie""},
                     {""op"":""add"",""path"":""/Friends"",""value"":{""FirstName"":""Henrik""}},
                     {""op"":""replace"",""path"":""/FirstName"",""value"":""Timothy""},
                     {""op"":""replace"",""path"":""/LastName"",""value"":""Wester""},
                     {""op"":""replace"",""path"":""/Friends/0/LastName"",""value"":""Hammarström""}}],
                    ";
                Assert.AreEqual(facit, result);
            });
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

            var session = new Session();
            j.Session = session;

            session.Use(() => {
                var start = JsonDebugHelper.ToFullString(json);

                Assert.AreEqual("{}", json.ToJson()); // The data is not bound so the JSON should still be an empty object

                var t = new TObject();
                var fname = t.Add<TString>("FirstName");
                var lname = t.Add<TString>("LastName");
                j.Template = t;
                j.Data = p;

                Assert.IsTrue(!json.HasBeenSent);
                Assert.AreEqual("{\"FirstName\":\"Joachim\",\"LastName\":\"Wester\"}", ((Json)j).ToJson());

                jsonPatch.Generate(j, true, false); // Flush
                var before = JsonDebugHelper.ToFullString((Json)j);

                p.FirstName = "Douglas";

                var after = JsonDebugHelper.ToFullString((Json)j);

                string patch = jsonPatch.Generate(j, true, false);

                Helper.ConsoleWriteLine("Start");
                Helper.ConsoleWriteLine("=====");
                Helper.ConsoleWriteLine(start);
                Helper.ConsoleWriteLine("");
                Helper.ConsoleWriteLine("Before Change");
                Helper.ConsoleWriteLine("=============");
                Helper.ConsoleWriteLine(before);
                Helper.ConsoleWriteLine("");
                Helper.ConsoleWriteLine("After Change");
                Helper.ConsoleWriteLine("============");
                Helper.ConsoleWriteLine(after);
                Helper.ConsoleWriteLine("");
                Helper.ConsoleWriteLine("JSON-Patch");
                Helper.ConsoleWriteLine("==========");
                Helper.ConsoleWriteLine(patch);
                Helper.ConsoleWriteLine("");

                Assert.AreEqual("{\"FirstName\":\"Douglas\",\"LastName\":\"Wester\"}", ((Json)j).ToJson());
                Assert.AreEqual("[{\"op\":\"replace\",\"path\":\"/FirstName\",\"value\":\"Douglas\"}]", patch);
            });
        }


        [Test]
        public static void TestPatchForBrandNewRoot() {
            string debugString;
            dynamic j = new Json();
            dynamic nicke = new Json();

            var session = new Session();
            j.Session = session;

            session.Use(() => {
                Assert.NotNull(Session.Current);

                j.FirstName = "Jack";
                nicke.FirstName = "Nicke";
                j.Friends = new List<Json>() { nicke };

                Helper.ConsoleWriteLine("Dirty status");
                Helper.ConsoleWriteLine("============");
                debugString = JsonDebugHelper.ToFullString(j);
                Helper.ConsoleWriteLine(debugString);

                string patch = jsonPatch.Generate(j, true, false);

                Helper.ConsoleWriteLine("Changes:");
                Helper.ConsoleWriteLine("========");
                Helper.ConsoleWriteLine(patch);

                Assert.AreEqual(
                    "[{\"op\":\"replace\",\"path\":\"\",\"value\":{\"FirstName\":\"Jack\",\"Friends\":[{\"FirstName\":\"Nicke\"}]}}]", patch);
            });
        }

        [Test]
        public static void TestPatchForSubItems() {
            dynamic item1 = new Json();
            dynamic item2 = new Json();
            dynamic subItem1 = new Json();
            dynamic subItem2 = new Json();
            dynamic root = new Json();

            var session = new Session();
            root.Session = session;

            session.Use(() => {
                root.Header = "Root";
                item1.Text = "1";
                item2.Text = "2";
                root.Items = new List<Json>();
                root.Items.Add(item1);

                subItem1.Text = "S1";
                subItem2.Text = "S2";
                item2.SubItems = new List<Json>();

                string patch = jsonPatch.Generate(root, true, false);
                Helper.ConsoleWriteLine(patch);

                root.Items[0] = item2;
                Assert.IsNotNull(item2.Parent);

                item2.SubItems.Add(subItem1);
                item2.SubItems.Add(subItem2);

                patch = jsonPatch.Generate(root, true, false);
                Helper.ConsoleWriteLine(patch);
                Helper.ConsoleWriteLine("");

                var expected = '[' + string.Format(Helper.PATCH_REPLACE, "/Items/0", @"{""Text"":""2"",""SubItems"":[{""Text"":""S1""},{""Text"":""S2""}]}") + ']';
                Assert.AreEqual(expected, patch);
            });
        }

        [Test]
        public static void TestPatchWithDecimal() {
            dynamic root = new Json();
            var session = new Session();
            root.Session = session;
            root.Number = 65.0m;

            session.Use(() => {
                var oldCulture = Thread.CurrentThread.CurrentCulture;
                Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("sv-SE");

                try {
                    var patch = jsonPatch.Generate(root, true, false);
                    var expected = '[' + string.Format(Helper.PATCH_REPLACE, "", @"{""Number"":65.0}") + ']';
                    Assert.AreEqual(expected, patch);

                    root.Number = 99.5545m;
                    patch = jsonPatch.Generate(root, true, false);
                    expected = '[' + string.Format(Helper.PATCH_REPLACE, "/Number", "99.5545") + ']';
                    Assert.AreEqual(expected, patch);
                } finally {
                    Thread.CurrentThread.CurrentCulture = oldCulture;
                }
            });
        }

        [Test]
        public static void TestPatchWithDouble() {
            dynamic root = new Json();
            var session = new Session();
            root.Session = session;
            root.Number = 65.0d;

            session.Use(() => {
                var oldCulture = Thread.CurrentThread.CurrentCulture;
                Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("sv-SE");

                try {
                    // Making sure '.0' is added to values without decimals and exponent.
                    var patch = jsonPatch.Generate(root, true, false);
                    var expected = '[' + string.Format(Helper.PATCH_REPLACE, "", @"{""Number"":65.0}") + ']';
                    Assert.AreEqual(expected, patch);

                    // Only interested in that the patch is generated without buffer overflow
                    root.Number = 454354544454545445453454534534453499.55d;
                    patch = jsonPatch.Generate(root, true, false);
                } finally {
                    Thread.CurrentThread.CurrentCulture = oldCulture;
                }
            });
        }

        [Test]
        public static void TestPatchWithUnnamedProperties() {
            var schema = new TObject();
            var tEmpty = schema.Add<TString>("");
            var tSpace = schema.Add<TString>(" ");

            var json = new Json() { Template = schema };
            var session = new Session() { Data = json };

            session.Use(() => {
                json.Set(tEmpty, "Empty");
                json.Set(tSpace, "Space");

                var patch = jsonPatch.Generate(json, true, false);
                var expected = string.Format(Helper.ONE_PATCH_ARR, "", @"{"""":""Empty"","" "":""Space""}");
                Assert.AreEqual(expected, patch);
            });
        }
    }
}
