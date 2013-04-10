// ***********************************************************************
// <copyright file="WebResourcesTest.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using Starcounter.Internal.Web;
using NUnit.Framework;
using Starcounter.Advanced.Hypermedia;

namespace Starcounter.Internal.Tests {
    /// <summary>
    /// </summary>
    public class TestHtmlMerge {

        /// <summary>
        /// Merges two simple html strings into one
        /// </summary>
        [Test]
        public static void SimpleMergeHtml() {
            var a = @"
<div global='Base1'>
   <div Global='Person.FirstName'>First</div>
</div>";
            var b = @"
<div global='Base2'>
   <div Global='Person.LastName'>Last</div>
</div>";
            var d = @"
<div global='Base1'>
   <div Global='Person.FirstName'>First</div>
</div>

<div global='Base2'>
   <div Global='Person.LastName'>Last</div>
</div>";

            var merger = new HtmlMerger(a, b);
            merger.Merge();
            var c = merger.GetString();
            Console.WriteLine();
            Assert.AreEqual(c[0], d[0]);
            Assert.AreEqual(c, d);
        }


        /// <summary>
        /// Merges two simple html strings with overlapping id
        /// </summary>
        [Test]
        public static void MergeHtmlWithIdOverride() {
            var a = @"
<div id='Base'>
   <div id='FirstName'>First</div>
</div>";
            var b = @"
<div id='Base'>
   <div id='FistName'>First</div>
   <div id='LastName'>Last</div>
</div>";
            var d = @"
<div id='Base'>
   <div id='FirstName'>First</div>
   <div id='LastName'>Last</div>
</div>";

            var merger = new HtmlMerger(a, b);
            merger.Merge();
            var c = merger.GetString();
            Console.WriteLine();
            Assert.AreEqual(c[0], d[0]);
            Assert.AreEqual(c, d);
        }

    
    }

}
