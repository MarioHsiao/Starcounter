using System;
using System.Runtime.InteropServices;
using System.Text;
using Starcounter.Internal.MsBuild;
using NUnit.Framework;
using Starcounter.Internal;
using Starcounter.Internal.Application;
using Starcounter.Internal.Application.JsonReader;
using Starcounter.Internal.ExeModule;
using Starcounter.Templates;

namespace Starcounter.Internal.JsonPatch.Test
{
    internal struct AppAndTemplate
    {
        internal readonly dynamic App;
        internal readonly dynamic Template;

        internal AppAndTemplate(dynamic app, dynamic template)
        {
            App = app;
            Template = template;
        }
    }

    [TestFixture]
    public class JsonPatchTest
    {
        [Test]
        public static void TestJsonPointer()
        {
            String strPtr = "";
            JsonPointer jsonPtr = new JsonPointer(strPtr);

            // TODO:
            // Empty string should contain one token, pointing
            // to the whole document.

            //Assert.IsTrue(jsonPtr.MoveNext());
            //Assert.AreEqual("", jsonPtr.Current);
            //PrintPointer(jsonPtr, strPtr);

            strPtr = "/master/login";
            jsonPtr = new JsonPointer(strPtr);
            Assert.IsTrue(jsonPtr.MoveNext());
            Assert.AreEqual("master", jsonPtr.Current);
            Assert.IsTrue(jsonPtr.MoveNext());
            Assert.AreEqual("login", jsonPtr.Current);
            Assert.IsFalse(jsonPtr.MoveNext());
            PrintPointer(jsonPtr, strPtr);

            strPtr = "/foo/2";
            jsonPtr = new JsonPointer(strPtr);
            Assert.IsTrue(jsonPtr.MoveNext());
            Assert.AreEqual("foo", jsonPtr.Current);
            Assert.IsTrue(jsonPtr.MoveNext());
            Assert.AreEqual(2, jsonPtr.CurrentAsInt);
            Assert.IsFalse(jsonPtr.MoveNext());
            PrintPointer(jsonPtr, strPtr);

            strPtr = "/a~1b/b~0r";
            jsonPtr = new JsonPointer(strPtr);
            Assert.IsTrue(jsonPtr.MoveNext());
            Assert.AreEqual("a/b", jsonPtr.Current);
            Assert.IsTrue(jsonPtr.MoveNext());
            Assert.AreEqual("b~r", jsonPtr.Current);
            Assert.IsFalse(jsonPtr.MoveNext());
            PrintPointer(jsonPtr, strPtr);

            strPtr = "/foo/b\'r";
            jsonPtr = new JsonPointer(strPtr);
            Assert.IsTrue(jsonPtr.MoveNext());
            Assert.AreEqual("foo", jsonPtr.Current);
            Assert.IsTrue(jsonPtr.MoveNext());
            Assert.AreEqual("b'r", jsonPtr.Current);
            Assert.IsFalse(jsonPtr.MoveNext());
            PrintPointer(jsonPtr, strPtr);

            strPtr = "/fo o/ ";
            jsonPtr = new JsonPointer(strPtr);
            Assert.IsTrue(jsonPtr.MoveNext());
            Assert.AreEqual("fo o", jsonPtr.Current);
            Assert.IsTrue(jsonPtr.MoveNext());
            Assert.AreEqual(" ", jsonPtr.Current);
            Assert.IsFalse(jsonPtr.MoveNext());
            PrintPointer(jsonPtr, strPtr);

            strPtr = "/value1/value2/3/value4";
            jsonPtr = new JsonPointer(strPtr);
            Assert.IsTrue(jsonPtr.MoveNext());
            Assert.AreEqual("value1", jsonPtr.Current);
            Assert.IsTrue(jsonPtr.MoveNext());
            Assert.AreEqual("value2", jsonPtr.Current);
            Assert.IsTrue(jsonPtr.MoveNext());
            Assert.AreEqual(3, jsonPtr.CurrentAsInt);
            Assert.IsTrue(jsonPtr.MoveNext());
            Assert.AreEqual("value4", jsonPtr.Current);
            Assert.IsFalse(jsonPtr.MoveNext());
            PrintPointer(jsonPtr, strPtr);
        }

        [Test]
        public static void TestReadJsonPatchBlob()
        {
            AppExeModule.IsRunningTests = true;
            String patchBlob;
            
            patchBlob = "[";
            patchBlob += "{\"replace\":\"/FirstName\", \"value\": \"Hmmz\"},";
            patchBlob += "{\"replace\":\"/FirstName\", \"value\": \"Hmmz\"   }";
            patchBlob += " { \"replace\" :\"/FirstName\" ,  \"value\"  : \"abc123\" }";
            patchBlob += "]";

            Session session = new Session();
            session.Execute(null, () =>
            {
                Session.Current.AttachRootApp(CreateSampleApp().App);
                JsonPatch.EvaluatePatches(patchBlob);
            });
        }

        [Test]
        public static void TestReadJsonPatch()
        {
            AppExeModule.IsRunningTests = true;

            ChangeLog.BeginRequest(new ChangeLog());
            
            AppAndTemplate aat = CreateSampleApp();
            dynamic app = aat.App;

            Object obj = JsonPatch.Evaluate(app, "/FirstName");
            Assert.AreEqual(obj, "Cliff");

            obj = JsonPatch.Evaluate(app, "/LastName");
            Assert.AreEqual(obj, "Barnes");

            obj = JsonPatch.Evaluate(app, "/Items/0/Description");
            Assert.AreEqual(obj, "Take a nap!");

            obj = JsonPatch.Evaluate(app, "/Items/1/IsDone");
            Assert.AreEqual(obj, true);

            obj = JsonPatch.Evaluate(app, "/Items/1");
//                Assert.IsInstanceOf<SampleApp.ItemsApp>(obj);

            Assert.Throws<Exception>(() => { JsonPatch.Evaluate(app, "/Nonono"); },
                                        "Unknown token 'Nonono' in patch message '/Nonono'");

            Int32 repeat = 1;
            DateTime start = DateTime.Now;
            for (Int32 i = 0; i < repeat; i++)
            {
                obj = JsonPatch.Evaluate(app, "/FirstName");
            }
            DateTime stop = DateTime.Now;

            Console.WriteLine("Evaluated {0} jsonpatches in {1} ms.", repeat, (stop - start).TotalMilliseconds);
            ChangeLog.EndRequest();
        }

        [Test]
        public static void TestWriteJsonPatch()
        {
            AppExeModule.IsRunningTests = true;

            AppAndTemplate aat = CreateSampleApp();
            dynamic app = aat.App;
            dynamic item = app.Items[1];
            Template from;
            String str;

            from = aat.Template.Children[0];
            str = JsonPatch.BuildJsonPatch(JsonPatch.REPLACE, app, from, "Hmmz", -1);
            Console.WriteLine(str);

            // "replace":"/FirstName", "value":"hmmz"
            Assert.AreEqual("\"replace\":\"/FirstName\", \"value\":\"Hmmz\"", str);

            //from = aat.Template.Children[2].Children[0].Children[0];
            //str = JsonPatch.BuildJsonPatch(JsonPatchType.replace, item, from, "Hmmz", -1);
            //Console.WriteLine(str);

            //// "replace":"/Items/1/Description", "value":"Hmmz"
            //Assert.AreEqual("\"replace\":\"/Items/1/Description\", \"value\":\"Hmmz\"", str);


            from = aat.Template.Children[0];
            Int32 repeat = 100000;
            DateTime start = DateTime.Now;
            for (Int32 i = 0; i < repeat; i++)
            {
                //                    str = JsonPatch.BuildJsonPatch(JsonPatchType.replace, app, app.Template.FirstName, "Hmmz");
                //                    str = JsonPatchHelper.BuildJsonPatch("replace", item, item.Template.Description, "Hmmz");
                str = JsonPatch.BuildJsonPatch(JsonPatch.REPLACE, app, from, "Hmmz", -1);
            }
            DateTime stop = DateTime.Now;

            Console.WriteLine("Created {0} replace patches in {1} ms", repeat, (stop - start).TotalMilliseconds);
        }

        [Test]
        public static void TestCreateHttpResponseWithPatches()
        {
            Byte[] response = null;
            DateTime start = DateTime.MinValue;
            DateTime stop = DateTime.MinValue;

            AppExeModule.IsRunningTests = true;
            Int32 repeat = 1;

            ChangeLog.BeginRequest(new ChangeLog());
            AppAndTemplate aat = CreateSampleApp();

            StringProperty lastName = aat.Template.Children[1]; 
            ListingProperty items = aat.Template.Children[2];

            dynamic app = aat.App;
            app.LastName = "Ewing";
            app.Items.RemoveAt(0);
            dynamic newitem = app.Items.Add();
            newitem.Description = "Aight!";
            app.LastName = "Poe";

            start = DateTime.Now;
            for (Int32 i = 0; i < repeat; i++)
            {
                ChangeLog.UpdateValue(app, lastName);
//                ChangeLog.RemoveItemInList(app, items, 0);
                ChangeLog.AddItemInList(app, items, app.Items.Count - 1);
                ChangeLog.UpdateValue(app, aat.Template.Children[2].Children[0].Children[0]);
                ChangeLog.UpdateValue(app, lastName);

                response = HttpPatchBuilder.CreateHttpPatchResponse(ChangeLog.Log);
                ChangeLog.Log.Clear();
            }
            stop = DateTime.Now;

            ChangeLog.EndRequest();
            Console.WriteLine("Created {0} responses in {1} ms", repeat, (stop - start).TotalMilliseconds);
            Console.WriteLine(Encoding.UTF8.GetString(response));
        }
 
        private static void PrintPointer(JsonPointer ptr, String originalStr)
        {
            ptr.Reset();
            Console.Write("Tokens for \"" + originalStr + "\": ");
            while (ptr.MoveNext())
            {
                Console.Write('\'');
                Console.Write(ptr.Current);
                Console.Write("' ");
            }
            Console.WriteLine();
        }

        private static AppAndTemplate CreateSampleApp()
        {
            dynamic template = TemplateFromJs.ReadFile("SampleApp.json");
            dynamic app = new App() { Template = template };
            
            app.FirstName = "Cliff";
            app.LastName = "Barnes";

            var itemApp = app.Items.Add();
            itemApp.Description = "Take a nap!";
            itemApp.IsDone = false;

            itemApp = app.Items.Add();
            itemApp.Description = "Fix Apps!";
            itemApp.IsDone = true;

            return new AppAndTemplate(app, template);
        }
    }
}
