using NUnit.Framework;
using Starcounter.Templates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Internal.XSON.Tests {

    class SerializationTests {

        [Test]
        public static void TestJsonSerialization1() {
            dynamic o1 = new Json();
            o1.SomeString = "NewString!";
            o1.AnotherString = "AnotherString!";
            o1.SomeDecimal = (decimal)1.234567;
            o1.SomeLong = 1234567;

            dynamic o2 = new Json();
            o2.SomeString = "NewString!";
            o2.AnotherString = "AnotherString!";
            o2.SomeDecimal = (decimal)1.234567;
            o2.SomeLong = 1234567;

            List<Json> o3 = new List<Json>();
            o3.Add(o1);
            o3.Add(o1);
            o3.Add(o1);

            dynamic o4 = new Json();
            o4.SomeString = "SomeString!";
            o4.SomeBool = true;
            o4.SomeDecimal = (decimal)1.234567;
            o4.SomeDouble = (double)-1.234567;
            o4.SomeLong = 1234567;
            o4.SomeObject = o2;
            o4.SomeArray = o3;
            o4.SomeString2 = "SomeString2!";

            String serString = o4.ToJson();

            Assert.AreEqual(@"{""SomeString"":""SomeString!"",""SomeBool"":true,""SomeDecimal"":1.234567,""SomeDouble"":-1.234567,""SomeLong"":1234567,""SomeObject"":{""SomeString"":""NewString!"",""AnotherString"":""AnotherString!"",""SomeDecimal"":1.234567,""SomeLong"":1234567},""SomeArray"":[{""SomeString"":""NewString!"",""AnotherString"":""AnotherString!"",""SomeDecimal"":1.234567,""SomeLong"":1234567},{""SomeString"":""NewString!"",""AnotherString"":""AnotherString!"",""SomeDecimal"":1.234567,""SomeLong"":1234567},{""SomeString"":""NewString!"",""AnotherString"":""AnotherString!"",""SomeDecimal"":1.234567,""SomeLong"":1234567}],""SomeString2"":""SomeString2!""}", serString);
        }

        [Test]
        public static void TestJsonSerialization2() {
            dynamic o1 = new Json();
            o1.SomeString = "NewString!";
            o1.AnotherString = "AnotherString!";
            o1.SomeDecimal = (decimal)1.234567;
            o1.SomeLong = 1234567;

            List<Json> o2 = new List<Json>();
            o2.Add(o1);
            o2.Add(o1);
            o2.Add(o1);

            dynamic o3 = new Json();
            o3.SomeArray = o2;

            String serString = o3.ToJson();

            Assert.AreEqual(@"{""SomeArray"":[{""SomeString"":""NewString!"",""AnotherString"":""AnotherString!"",""SomeDecimal"":1.234567,""SomeLong"":1234567},{""SomeString"":""NewString!"",""AnotherString"":""AnotherString!"",""SomeDecimal"":1.234567,""SomeLong"":1234567},{""SomeString"":""NewString!"",""AnotherString"":""AnotherString!"",""SomeDecimal"":1.234567,""SomeLong"":1234567}]}", serString);
        }
    }
}
