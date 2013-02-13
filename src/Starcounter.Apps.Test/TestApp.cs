// ***********************************************************************
// <copyright file="TestApp.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using NUnit.Framework;
using Starcounter.Templates;
using Starcounter.Internal.ExeModule;
using System.Diagnostics;

namespace Starcounter.Internal.Test {
    /// <summary>
    /// Class TestApp
    /// </summary>
    class TestApp {
        /// <summary>
        /// Initializes static members of the <see cref="TestApp" /> class.
        /// </summary>
        static TestApp() {
            AppExeModule.IsRunningTests = true;
        }

        /// <summary>
        /// Tests the set get.
        /// </summary>
        [Test]
        public static void TestSetGet() {
            var at = new AppTemplate();
            var st = new StringProperty() { Name = "FirstName", Parent = at };
            var app = new App() { Template = at };
            app.SetValue(st, "Joachim");
            Assert.AreEqual("Joachim", app.GetValue(st));
            Console.WriteLine(app.ToJson());
        }

        /// <summary>
        /// Tests the nested app.
        /// </summary>
        [Test]
        public static void TestNestedApp() {
            var main = new AppTemplate();
            var userId = new StringProperty() { Name = "UserId", Parent = main };
            var search = new AppTemplate() { Name = "Search", Parent = main };
            var app = new App() { Template = main };
            var app2 = new App() { Template = search };
            app.SetValue(userId, "Jocke");
            app.SetValue(search, app2);
            Console.WriteLine(app.ToJson(false)); //, IncludeView.Never));
        }

        /// <summary>
        /// Tests the array.
        /// </summary>
        [Test]
        public static void TestArray() {
            var appTemplate = new AppTemplate();
            var persons = new ListingProperty<App,AppTemplate>() { Name = "Persons", Parent = appTemplate };
            var person = new AppTemplate() { Parent = persons };
            var firstName = new StringProperty() { Name = "FirstName", Parent = person };
            var lastName = new StringProperty() { Name = "LastName", Parent = person };
            var address = new StringProperty() { Name = "Address", Parent = person };
            var userId = new StringProperty() { Name = "UserId", Parent = appTemplate };

            var app = new App() { Template = appTemplate };
            var jocke = new App() { Template = person };
            jocke.SetValue(firstName, "Joachim");
            jocke.SetValue(lastName, "Wester");
            app.GetValue(persons).Add(jocke);

            var addie = new App() { Template = person };
            addie.SetValue(firstName, "Adrienne");
            addie.SetValue(lastName, "Wester");
            app.GetValue(persons).Add(addie);

            //	     Assert.AreEqual("[[[\"Joachim\",\"Wester\",null],[\"Adrienne\",\"Wester\",null]],null]",//
            //	                     app.QuickAndDirtyObject.DebugDump());
            Assert.AreEqual("Adrienne", app.GetValue(persons)[1].GetValue(firstName));

            Console.WriteLine("Raw tuple:");
            //	     Console.WriteLine(app.QuickAndDirtyObject.DebugDump());
            Console.WriteLine("");
            Console.WriteLine("JSON:");
            Console.WriteLine(app.ToJson());
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
//            Console.WriteLine(String.Format("Time to create {0} documents was {1} milliseconds", count, sw.ElapsedMilliseconds));
//        }
    }
}