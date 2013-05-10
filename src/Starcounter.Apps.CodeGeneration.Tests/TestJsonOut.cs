// ***********************************************************************
// <copyright file="TestJsonOut.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using NUnit.Framework;
using Starcounter.Templates;
using Starcounter.Templates.Interfaces;

namespace Starcounter.Client.Tests.Application
{

    /// <summary>
    /// Class TestJson
    /// </summary>
    [TestFixture]
    public class TestJson
    {
        private static IJsonFactory factory;

        [TestFixtureSetUp]
        public static void InitializeTest() {
            factory = new JsonFactoryImpl();
        }

        /// <summary>
        /// Apps to json.
        /// </summary>
        [Test]
//        [Fact]
        public static void AppToJson()
        {
            dynamic app = new Json() { Template = (TObj)factory.CreateJsonTemplateFromFile("MySampleApp2.json") };
            app.FirstName = "Joachim";
            app.LastName = "Wester";

            dynamic item = app.Items.Add();
            item.Description = "Release this cool stuff to the market";
            item.IsDone = false;

            item = app.Items.Add();
            item.Description = "Take a vacation";
            item.IsDone = true;
            Json app2 = (Json)app;
            Console.WriteLine(app2.ToJson());
            // Assert.IsTrue(true);
            Assert.True(true);
        }
    }
}
