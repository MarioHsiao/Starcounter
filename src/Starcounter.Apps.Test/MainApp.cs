﻿// ***********************************************************************
// <copyright file="MainApp.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;
using System;
using NUnit.Framework;
using Starcounter.Advanced;
using System.Diagnostics;

namespace PlayersDemoApp {
    internal static class Helper {
        [Conditional("CONSOLE")]
        internal static void ConsoleWriteLine(string msg) {
            Console.WriteLine(msg);
        }
    }
    /// <summary>
    /// Class MainApp
    /// </summary>
    partial class MainApp : StarcounterBase {

        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        public static void Main() {

//            GET("/{x}/test/test", (int playerId) => {
//                Assert.AreEqual(123, playerId);
//                Helper.ConsoleWriteLine("playerId=" + playerId);
//                return null;
//            });

//            GET("/{x}", () => {
//                return "404 Not Found";
//            });

            GET("/players/{x}", (int playerId) => {
                Assert.AreEqual(123, playerId);
                Helper.ConsoleWriteLine("playerId=" + playerId);
                return null;
            });

            GET("/dashboard/{x}", (int playerId) => {
                Assert.AreEqual(123, playerId);
                Helper.ConsoleWriteLine("playerId=" + playerId);
                return null;
            });

            GET("/players?f={x}", (string fullName) => {
                Assert.AreEqual("KalleKula", fullName);
                Helper.ConsoleWriteLine("fullName=" + fullName);
                return null;
            });

            PUT("/players/{x}", (int playerId, Request request) => {
                Assert.AreEqual(123, playerId);
//                Assert.IsNotNull(request);
                Helper.ConsoleWriteLine("playerId: " + playerId); //+ ", request: " + request);
                return null;
            });

            POST("/transfer?f={x}&t={x}&x={x}",  (int from, int to, int amount) => {
                Assert.AreEqual(99, from);
                Assert.AreEqual(365, to);
                Assert.AreEqual(46, amount);
                Helper.ConsoleWriteLine("From: " + from + ", to: " + to + ", amount: " + amount);
                return null;
            });

            POST("/deposit?a={x}&x={x}", (int to, int amount) => {
                Assert.AreEqual(56754, to);
                Assert.AreEqual(34653, amount);
                Helper.ConsoleWriteLine("To: " + to + ", amount: " + amount);
                return null;
            });

            DELETE("/all", () => {
                Helper.ConsoleWriteLine("deleteAll");
                return null;
            });
        }
    }
}