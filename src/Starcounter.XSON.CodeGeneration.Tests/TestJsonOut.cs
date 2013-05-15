// ***********************************************************************
// <copyright file="TestJsonOut.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using NUnit.Framework;
using Starcounter.Internal;
using Starcounter.Templates;
using Starcounter.XSON.CodeGeneration;

namespace Starcounter.Client.Tests.Application
{
    /// <summary>
    /// Class TestJson
    /// </summary>
    [TestFixture]
    public class TestJson
    {
        private static ITypedJsonFactory factory;

        [TestFixtureSetUp]
        public static void InitializeTest() {
            factory = new TypedJsonFactory();
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
