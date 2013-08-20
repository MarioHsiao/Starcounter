
using NUnit.Framework;
using Starcounter.Templates;
using System;
using System.Collections.Generic;

namespace Starcounter.Internal.XSON.JsonPatch.Tests {

    [TestFixture]
    static class ArrayPatchTests {


        [Test]
        public static void TestPatchForBrandNewRoot() {
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

            Console.WriteLine("Dirty status");
            Console.WriteLine("============");
            Console.WriteLine(j.DebugString);


            var patch = Session.Current.CreateJsonPatch(true);

            Console.WriteLine("Changes:");
            Console.WriteLine("========");
            Console.WriteLine(patch);

            Assert.AreEqual(
                "[{\"op\":\"add\",\"path\":\"/\",\"value\":{\"FirstName\":\"Jack\",\"Friends\":[{\"FirstName\":\"Nicke\"}]}}]", patch);



        }




        [Test]
        public static void TestCreateNewArray() {
            dynamic j = new Json();
            dynamic nicke = new Json();

            Session.Data = j;

            j.FirstName = "Jack";
            nicke.FirstName = "Nicke";

            Session.Current.CreateJsonPatch(true); // Flushing

            j.Friends = new List<Obj>() { nicke };

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



 //     [Test]       
		public static void TestPatchForChangingAnElement() {

            dynamic j = new Json();
            dynamic nicke = new Json();

            Session.Data = j;
            j.FirstName = "Jack";
            nicke.FirstName = "Nicke";
            j.Friends = new List<Obj>() { nicke };

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
    }
}
