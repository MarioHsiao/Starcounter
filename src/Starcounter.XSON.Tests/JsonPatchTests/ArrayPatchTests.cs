
using NUnit.Framework;
using Starcounter.Templates;
using System;
using System.Collections.Generic;

namespace Starcounter.Internal.XSON.JsonPatch.Tests {

    [TestFixture]
    static class ArrayPatchTests {



        [Test]
        public static void TestJsonPatchSimpleArray() {

            dynamic j = new Json();
            dynamic nicke = new Json();
            nicke.FirstName = "Nicke";

            j.FirstName = "Joachim";
            j.Friends = new List<Json>() { nicke };

            Session.Data = j;
            var before = ((Json)j).ToJson();
            //            Session.Current.CheckpointChangeLog();
            Session.Current.CreateJsonPatch(true);


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

        /// <summary>
        /// Creates a new property in a dynamic JSON object and assigns it to a
        /// new array of JSON objects.
        /// </summary>
     //   [Test]
        public static void CreateANewArrayProperty() {
            dynamic j = new Json();
            dynamic nicke = new Json();

            Session.Data = j;

            j.FirstName = "Jack";
            nicke.FirstName = "Nicke";

            Session.Current.CreateJsonPatch(true); // Flushing

            j.Friends = new List<Json>() { nicke };

            Console.WriteLine("Dirty status");
            Console.WriteLine("============");
            Console.WriteLine(j.DebugString);

            var patch = Session.Current.CreateJsonPatch(true);

            Console.WriteLine("Changes:");
            Console.WriteLine("========");
            Console.WriteLine(patch);

            Assert.AreEqual(
               "[{\"op\":\"replace\",\"path\":\"/Friends\",\"value\":[{\"FirstName\":\"Nicke\"}}]", patch);
        }

        /// <summary>
        /// Replaces an element in an array with a completelly new object. The resulting JSON-patch should
        /// contain a "replace" using a JSON pointer that ends with the index of the element in the array.
        /// </summary>
        [Test]       
		public static void ReplaceAnElementInAnArray() {

            TObject schema = new TObject() { ClassName = "Person" };
            schema.Add<TString>("FirstName");
            schema.Add<TArray<Json>>("Friends",schema);

            TObject friendSchema = new TObject() { ClassName = "Friend" };
            friendSchema.Add<TString>("FirstName");

            dynamic j = new Json() { Template = schema };
            dynamic nicke = new Json() { Template = friendSchema };

            Session.Data = j;
            j.FirstName = "Jack";
            nicke.FirstName = "Nicke";
            j.Friends.Add( nicke );

            Session.Current.CreateJsonPatch(true);

            dynamic henrik = new Json() { Template = friendSchema };
            henrik.FirstName = "Henrik";
            j.Friends[0] = henrik;

            Console.WriteLine("Dirty status");
            Console.WriteLine("============");
            Console.WriteLine(j.DebugString);


            var patch = Session.Current.CreateJsonPatch(true);

            Console.WriteLine("Changes:");
            Console.WriteLine("========");
            Console.WriteLine(patch);

            Assert.AreEqual(
                "[{\"op\":\"replace\",\"path\":\"/Friends/0\",\"value\":{\"FirstName\":\"Henrik\"}}]", patch);
        }

        [Test]
        public static void AssignArrayPropertyToEnumerableOfDataObjects() {
        }

        [Test]
        public static void AssignArrayPropertyToNewArray() {
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

            j.Friends = new List<Json>() { nicke };

            Console.WriteLine("Changes:");
            Console.WriteLine("========");
            Console.WriteLine(Session.Current.CreateJsonPatch(true));

        }
    }
}
