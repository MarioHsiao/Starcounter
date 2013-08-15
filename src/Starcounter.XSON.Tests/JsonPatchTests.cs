
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

            foreach (var c in ChangeLog.CurrentOnThread) {
                Console.WriteLine(String.Format("Change:{0} on {1}",c.ChangeType,c.Template.PropertyNameWithPath));
            }


        }


    }
}
