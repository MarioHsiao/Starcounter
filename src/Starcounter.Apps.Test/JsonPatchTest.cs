// ***********************************************************************
// <copyright file="JsonPatchTest.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Runtime.InteropServices;
using System.Text;
using NUnit.Framework;
using Starcounter.Apps;
using Starcounter.Internal;
using Starcounter.Templates;
using Starcounter.Internal.JsonTemplate;

namespace Starcounter.Internal.JsonPatch.Test
{
    /// <summary>
    /// Class JsonPatchTest
    /// </summary>
    [TestFixture]
    public class JsonPatchTest
    {
        /// <summary>
        /// Tests the json pointer.
        /// </summary>
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

        /// <summary>
        /// Tests the read json patch BLOB.
        /// </summary>
        [Test]
        public static void TestReadJsonPatchBlob()
        {
            String patchBlob;
            
            patchBlob = "[";
            patchBlob += "{\"replace\":\"/FirstName\", \"value\": \"Hmmz\"},";
            patchBlob += "{\"replace\":\"/FirstName\", \"value\": \"Hmmz\"   }";
            patchBlob += " { \"replace\" :\"/FirstName\" ,  \"value\"  : \"abc123\" }";
            patchBlob += "]";

            Session session = new Session();
            Session.Execute(session, () =>
            {
                Obj rootApp = CreateSampleApp().App;
                Session.Data = rootApp;
                JsonPatch.EvaluatePatches(rootApp, System.Text.Encoding.UTF8.GetBytes(patchBlob));
            });
        }

        /// <summary>
        /// Tests the read json patch.
        /// </summary>
        [Test]
        public static void TestReadJsonPatch()
        {
            Session session = new Session();

            Session.Execute(session, () => {

                AppAndTemplate aat = CreateSampleApp();
                dynamic app = aat.App;

                AppAndTemplate obj = JsonPatch.Evaluate(app, "/FirstName");
                String value = obj.App.Get((TString)obj.Template);
                Assert.AreEqual(value, "Cliff");

                obj = JsonPatch.Evaluate(app, "/LastName");
                value = obj.App.Get((TString)obj.Template);
                Assert.AreEqual(value, "Barnes");

                obj = JsonPatch.Evaluate(app, "/Items/0/Description");
                value = obj.App.Get((TString)obj.Template);
                Assert.AreEqual(value, "Take a nap!");

                obj = JsonPatch.Evaluate(app, "/Items/1/IsDone");
                bool b = obj.App.Get((TBool)obj.Template);
                Assert.AreEqual(b, true);

                obj = JsonPatch.Evaluate(app, "/Items/1");
                //                Assert.IsInstanceOf<SampleApp.ItemsApp>(obj);

                Assert.Throws<Exception>(() => { JsonPatch.Evaluate(app, "/Nonono"); },
                                            "Unknown token 'Nonono' in patch message '/Nonono'");

                Int32 repeat = 1;
                DateTime start = DateTime.Now;
                for (Int32 i = 0; i < repeat; i++) {
                    obj = JsonPatch.Evaluate(app, "/FirstName");
                }
                DateTime stop = DateTime.Now;

                Console.WriteLine("Evaluated {0} jsonpatches in {1} ms.", repeat, (stop - start).TotalMilliseconds);
            });
        }

        /// <summary>
        /// Tests the write json patch.
        /// </summary>
        [Test]
        public static void TestWriteJsonPatch()
        {
            AppAndTemplate aat = CreateSampleApp();
            dynamic app = aat.App;
            dynamic item = app.Items[1];
            Template from;
            String str;

            TPuppet appt = (TPuppet)aat.Template;
            from = appt.Properties[0];
            str = JsonPatch.BuildJsonPatch(JsonPatch.REPLACE, app, from, "Hmmz", -1);
            Console.WriteLine(str);

            // "replace":"/FirstName", "value":"hmmz"
            Assert.AreEqual("\"replace\":\"/FirstName\", \"value\":\"Hmmz\"", str);

            //from = aat.Template.Children[2].Children[0].Children[0];
            //str = JsonPatch.BuildJsonPatch(JsonPatchType.replace, item, from, "Hmmz", -1);
            //Console.WriteLine(str);

            //// "replace":"/Items/1/Description", "value":"Hmmz"
            //Assert.AreEqual("\"replace\":\"/Items/1/Description\", \"value\":\"Hmmz\"", str);


            from = appt.Properties[0];
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

        /// <summary>
        /// Tests the app index path.
        /// </summary>
        [Test]
        public static void TestAppIndexPath()
        {
            AppAndTemplate aat = CreateSampleApp();
            TObj appt = (TPuppet)aat.Template;

            TString firstName = (TString)appt.Properties[0];
            Int32[] indexPath = aat.App.IndexPathFor(firstName);
            VerifyIndexPath(new Int32[] { 0 }, indexPath);

            TObj anotherAppt = (TPuppet)appt.Properties[3];
            Obj nearestApp = aat.App.Get(anotherAppt);

            TString desc = (TString)anotherAppt.Properties[1];
            indexPath = nearestApp.IndexPathFor(desc);
            VerifyIndexPath(new Int32[] { 3, 1 }, indexPath);

            TObjArr itemProperty = (TObjArr)appt.Properties[2];
            Arr items = aat.App.Get(itemProperty);

            nearestApp = items[1];
            anotherAppt = nearestApp.Template;

            TBool delete = (TBool)anotherAppt.Properties[2];
            indexPath = nearestApp.IndexPathFor(delete);
            VerifyIndexPath(new Int32[] { 2, 1, 2 }, indexPath);
        }

        /// <summary>
        /// Verifies the index path.
        /// </summary>
        /// <param name="expected">The expected.</param>
        /// <param name="received">The received.</param>
        private static void VerifyIndexPath(Int32[] expected, Int32[] received)
        {
            Assert.AreEqual(expected.Length, received.Length);
            for (Int32 i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], received[i]);
            }
        }

        /// <summary>
        /// Tests the create HTTP response with patches.
        /// </summary>
        [Test]
        public static void TestCreateHttpResponseWithPatches()
        {
            TPuppet appt;
            Byte[] response = null;
            DateTime start = DateTime.MinValue;
            DateTime stop = DateTime.MinValue;

            Int32 repeat = 1;

            ChangeLog log = new ChangeLog();
            ChangeLog.CurrentOnThread = log;

            AppAndTemplate aat = CreateSampleApp();

            appt = (TPuppet)aat.Template;

            TString lastName = (TString)appt.Properties[1];
            TObjArr items = (TObjArr)appt.Properties[2];

            dynamic app = aat.App;
            app.LastName = "Ewing";
            app.Items.RemoveAt(0);
            dynamic newitem = app.Items.Add();
            newitem.Description = "Aight!";
            app.LastName = "Poe";

            start = DateTime.Now;
            for (Int32 i = 0; i < repeat; i++) {
                ChangeLog.UpdateValue(app, lastName);
                //                ChangeLog.RemoveItemInList(app, items, 0);
                ChangeLog.AddItemInList(app, items, app.Items.Count - 1);

                //ChangeLog.UpdateValue(app, aat.Template.Children[2].Children[0].Children[0]);
                ChangeLog.UpdateValue(app, lastName);

                response = HttpPatchBuilder.CreateHttpPatchResponse(log);
                log.Clear();
            }
            stop = DateTime.Now;
            ChangeLog.CurrentOnThread = null;

            Console.WriteLine("Created {0} responses in {1} ms", repeat, (stop - start).TotalMilliseconds);
            Console.WriteLine(Encoding.UTF8.GetString(response));
        }

        /// <summary>
        /// Prints the pointer.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="originalStr">The original STR.</param>
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

        /// <summary>
        /// Creates the sample app.
        /// </summary>
        /// <returns>AppAndTemplate.</returns>
        private static AppAndTemplate CreateSampleApp()
        {
            dynamic template = TemplateFromJs.ReadPuppetTemplateFromFile("SampleApp.json");
            dynamic app = new Puppet() { Template = template };
            
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
