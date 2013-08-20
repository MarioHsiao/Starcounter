
using NUnit.Framework;
using Starcounter.Templates;
using System;
using System.Collections.Generic;

namespace Starcounter.Internal.XSON.Tests {

    [TestFixture]
    static class JsonPatchTests {


        [Test]
        public static void TestSimpleJsonPatch() {

            dynamic j = new Json();
            dynamic nicke = new Json();
            nicke.FirstName = "Nicke";

            j.FirstName = "Joachim";
            j.Age = 43;
            j.Length = 184.7;

            Session.Data = j;

            j.FirstName = "Douglas";

            var before = ((Json)j).ToJson();
            Session.Current.CheckpointChangeLog();

            j.FirstName = "Timothy";
            j.LastName = "Wester";
            j.FirstName = "Charlie";

            var after = ((Json)j).ToJson();
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

            string facit = "[{\"op\":\"replace\",\"path\":\"/FirstName\",\"value\":\"Charlie\"},\n{\"op\":\"replace\",\"path\":\"/LastName\",\"value\":\"Wester\"}]";
            Assert.AreEqual(facit, result);

        }


        [Test]
        public static void TestJsonPatchSimpleArray() {

            dynamic j = new Json();
            dynamic nicke = new Json();
            nicke.FirstName = "Nicke";

            j.FirstName = "Joachim";
            j.Friends = new List<Obj>() { nicke };

            Session.Data = j;
            var before = ((Json)j).ToJson();
            Session.Current.CheckpointChangeLog();


            var x = j.Friends.Add();
            x.FirstName = "Henrik";
            x.LastName = "Boman";

            var after = ((Json)j).ToJson();
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

            string facit = @"[{""op"":""replace"",""path"":""/Friends/1"",""value"":{""FirstName"":""Henrik"",""LastName"":""Boman""}}]";
            Assert.AreEqual(facit, result);

        }

        [Test]
        public static void TestDirtyFlagsWithoutBinding() {

            TObj.UseCodegeneratedSerializer = false;

            dynamic j = new Json();
            dynamic nicke = new Json();
            nicke.FirstName = "Nicke";
            dynamic henrik = new Json();
            henrik.FirstName = "Henrik";

            j.FirstName = "Joachim";
            j.Age = 42;
            j.Length = 184.7;
            j.Friends = new List<Obj>() { nicke };

            Session.Data = j;

            j.Friends.Add(henrik);

            Console.WriteLine("New stuff");
            Console.WriteLine("=========");
            Console.WriteLine(((Json)j).DebugString);

            Session.Current.CheckpointChangeLog();            
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

            TObj.UseCodegeneratedSerializer = false;

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
            jockeJson.Friends = new List<Obj>() { nickeJson };

            Session.Data = jockeJson;

            jockeJson.Friends.Add(henrikJson);

            Console.WriteLine("New stuff");
            Console.WriteLine("=========");
            Console.WriteLine(((Json)jockeJson).DebugString);

            Session.Current.CheckpointChangeLog();
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

            TObj.UseCodegeneratedSerializer = false;

            dynamic j = new Json();
            dynamic nicke = new Json();
            nicke.FirstName = "Nicke";
            nicke.Age = 43;

            j.FirstName = "Joachim";
            j.Age = 43;
            j.Length = 184.7;
            j.Friends = new List<Obj>() { nicke };

            Session.Data = j;

            var before = ((Json)j).DebugString;
            Session.Current.CheckpointChangeLog();


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

            Session.Data = j;
            var before = ((Json)j).DebugString;

            Assert.AreEqual("{}", ((Json)j).ToJson()); // The data is not bound so the JSON should still be an empty object

            TJson t = new TJson();
            var prop = t.Add<TString>("FirstName"); // TODO! By default, properties are automatically bound my matching property names
            prop.Bind = "FirstName";
            j.Template = t;
            j.Data = p;

            Assert.AreEqual("{\"FirstName\":\"Joachim\"}", ((Json)j).ToJson());

            p.FirstName = "Douglas";

            var after = ((Json)j).DebugString;
            Assert.AreEqual("{\"FirstName\":\"Douglas\"}", ((Json)j).ToJson());

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
        }


        [Test]
        public static void TestArrayPatches() {
            dynamic j = new Json();
            dynamic nicke = new Json();


            Session.Data = j;

            Assert.NotNull(Session.Current);

            //Session.Data.LogChanges = true;
            //var cl = ChangeLog.CurrentOnThread = new ChangeLog();

            j.FirstName = "Jack";
            nicke.FirstName = "Nicke";
            //((Json)j).LogChanges = true;

           // Session.Current.LogChanges = true;

            j.Friends = new List<Obj>() { nicke };

            Console.WriteLine("Changes:");
            Console.WriteLine("========");
            Console.WriteLine( Session.Current.CreateJsonPatch(true));

        }


    }
}
