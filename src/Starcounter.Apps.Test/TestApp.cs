// ***********************************************************************
// <copyright file="TestApp.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using NUnit.Framework;
using Starcounter.Templates;
using System.Diagnostics;

using TJson = Starcounter.Templates.TObject;
using PlayersDemoApp;

namespace Starcounter.Internal.Test {
    /// <summary>
    /// Class TestApp
    /// </summary>
    class TestApp {

        /// <summary>
        /// Tests the set get.
        /// </summary>
        [Test]
        public static void TestSetGet() {
            var at = new TJson();
            var st = new TString() { TemplateName = "FirstName", Parent = at };
            var app = new Json() { Template = at };
			st.Setter(app, "Joachim");
			Assert.AreEqual("Joachim", st.Getter(app));
            Helper.ConsoleWriteLine(app.ToJson());
        }

        /// <summary>
        /// Tests the nested app.
        /// </summary>
        [Test]
        public static void TestNestedApp() {
            var main = new TJson();
            var userId = new TString() { TemplateName = "UserId", Parent = main };
            var search = new TJson() { TemplateName = "Search", Parent = main };
            var app = new Json() { Template = main };
            var app2 = new Json() { Template = search };
            userId.Setter(app, "Jocke");
            search.Setter(app, app2);
            Helper.ConsoleWriteLine(app.ToJson()); //, IncludeView.Never));
        }

        /// <summary>
        /// Tests the array.
        /// </summary>
        [Test]
        public static void TestArray() {
            var appTemplate = new TJson();
            var persons = new TArray<Json>() { TemplateName = "Persons", Parent = appTemplate };
            var person = new TJson() { Parent = persons };

//            persons.App = person;

            var firstName = new TString() { TemplateName = "FirstName", Parent = person };
            var lastName = new TString() { TemplateName = "LastName", Parent = person };
            var address = new TString() { TemplateName = "Address", Parent = person };
            var userId = new TString() { TemplateName = "UserId", Parent = appTemplate };

            var obj = new Json() { Template = appTemplate };
            var jocke = new Json() { Template = person };

            firstName.Setter(jocke, "Joachim");
            lastName.Setter(jocke, "Wester");
            persons.Getter(obj).Add(jocke);

            var addie = new Json() { Template = person };
            firstName.Setter(addie, "Adrienne");
            lastName.Setter(addie, "Wester");
            persons.Getter(obj).Add(addie);

            //	     Assert.AreEqual("[[[\"Joachim\",\"Wester\",null],[\"Adrienne\",\"Wester\",null]],null]",//
            //	                     app.QuickAndDirtyObject.DebugDump());
            Assert.AreEqual("Adrienne", firstName.Getter((Json)(persons.Getter(obj)._GetAt(1))));

            Helper.ConsoleWriteLine("Raw tuple:");
            //	     Helper.ConsoleWriteLine(app.QuickAndDirtyObject.DebugDump());
            Helper.ConsoleWriteLine("");
            Helper.ConsoleWriteLine("JSON:");
            Helper.ConsoleWriteLine(obj.ToJson());
        }

//        [Test]
//        public void BenchmarkCreateApp() {
//            Stopwatch sw = new Stopwatch();
//            int count = 1000000;
//            sw.Start();
//            for (int t = 0; t < count; t++) {
//                var app = new PlayerAndAccounts();
//                app.FullName = t.ToString();
//                app.PlayerId = t;
//                var acc = app.Accounts.Add();
//                acc.AccountId = t;
//                acc.AccountType = 1;
//                acc.Balance = 1000;
//            }
//            sw.Stop();
//            Helper.ConsoleWriteLine(String.Format("Time to create {0} documents was {1} milliseconds", count, sw.ElapsedMilliseconds));
//        }
    }
}