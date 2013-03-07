﻿// ***********************************************************************
// <copyright file="TestApp.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using NUnit.Framework;
using Starcounter.Templates;
using System.Diagnostics;

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
            var at = new TPuppet();
            var st = new TString() { Name = "FirstName", Parent = at };
            var app = new Puppet() { Template = at };
            app.Set(st, "Joachim");
            Assert.AreEqual("Joachim", app.Get(st));
            Console.WriteLine(app.ToJson());
        }

        /// <summary>
        /// Tests the nested app.
        /// </summary>
        [Test]
        public static void TestNestedApp() {
            var main = new TPuppet();
            var userId = new TString() { Name = "UserId", Parent = main };
            var search = new TPuppet() { Name = "Search", Parent = main };
            var app = new Puppet() { Template = main };
            var app2 = new Puppet() { Template = search };
            app.Set(userId, "Jocke");
            app.Set(search, app2);
            Console.WriteLine(app.ToJson()); //, IncludeView.Never));
        }

        /// <summary>
        /// Tests the array.
        /// </summary>
        [Test]
        public static void TestArray() {
            var appTemplate = new TPuppet();
            var persons = new TArr<Puppet,TPuppet>() { Name = "Persons", Parent = appTemplate };
            var person = new TPuppet() { Parent = persons };
            var firstName = new TString() { Name = "FirstName", Parent = person };
            var lastName = new TString() { Name = "LastName", Parent = person };
            var address = new TString() { Name = "Address", Parent = person };
            var userId = new TString() { Name = "UserId", Parent = appTemplate };

            var app = new Puppet() { Template = appTemplate };
            var jocke = new Puppet() { Template = person };
            jocke.Set(firstName, "Joachim");
            jocke.Set(lastName, "Wester");
            app.Get(persons).Add(jocke);

            var addie = new Puppet() { Template = person };
            addie.Set(firstName, "Adrienne");
            addie.Set(lastName, "Wester");
            app.Get(persons).Add(addie);

            //	     Assert.AreEqual("[[[\"Joachim\",\"Wester\",null],[\"Adrienne\",\"Wester\",null]],null]",//
            //	                     app.QuickAndDirtyObject.DebugDump());
            Assert.AreEqual("Adrienne", app.Get(persons)[1].Get(firstName));

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