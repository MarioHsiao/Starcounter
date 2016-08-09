using System;
using System.Collections.Generic;
using NUnit.Framework;
using Starcounter.Templates;

namespace Starcounter.Internal.XSON.Tests {

    [TestFixture]
    static class DynamicFunctionTests {
        [Test]
        public static void TestDynamicJson1() {

            dynamic j = new Json();
            dynamic nicke = new Json();
            nicke.FirstName = "Nicke";

            j.FirstName = "Joachim";
            j.Age = 43;
            j.Length = 184.7;
            j.Friends = new List<Json>() { nicke };

            Assert.AreEqual("Joachim", j.FirstName);
            Assert.AreEqual(43, j.Age);
            Assert.AreEqual("Nicke", j.Friends[0].FirstName);
        }

        [Test]
        public static void TestDynamicJson2() {
            dynamic json = new Json();
            json["foo"] = "bar";
            Assert.AreEqual(@"{""foo"":""bar""}", json.ToJson());

            dynamic json2 = new Json();
            json2.foo = "bar";
            Assert.AreEqual(@"{""foo"":""bar""}", json2.ToJson());

            Json json3 = new Json();
            json3["foo"] = "bar";
            Assert.AreEqual(@"{""foo"":""bar""}", json2.ToJson());
        }

        [Test]
        public static void TestDynamicJsonInLoop() {
            for (int i = 0; i < 2; i++) {
                Json jsonItem = new Json();
                jsonItem["dummy"] = "dummy";
            }

            for (int i = 0; i < 2; i++) {
                dynamic jsonItem = new Json();
                jsonItem["dummy"] = "dummy";
            }

            for (int i = 0; i < 2; i++) {
                dynamic jsonItem = new Json();
                jsonItem.dummy = "dummy";
            }

            TObject template = new TObject();
            template.Add<TString>("dummy");
            for (int i = 0; i < 2; i++) {
                dynamic json = new Json() { Template = template };
                json.dummy = "dummy";
            }
        }

        [Test]
        public static void TestDynamicJsonFromString() {
            string jsonStr = @"{""Key"":""Value"", ""Number"":99}";

            dynamic json = new Json(jsonStr);
            Assert.AreEqual("Value", json.Key);
            Assert.AreEqual(99, json.Number);

            jsonStr = @"{""Items"":[{""Key"":""Item1""},{""Key"":""Item2""}]}";
            json = new Json(jsonStr);
            Assert.AreEqual(2, json.Items.Count);
            Assert.AreEqual("Item1", json.Items[0].Key);
            Assert.AreEqual("Item2", json.Items[1].Key);

            // TODO:
            // Disabled this path as currently it does not work to have two different objects in an array.
            // This should be solvable however.

            //jsonStr = @"{""Items"":[{""Key"":""Item1""},{""AnotherKey"":""Item2""}]}";
            //json = new Json(jsonStr);
            //Assert.AreEqual(2, json.Items.Count);
            //Assert.AreEqual("Item1", json.Items[0].Key);
            //Assert.AreEqual("Item2", json.Items[1].AnotherKey);
        }
        
        [Test]
        public static void TestDynamicJsonTemplateProtection() {
            dynamic j = new Json();
            dynamic nicke = new Json();
            dynamic olle = new Json();

            j.FirstName = "Joachim";

            Assert.Throws(typeof(Exception), () => { nicke.Template = j.Template; });
            Assert.DoesNotThrow(() => olle.Template = new TObject());
        }
        
        [Test]
        public static void TestDynamicJsonBinding() {
            var p = new Person();
            p.FirstName = "Joachim";
            p.LastName = "Wester";

            dynamic j = new Json();
            j.Data = p;

            Assert.AreEqual("Joachim", p.FirstName);
            Assert.AreEqual("Joachim", j.FirstName);
        }

        [Test]
        public static void TestDynamicJsonBinding2() {
            var data = new ObjectWithTypes();
            data.Value1 = true;
            data.Value2 = 12;
            data.Value3 = 666L;
            data.Value4 = 23.11d;
            data.Value5 = 19.65m;
            data.Value6 = "Qwerty";
            data.Value7 = ShortEnum.Value1;
            data.Value8 = IntEnum.Value2;
            data.Value9 = new Entity();

            var list = new List<Entity>();
            list.Add(new Entity());
            data.Value10 = list;

            list = new List<Entity>();
            list.Add(new Entity());
            data.Value11 = list;

            var json = new Json();
            json.Data = data;

            Assert.AreEqual(data.Value1, json["Value1"]);
            Assert.AreEqual(data.Value2, json["Value2"]);
            Assert.AreEqual(data.Value3, json["Value3"]);
            Assert.AreEqual(data.Value4, json["Value4"]);
            Assert.AreEqual(data.Value5, json["Value5"]);

            Assert.AreEqual(data.Value6, json["Value6"]);
            Assert.AreEqual(Convert.ToInt64(data.Value7), json["Value7"]);
            Assert.AreEqual(Convert.ToInt64(data.Value8), json["Value8"]);

            // Not supporting objects and arrays currently.
            //var childJson = (Json)json["Value9"];
            //Assert.AreEqual(data.Value9, childJson.Data);

            //childJson = (Json)json["Value10"];
            //Assert.IsTrue(childJson.IsArray);
            //Assert.AreEqual(1, ((IList)childJson).Count);

            //childJson = (Json)((IList)childJson)[0];
            //Assert.AreEqual(data.Value10[0], childJson);

            //childJson = (Json)json["Value11"];
            //Assert.IsTrue(childJson.IsArray);
            //Assert.AreEqual(1, ((IList)childJson).Count);

            //childJson = (Json)((IList)childJson)[0];
            //Assert.AreEqual(data.Value11[0], childJson);
        }
    }
}
