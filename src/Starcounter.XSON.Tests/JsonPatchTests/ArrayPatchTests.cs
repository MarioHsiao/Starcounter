
using NUnit.Framework;
using Starcounter.Templates;
using System;
using System.Collections.Generic;

namespace Starcounter.Internal.XSON.JsonPatch.Tests {

    [TestFixture]
    static class ArrayPatchTests {

        /// <summary>
        /// Creates a new property in a dynamic JSON object and assigns it to a
        /// new array of JSON objects.
        /// </summary>
        [Test]
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

            dynamic j = new Json();
            dynamic nicke = new Json();

            Session.Data = j;
            j.FirstName = "Jack";
            nicke.FirstName = "Nicke";
            j.Friends = new List<Json>() { nicke };

            Console.WriteLine("Dirty status");
            Console.WriteLine("============");
            Console.WriteLine(j.DebugString);

            Session.Current.CreateJsonPatch(true);
            dynamic henrik = new Json();
            henrik.FirstName = "Henrik";
            j.Friends[0] = henrik;

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
    }
}
