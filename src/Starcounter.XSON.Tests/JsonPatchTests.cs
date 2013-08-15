
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
            j.Friends = new List<Obj>() { nicke };

            Session.Data = j;

            Session.Data.LogChanges = true;
            nicke.LogChanges = true;
            ChangeLog.CurrentOnThread = new ChangeLog();

            j.FirstName = "Timothy";
            j.LastName = "Wester";
            nicke.LastName = "Hammarström";
            j.FirstName = "Charlie";
            j.Friends.Add().FirstName = "Henrik";

            var cl = ChangeLog.CurrentOnThread;

            string facit = @"[{""op"":""replace"",""path"":""/FirstName"",""value"":""Charlie""},
{""op"":""replace"",""path"":""/LastName"",""value"":""Wester""},
{""op"":""replace"",""path"":""/Friends/0/LastName"",""value"":""Hammarström""},
{""op"":""replace"",""path"":""/FirstName"",""value"":""Charlie""},
{""op"":""add"",""path"":""/Friends/1"",""value"":{""FirstName"":""Henrik""}}]";
            Assert.AreEqual(facit,cl.ToJsonPatch());

        }



        [Test]
        public static void TestArrayPatches() {
            dynamic j = new Json();
            dynamic nicke = new Json();


            Session.Data = j;
            //Session.Data.LogChanges = true;
            var cl = ChangeLog.CurrentOnThread = new ChangeLog();

            j.FirstName = "Jack";
            nicke.FirstName = "Nicke";
            ((Json)j).LogChanges = true;
            j.Friends = new List<Obj>() { nicke };

            Console.WriteLine("Changes:");
            Console.WriteLine("========");
            Console.WriteLine(cl.ToJsonPatch());

        }


    }
}
