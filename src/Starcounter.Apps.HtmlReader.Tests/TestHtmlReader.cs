﻿// ***********************************************************************
// <copyright file="TestHtmlReader.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using NUnit.Framework;
using Starcounter.Templates;

namespace Starcounter.Internal.JsonTemplate.Tests {
    /// <summary>
    /// Class JsToTemplateTests
    /// </summary>
    public class JsToTemplateTests {


        /// <summary>
        /// Creates from HTML file.
        /// </summary>
        [Test]
        public static void CreateFromHtmlFile() {
            TPuppet template = TemplateFromHtml.CreateFromHtmlFile("testtemplate.html");
            Assert.NotNull(template);
            Assert.IsInstanceOf<TString>(template.Properties[0]);
            Assert.IsInstanceOf<TObjArr>(template.Properties[1]);
        }


        /// <summary>
        /// Creates from HTML file_ misplaced.
        /// </summary>
        [Test]
        public static void CreateFromHtmlFile_Misplaced() {
            try {
                TPuppet template = TemplateFromHtml.CreateFromHtmlFile("template\\misplaced.html");
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
                Console.WriteLine("Exception was successfully provoked");
                return;
            }
            Assert.Fail();
        }
    }
}
