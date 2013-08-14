
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


        }


    }
}
