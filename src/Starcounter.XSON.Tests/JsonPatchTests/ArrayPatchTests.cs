
using NUnit.Framework;
using Starcounter.Internal.XSON.Tests;
using Starcounter.Templates;
using System;
using System.Collections.Generic;
using Starcounter.XSON;
using System.Collections;

namespace Starcounter.Internal.XSON.Tests {

    [TestFixture]
    public class ArrayPatchTests : GenerateJsonPatchTests {
        [Test]
        public static void TestJsonPatchSimpleArray() {
			dynamic j = new Json();
			dynamic nicke = new Json();
			nicke.FirstName = "Nicke";

			j.FirstName = "Joachim";
			j.Friends = new List<Json>() { nicke };

            Session.Current = new Session() { Data = j };

			var before = ((Json)j).DebugString;
			jsonPatch.Generate(j, true, false);

			var x = j.Friends.Add();
			x.FirstName = "Henrik";
			x.LastName = "Boman";

			var after = ((Json)j).DebugString;
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

			string facit = @"[{""op"":""add"",""path"":""/Friends/1"",""value"":{""FirstName"":""Henrik"",""LastName"":""Boman""}}]";
			Assert.AreEqual(facit, result);
        }

        /// <summary>
        /// Creates a new property in a dynamic JSON object and assigns it to a
        /// new array of JSON objects.
        /// </summary>
        [Test]
        public static void CreateANewArrayProperty() {
            dynamic j = new Json();
            dynamic nicke = new Json();
            string debugString;
            Session.Current = new Session() { Data = j };

            j.FirstName = "Jack";
            nicke.FirstName = "Nicke";

            jsonPatch.Generate(j, true, false); // Flushing

            j.Friends = new List<Json>() { nicke };

            Helper.ConsoleWriteLine("Dirty status");
            Helper.ConsoleWriteLine("============");
            debugString = j.DebugString;
            Helper.ConsoleWriteLine(debugString);

            string patch = jsonPatch.Generate(j, true, false);

            Helper.ConsoleWriteLine("Changes:");
            Helper.ConsoleWriteLine("========");
            Helper.ConsoleWriteLine(patch);

            Assert.AreEqual(
               "[{\"op\":\"replace\",\"path\":\"/Friends\",\"value\":[{\"FirstName\":\"Nicke\"}]}]", patch);
        }

        /// <summary>
        /// Replaces an element in an array with a completelly new object. The resulting JSON-patch should
        /// contain a "replace" using a JSON pointer that ends with the index of the element in the array.
        /// </summary>
        [Test]       
		public static void ReplaceAnElementInAnArray() {
            string debugString;
            TObject schema = new TObject() { ClassName = "Person" };
            TObject friendSchema = new TObject() { ClassName = "Friend" };

            schema.Add<TString>("FirstName");
            schema.Add<TArray<Json>>("Friends",friendSchema);

            friendSchema.Add<TString>("FirstName");

            dynamic j = new Json() { Template = schema };
            dynamic nicke = new Json() { Template = friendSchema };

            Session.Current = new Session() { Data = j };

			j.FirstName = "Jack";
			nicke.FirstName = "Nicke";
			j.Friends.Add(nicke);
			j["FirstName"] = "Jack";
			nicke["FirstName"] = "Nicke";
			(j["Friends"] as Arr<Json>).Add( nicke );

            string str = jsonPatch.Generate(j, true, false);

            dynamic henrik = new Json() { Template = friendSchema };

			//henrik.FirstName = "Henrik";
			//j.Friends
            henrik["FirstName"] = "Henrik";
            (j["Friends"] as Json)[0] = henrik;

            Helper.ConsoleWriteLine("Dirty status");
            Helper.ConsoleWriteLine("============");
            debugString = j.DebugString;
            Helper.ConsoleWriteLine(debugString);

            string patch = jsonPatch.Generate(j, true, false);

            Helper.ConsoleWriteLine("Changes:");
            Helper.ConsoleWriteLine("========");
            Helper.ConsoleWriteLine(patch);

			Assert.AreEqual(
				"[{\"op\":\"replace\",\"path\":\"/Friends/0\",\"value\":{\"FirstName\":\"Henrik\"}}]", patch);
        }

        [Test]
        public static void AssignToEnumerable() {
            dynamic company = new Json();
            company.Name = "Starcounter";
            var person = new Person();
            var person2 = new Person();
            company.Contacts = new object[] { person, person2 };
            person.FirstName = "Timothy";
            person2.FirstName = "Douglas";

            Write("Status 1",company.DebugString);

            Session.Current = new Session() { Data = company };
            Write("JSON-Patch 1", jsonPatch.Generate(company, true, false));
			
            Write("Before status",company.DebugString);

            var charlie = new Person();
            charlie.FirstName = "Charlie";
            company.Contacts = new object[] { charlie };

            Write("After status 2", company.DebugString);
            Write("JSON-Patch 2", jsonPatch.Generate(company, true, false));

            company.Contacts = new object[] { person, person2 };

            Write("After status 3",company.DebugString);
            Write("JSON-Patch 3", jsonPatch.Generate(company, true, false));

            Write("After status 4 (no changes)", company.DebugString);
            Write("JSON-Patch 4 (empty)", jsonPatch.Generate(company, true, false));
        }

        [Test]
        public static void AssignArrayPropertyToNewArray() {
            string debugString;
            dynamic company = new Json();
            company.Name = "Starcounter";
 
            Session.Current = new Session() { Data = company };
            string patch = jsonPatch.Generate(company, true, false);

            Helper.ConsoleWriteLine(patch);
            Helper.ConsoleWriteLine("");

            dynamic person = new Json();
            dynamic person2 = new Json();
            person.FirstName = "Timothy";
            person2.FirstName = "Douglas";

            company.Contacts = new object[] { person, person2 };
            company.Success = true;


            Helper.ConsoleWriteLine("After status");
            Helper.ConsoleWriteLine("============");
            debugString = company.DebugString;
            Helper.ConsoleWriteLine(debugString);

            Helper.ConsoleWriteLine("JSON-Patch");
            Helper.ConsoleWriteLine("==========");

            patch = jsonPatch.Generate(company, true, false);
            Helper.ConsoleWriteLine(patch);
        }

        [Test]
        public static void TestArrayPatches() {
            dynamic j = new Json();
            dynamic nicke = new Json();

            Session.Current = new Session() { Data = j };
            Assert.NotNull(Session.Current);

            j.FirstName = "Jack";
            nicke.FirstName = "Nicke";
            j.Friends = new List<Json>() { nicke };

            Helper.ConsoleWriteLine("Changes:");
            Helper.ConsoleWriteLine("========");

            string patch = jsonPatch.Generate(j, true, false);
            Helper.ConsoleWriteLine(patch);
        }

        [Test]
        public static void TestAddItems() {
            dynamic j = new Json();
            dynamic nicke = new Json();

            Session.Current = new Session() { Data = j };
            Assert.NotNull(Session.Current);

            j.FirstName = "Jack";
            nicke.FirstName = "Nicke";
            j.Friends = new List<Json>() { nicke };

            jsonPatch.Generate(j, true, false);
            var p = j.Friends.Add();
            p.FirstName = "Marten";
            var p2 = j.Friends.Add();
            p2.FirstName = "Asa";

            Helper.ConsoleWriteLine("Changes:");
            Helper.ConsoleWriteLine("========");

            string patch = jsonPatch.Generate(j, true, false);
            Helper.ConsoleWriteLine(patch);
        }

        [Test]
        public static void TestReplaceItem() {
            dynamic root = new Json();
            dynamic item;
            string patch;

            Session.Current = new Session() { Data = root };
            Assert.NotNull(Session.Current);

            root.FirstName = "Jack";
            root.Items = new List<Json>();

            item = new Json();
            item.Number = 1;
            root.Items.Add(item);

            item = new Json();
            item.Number = 2;
            root.Items.Add(item);

            item = new Json();
            item.Number = 3;
            root.Items.Add(item);

            patch = jsonPatch.Generate(root, true, false);

            Helper.ConsoleWriteLine("BEFORE:");
            Helper.ConsoleWriteLine("-----------");
            Helper.ConsoleWriteLine(patch);

            item = new Json();
            item.Number = 99;
            root.Items[1] = item;

            patch = jsonPatch.Generate(root, true, false);

            Helper.ConsoleWriteLine("AFTER");
            Helper.ConsoleWriteLine("----------");
            Helper.ConsoleWriteLine(patch);

            string correctPatch = @"[{""op"":""replace"",""path"":""/Items/1"",""value"":{""Number"":99}}]";
            Assert.AreEqual(correctPatch, patch);
        }

        [Test]
        public static void TestInsertItem() {
            dynamic root = new Json();
            dynamic item;
            string patch;

            Session.Current = new Session() { Data = root };
            Assert.NotNull(Session.Current);

            root.FirstName = "Jack";
            root.Items = new List<Json>();

            item = new Json();
            item.Number = 1;
            root.Items.Add(item);

            item = new Json();
            item.Number = 2;
            root.Items.Add(item);

            item = new Json();
            item.Number = 3;
            root.Items.Add(item);

            patch = jsonPatch.Generate(root, true, false);

            Helper.ConsoleWriteLine("BEFORE:");
            Helper.ConsoleWriteLine("-----------");
            Helper.ConsoleWriteLine(patch);

            item = new Json();
            item.Number = 99;
            root.Items.Insert(1, item);

            patch = jsonPatch.Generate(root, true, false);

            Helper.ConsoleWriteLine("AFTER");
            Helper.ConsoleWriteLine("----------");
            Helper.ConsoleWriteLine(patch);

            string correctPatch = @"[{""op"":""add"",""path"":""/Items/1"",""value"":{""Number"":99}}]";
            Assert.AreEqual(correctPatch, patch);
        }

        [Test]
        public static void TestRemoveItem() {
            dynamic root = new Json();
            dynamic item;
            string patch;

            Session.Current = new Session() { Data = root };
            Assert.NotNull(Session.Current);

            root.FirstName = "Jack";
            root.Items = new List<Json>();

            item = new Json();
            item.Number = 1;
            root.Items.Add(item);

            item = new Json();
            item.Number = 2;
            root.Items.Add(item);

            item = new Json();
            item.Number = 3;
            root.Items.Add(item);

            patch = jsonPatch.Generate(root, true, false);

            Helper.ConsoleWriteLine("BEFORE:");
            Helper.ConsoleWriteLine("-----------");
            Helper.ConsoleWriteLine(patch);

            root.Items.RemoveAt(1);
            patch = jsonPatch.Generate(root, true, false);

            Helper.ConsoleWriteLine("AFTER");
            Helper.ConsoleWriteLine("----------");
            Helper.ConsoleWriteLine(patch);

            string correctPatch = @"[{""op"":""remove"",""path"":""/Items/1""}]";
            Assert.AreEqual(correctPatch, patch);
        }

        [Test]
        public static void TestChangeAfterInsertAndRemoveItem() {
            dynamic root = new Json();
            dynamic item1;
            dynamic item2;
            string correctPatch;
            string patch;

            Session.Current = new Session() { Data = root };
            Assert.NotNull(Session.Current);

            root.FirstName = "Jack";
            root.Items = new List<Json>();

            item1 = new Json();
            item1.Number = 1;
            root.Items.Add(item1);

            item2 = new Json();
            item2.Number = 99;
            root.Items.Insert(0, item2);

            // Clearing existing changes.
            patch = jsonPatch.Generate(root, true, false);
            
            item1.Number = 666;
            patch = jsonPatch.Generate(root, true, false);
            correctPatch = @"[{""op"":""replace"",""path"":""/Items/1/Number"",""value"":666}]";
            Assert.AreEqual(correctPatch, patch);

            root.Items.RemoveAt(0); // item2

            // Clearing existing changes.
            patch = jsonPatch.Generate(root, true, false);

            item1.Number = 19;
            patch = jsonPatch.Generate(root, true, false);
            correctPatch = @"[{""op"":""replace"",""path"":""/Items/0/Number"",""value"":19}]";
            Assert.AreEqual(correctPatch, patch);
        }

        [Test]
        public static void TestSeveralModifications() {
            dynamic root = new Json();
            dynamic item1;
            dynamic item2;
            dynamic item3;
            string correctPatch;
            string patch;

            Session.Current = new Session() { Data = root };
            Assert.NotNull(Session.Current);

            root.FirstName = "Jack";
            root.Items = new List<Json>();

            // Clearing existing changes.
            patch = jsonPatch.Generate(root, true, false);

            item1 = new Json();
            item1.Number = 1;
            root.Items.Add(item1);

            item2 = new Json();
            item2.Number = 2;
            root.Items[0] = item2;

            root.Items.Remove(item2); // index 0

            item3 = new Json();
            item3.Number = 3;
            root.Items.Insert(0, item3);

            patch = jsonPatch.Generate(root, true, false);

            correctPatch = "["
                + string.Format(Helper.PATCH_ADD, "/Items/0", @"{""Number"":1}")
                + ","
                + string.Format(Helper.PATCH_REPLACE, "/Items/0", @"{""Number"":2}")
                + ","
                + string.Format(Helper.PATCH_REMOVE, "/Items/0")
                + ","
                + string.Format(Helper.PATCH_ADD, "/Items/0", @"{""Number"":3}")
                + "]";

            Assert.AreEqual(correctPatch, patch);
        }

        [Test]
        public static void TestArrayChangesWithBoundData() {
            var tSchema = new TObject() { ClassName = "ObjWithArr" };
            var tItems = tSchema.Add<TObjArr>("Items");
            tItems.BindingStrategy = BindingStrategy.Auto;
            tItems.ElementType = new TLong() { BindingStrategy = BindingStrategy.Auto };
            dynamic json = new Json() { Template = tSchema };

            var session = new Session();
            
            session.StartUsing();
            try {
                session.Data = json;

                var newArr = new long[] { 1, 2, 3};
                json.Items.Data = newArr;
                AssertArray(json.Items, newArr);

                var patch = jsonPatch.Generate(json, true, false);
                
                newArr = new long[] { 1, 4, 2, 3 };
                json.Items.CheckBoundArray(newArr);
                AssertArray(json.Items, newArr);

                patch = jsonPatch.Generate(json, true, false);
                var expectedPatch = @"[{""op"":""add"",""path"":""/Items/1"",""value"":4}]";
                Assert.AreEqual(expectedPatch, patch);
                
                json.Items.RemoveAt(1); // Reset to inital state, [1, 2, 3]
                patch = jsonPatch.Generate(json, true, false);

                newArr = new long[] { 1, 3 };
                json.Items.CheckBoundArray(newArr);
                AssertArray(json.Items, newArr);

                patch = jsonPatch.Generate(json, true, false);
                expectedPatch = @"[{""op"":""remove"",""path"":""/Items/1""}]";
                Assert.AreEqual(expectedPatch, patch);
                
                newArr = new long[] { 1, 2, 3, 4, 5};
                json.Items.CheckBoundArray(newArr); // Reset to inital state, [1, 2, 3, 4, 5]
                AssertArray(json.Items, newArr);

                patch = jsonPatch.Generate(json, true, false);
                
                newArr = new long[] { 1, 3, 4, 5 };
                json.Items.CheckBoundArray(newArr);
                AssertArray(json.Items, newArr);

                patch = jsonPatch.Generate(json, true, false);
                expectedPatch = @"[{""op"":""remove"",""path"":""/Items/1""}]";
                Assert.AreEqual(expectedPatch, patch);
                
                newArr = new long[] { 1, 2, 3, 4, 5 };
                json.Items.CheckBoundArray(newArr); // Reset to inital state, [1, 2, 3, 4, 5]
                AssertArray(json.Items, newArr);
                patch = jsonPatch.Generate(json, true, false);

                expectedPatch = @"[{""op"":""add"",""path"":""/Items/1"",""value"":2}]";
                Assert.AreEqual(expectedPatch, patch);

                newArr = new long[] { 1, 5, 4, 3 };
                json.Items.CheckBoundArray(newArr);
                AssertArray(json.Items, newArr);
                patch = jsonPatch.Generate(json, true, false);

                expectedPatch = @"[{""op"":""remove"",""path"":""/Items/1""},{""op"":""remove"",""path"":""/Items/3""},{""op"":""add"",""path"":""/Items/1"",""value"":5},{""op"":""remove"",""path"":""/Items/3""},{""op"":""add"",""path"":""/Items/2"",""value"":4}]";
                Assert.AreEqual(expectedPatch, patch);

            } finally {
                session.StopUsing();
            }
        }

        private static void AssertArray(IList actual, long[] expected) {
            Assert.AreEqual(expected.Length, actual.Count);
            for (int i = 0; i < expected.Length; i++) {
                Assert.AreEqual(expected[i], (long)(((Json)actual[i]).Data));
            }
        }

        [Test]
        public static void BenchmarkArrayChangesWithBoundData() {
            var tSchema = new TObject() { ClassName = "ObjWithArr" };
            var tItems = tSchema.Add<TObjArr>("Items");
            tItems.BindingStrategy = BindingStrategy.Auto;
            tItems.ElementType = new TLong() { BindingStrategy = BindingStrategy.Auto };
            dynamic json = new Json() { Template = tSchema };

            var session = new Session();
            session.StartUsing();
            try {
                session.Data = json;

                int testCount;
                double oldTime;
                int oldChangeCount;
                double newTime;
                int newChangeCount;
                Json items = (Json)json.Items;
                Action<IEnumerable> oldCheck = items.CheckBoundArray_OLD;
                Action<IEnumerable> newCheck = items.CheckBoundArray;

                List<long> bound;

                testCount = 5000;

                // 1 item removed in beginning.
                bound = CreateArray(testCount);
                bound.RemoveAt(2);

                // Testing using old code.
                ResetJsonArray(json.Items, testCount);
                oldTime = ExecuteBenchmark(oldCheck, bound);
                oldChangeCount = json.Items.ArrayAddsAndDeletes.Count;
                
                // Testing using new code.
                ResetJsonArray(json.Items, testCount);
                newTime = ExecuteBenchmark(newCheck, bound);
                newChangeCount = json.Items.ArrayAddsAndDeletes.Count;

                Helper.ConsoleWriteLine("Initial array: " + testCount + " items, bound array 1 item removed in beginning.");
                Helper.ConsoleWriteLine("OLD: " + oldTime + " ms (" + oldChangeCount + " changes).");
                Helper.ConsoleWriteLine("NEW: " + newTime + " ms (" + newChangeCount + " changes).");


                testCount = 50000;

                // 1 item removed in the end.
                bound = CreateArray(testCount);
                bound.RemoveAt(testCount - 5);

                // Testing using old code.
                ResetJsonArray(json.Items, testCount);
                oldTime = ExecuteBenchmark(oldCheck, bound);
                oldChangeCount = json.Items.ArrayAddsAndDeletes.Count;

                // Testing using new code.
                ResetJsonArray(json.Items, testCount);
                newTime = ExecuteBenchmark(newCheck, bound);
                newChangeCount = json.Items.ArrayAddsAndDeletes.Count;

                Helper.ConsoleWriteLine("Initial array: " + testCount + " items, bound array 1 item removed in the end.");
                Helper.ConsoleWriteLine("OLD: " + oldTime + " ms (" + oldChangeCount + " changes).");
                Helper.ConsoleWriteLine("NEW: " + newTime + " ms (" + newChangeCount + " changes).");

                testCount = 1000;

                // array reversed
                bound = CreateArray(testCount);
                bound.Reverse();

                // Testing using old code.
                ResetJsonArray(json.Items, testCount);
                oldTime = ExecuteBenchmark(oldCheck, bound);
                oldChangeCount = json.Items.ArrayAddsAndDeletes.Count;

                // Testing using new code.
                ResetJsonArray(json.Items, testCount);
                newTime = ExecuteBenchmark(newCheck, bound);
                newChangeCount = json.Items.ArrayAddsAndDeletes.Count;

                Helper.ConsoleWriteLine("Initial array: " + testCount + " items, bound array reversed.");
                Helper.ConsoleWriteLine("OLD: " + oldTime + " ms (" + oldChangeCount + " changes).");
                Helper.ConsoleWriteLine("NEW: " + newTime + " ms (" + newChangeCount + " changes).");

            } finally {
                session.StopUsing();
            }
        }

        private static void ResetJsonArray(Json items, int nrOfItems) {
            ((IList)items).Clear();
            items.Data = CreateArray(nrOfItems);
            items.ChangeLog.Clear();
            items.ArrayAddsAndDeletes.Clear();
        }

        private static double ExecuteBenchmark(Action<IEnumerable> action, IEnumerable bound) {
            DateTime start = DateTime.Now;
            action(bound);
            DateTime stop = DateTime.Now;

            return (stop - start).TotalMilliseconds;
        }

        private static List<long> CreateArray(int count) {
            List<long> list = new List<long>();

            for (int i = 1; i < count + 1; i++) {
                list.Add(i);
            }
            return list;
        }
    }
}
