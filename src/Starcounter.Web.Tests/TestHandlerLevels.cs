// ***********************************************************************
// <copyright file="WebResourcesTest.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using Starcounter.Internal.Web;
using NUnit.Framework;
using Starcounter.Advanced.Hypermedia;
using Starcounter.Rest;
using System.Collections.Generic;
using Starcounter.Advanced;
using System.IO;
using System.Reflection;

namespace Starcounter.Internal.Tests {

    /// <summary>
    /// Used for tests initialization/shutdown.
    /// </summary>
    [SetUpFixture]
    class TestHttpHandlersSetup {
        /// <summary>
        /// Tests initialization.
        /// </summary>
        [SetUp]
        public void NewRestTestsSetupInit() {
            Db.SetEnvironment(new DbEnvironment("TestLocalNode", false));
            StarcounterEnvironment.AppName = Path.GetFileNameWithoutExtension(Assembly.GetCallingAssembly().Location);

            Dictionary<UInt16, StaticWebServer> fileServer = new Dictionary<UInt16, StaticWebServer>();
            AppRestServer appServer = new AppRestServer(fileServer);

            UriManagedHandlersCodegen.Setup(null, null, null, null, appServer.HandleRequest);
            Node.InjectHostedImpl(UriManagedHandlersCodegen.DoLocalNodeRest, null);

            // Initializing system profilers.
            Profiler.Init(true);

            X.LocalNode = true;
        }
    }

    /// <summary>
    /// Testing different levels of handlers.
    /// </summary>
    [TestFixture]
    public class TestHandlerLevels {

        [Test]
        public static void TestBasics() {

            UriHandlersManager.ResetUriHandlersManagers();
            HandlerOptions.DefaultHandlerOptions = HandlerOptions.DefaultLevel;

            HandlerOptions ho0 = new HandlerOptions() { HandlerLevel = HandlerOptions.HandlerLevels.DefaultLevel };
            HandlerOptions ho1 = new HandlerOptions() { HandlerLevel = HandlerOptions.HandlerLevels.ApplicationLevel };
            HandlerOptions ho2 = new HandlerOptions() { HandlerLevel = HandlerOptions.HandlerLevels.ApplicationExtraLevel };
            
            Handle.GET("/handler_lev0_normal", () => { return "/handler_lev0_normal"; }, ho0);
            Handle.GET("/handler_lev1_normal", () => { return "/handler_lev1_normal"; }, ho1);
            Handle.GET("/handler_lev2_normal", () => { return "/handler_lev2_normal"; }, ho2);

            Response resp;
            X.GET("/handler_lev0_normal", out resp, null, 0);
            Assert.AreEqual(resp.Body, "/handler_lev0_normal");

            X.GET("/handler_lev0_normal", out resp, null, 0, ho0);
            Assert.AreEqual(resp.Body, "/handler_lev0_normal");

            X.GET("/handler_lev1_normal", out resp, null, 0, ho1);
            Assert.AreEqual(resp.Body, "/handler_lev1_normal");

            X.GET("/handler_lev2_normal", out resp, null, 0, ho2);
            Assert.AreEqual(resp.Body, "/handler_lev2_normal");

            // ==============================================

            Handle.GET("/handler_multi_a", () => { return "/handler_multi_a_0"; }, ho0);
            Handle.GET("/handler_multi_a", () => { return "/handler_multi_a_1"; }, ho1);
            Handle.GET("/handler_multi_a", () => { return "/handler_multi_a_2"; }, ho2);

            X.GET("/handler_multi_a", out resp, null, 0, ho0);
            Assert.AreEqual(resp.Body, "/handler_multi_a_0");

            X.GET("/handler_multi_a", out resp, null, 0, ho1);
            Assert.AreEqual(resp.Body, "/handler_multi_a_1");

            X.GET("/handler_multi_a", out resp, null, 0, ho2);
            Assert.AreEqual(resp.Body, "/handler_multi_a_2");

            // ==============================================

            X.GET("/handler_multi_a", out resp);
            Assert.AreEqual(resp.Body, "/handler_multi_a_0");

            // ==============================================

            Handle.GET("/handler_multi_b", () => { return HandlerStatus.NotHandled; }, ho0);
            Handle.GET("/handler_multi_b", () => { return HandlerStatus.NotHandled; }, ho1);
            Handle.GET("/handler_multi_b", () => { return "/handler_multi_b"; }, ho2);

            X.GET("/handler_multi_b", out resp);
            Assert.AreEqual(false, resp.IsSuccessStatusCode);

            X.GET("/handler_multi_b", out resp, null, 0, ho0);
            Assert.AreEqual(false, resp.IsSuccessStatusCode);

            X.GET("/handler_multi_b", out resp, null, 0, ho1);
            Assert.AreEqual(false, resp.IsSuccessStatusCode);

            X.GET("/handler_multi_b", out resp, null, 0, ho2);
            Assert.AreEqual("/handler_multi_b", resp.Body);
        }

        [Test]
        public static void TestMapperHandlers() {
            UriHandlersManager.ResetUriHandlersManagers();
            HandlerOptions.DefaultHandlerOptions = HandlerOptions.DefaultLevel;

            Handle.GET("/handler_normal", () => { return "/handler_normal0"; });

            Response resp;
            X.GET("/handler_normal", out resp);
            Assert.AreEqual(resp.Body, "/handler_normal0");

            // Adding new handlers level and setting it to default.
            HandlerOptions.DefaultHandlerOptions = HandlerOptions.ApplicationLevel;

            HandlerOptions ho1 = new HandlerOptions() { HandlerLevel = HandlerOptions.HandlerLevels.ApplicationLevel };

            Handle.GET("/handler_normal", () => { return "/handler_normal1"; }, ho1);

            X.GET("/handler_normal", out resp);
            Assert.AreEqual(resp.Body, "/handler_normal1");
        }

        [Test]
        public static void TestAppNameHandlers() {
            UriHandlersManager.ResetUriHandlersManagers();
            HandlerOptions.DefaultHandlerOptions = HandlerOptions.DefaultLevel;

            Handle.GET("/handler_normal", () => {
                Assert.AreEqual("nunit.core", StarcounterEnvironment.AppName);
                return "/handler_normal0";
            });

            HandlerOptions ho1 = new HandlerOptions() {
                HandlerLevel = HandlerOptions.HandlerLevels.ApplicationLevel,
                AppName = "NewApp1" };

            Handle.GET("/handler_normal", () => {
                Assert.AreEqual("NewApp1", StarcounterEnvironment.AppName);
                return "/handler_normal1";
            }, ho1);

            HandlerOptions ho2 = new HandlerOptions() {
                HandlerLevel = HandlerOptions.HandlerLevels.ApplicationExtraLevel,
                AppName = "NewApp2"
            };

            Handle.GET("/handler_normal", () => {
                Assert.AreEqual("NewApp2", StarcounterEnvironment.AppName);
                return "/handler_normal2";
            }, ho2);

            Response resp;
            X.GET("/handler_normal", out resp);
            Assert.AreEqual(resp.Body, "/handler_normal0");

            X.GET("/handler_normal", out resp, null, 0, ho1);
            Assert.AreEqual(resp.Body, "/handler_normal1");

            X.GET("/handler_normal", out resp, null, 0, ho2);
            Assert.AreEqual(resp.Body, "/handler_normal2");
        }
    }
}