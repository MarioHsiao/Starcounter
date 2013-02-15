// ***********************************************************************
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
            AppTemplate template = TemplateFromHtml.CreateFromHtmlFile("testtemplate.html");
            Assert.NotNull(template);
            Assert.IsInstanceOf<StringProperty>(template.Properties[0]);
            Assert.IsInstanceOf<ObjArrTemplate>(template.Properties[1]);
        }


        /// <summary>
        /// Creates from HTML file_ misplaced.
        /// </summary>
        [Test]
        public static void CreateFromHtmlFile_Misplaced() {
            try {
                AppTemplate template = TemplateFromHtml.CreateFromHtmlFile("template\\misplaced.html");
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
