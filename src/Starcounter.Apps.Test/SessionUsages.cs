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
    /// Class SessionUsages
    /// </summary>
    class SessionUsages {

        /// <summary>
        /// Tests some ways of session usages.
        /// </summary>
        [Test]
        public static void TestSessionUsages() {

            Session s = new Session();

            String testString = "";

            // Setting the destroyed events.
            s.Destroyed += (object sender, EventArgs e) => {
                Assert.AreEqual(s, (Session)sender);
                testString += "One";
            };
            s.Destroyed += (object sender, EventArgs e) => {
                Assert.AreEqual(s, (Session)sender);
                testString += "Two";
            };

            // Destroying the session.
            s.Destroy();
            Assert.AreEqual("OneTwo", testString);

            // NOTE: Testing once again that we don't have a double call.
            s.Destroy();
            Assert.AreEqual("OneTwo", testString);
        }
    }
}