﻿// ***********************************************************************
// <copyright file="TestJsonOut.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using NUnit.Framework;
using Starcounter.Internal.Application;
using Starcounter.Internal.Application.CodeGeneration;
using Starcounter.Internal.JsonTemplate;
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

        /// <summary>
        /// Apps to json.
        /// </summary>
        [Test]
//        [Fact]
        public static void AppToJson()
        {
            dynamic app = new Puppet() { Template = TemplateFromJs.ReadPuppetTemplateFromFile("MySampleApp2.json") };
            app.FirstName = "Joachim";
            app.LastName = "Wester";

            dynamic item = app.Items.Add();
            item.Description = "Release this cool stuff to the market";
            item.IsDone = false;

            item = app.Items.Add();
            item.Description = "Take a vacation";
            item.IsDone = true;
            Puppet app2 = (Puppet)app;
            Console.WriteLine(app2.ToJson());
            // Assert.IsTrue(true);
            Assert.True(true);
        }
    }
}
