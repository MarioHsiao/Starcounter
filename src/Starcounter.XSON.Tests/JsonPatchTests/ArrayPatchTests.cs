
using NUnit.Framework;
using Starcounter.Templates;
using System;
using System.Collections.Generic;

namespace Starcounter.Internal.XSON.Tests {

    [TestFixture]
    static class ArrayPatchTests {


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
            var patch = Session.Current.CreateJsonPatch(true);

            Console.WriteLine("Changes:");
            Console.WriteLine("========");
            Console.WriteLine(patch);

            Assert.AreEqual(
                "[{\"op\":\"add\",\"path\":\"/Friends\",\"value\":{\"FirstName\":\"Nicke\"}}]", patch);



        }





    }
}
