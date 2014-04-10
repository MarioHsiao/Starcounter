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

            UriManagedHandlersCodegen.Setup(null, null, appServer.HandleRequest, UriHandlersManager.AddExtraHandlerLevel);
            Node.InjectHostedImpl(UriManagedHandlersCodegen.DoLocalNodeRest, null);

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
            HandlerOptions.Reset();

            Handlers.AddExtraHandlerLevel();
            Handlers.AddExtraHandlerLevel();
            Handlers.AddExtraHandlerLevel();

            HandlerOptions ho0 = new HandlerOptions() { HandlerLevel = 0 };
            HandlerOptions ho1 = new HandlerOptions() { HandlerLevel = 1 };
            HandlerOptions ho2 = new HandlerOptions() { HandlerLevel = 2 };
            HandlerOptions ho3 = new HandlerOptions() { HandlerLevel = 3 };
            
            Handle.GET("/handler_L0_normal", () => { return "/handler_L0_normal"; }, ho0);
            Handle.GET("/handler_L1_normal", () => { return "/handler_L1_normal"; }, ho1);
            Handle.GET("/handler_L2_normal", () => { return "/handler_L2_normal"; }, ho2);
            Handle.GET("/handler_L3_normal", () => { return "/handler_L3_normal"; }, ho3);

            Response resp;
            X.GET("/handler_L0_normal", out resp, null, 0);
            Assert.AreEqual(resp.Body, "/handler_L0_normal");

            X.GET("/handler_L0_normal", out resp, null, 0, ho0);
            Assert.AreEqual(resp.Body, "/handler_L0_normal");

            X.GET("/handler_L1_normal", out resp, null, 0, ho1);
            Assert.AreEqual(resp.Body, "/handler_L1_normal");

            X.GET("/handler_L2_normal", out resp, null, 0, ho2);
            Assert.AreEqual(resp.Body, "/handler_L2_normal");

            X.GET("/handler_L3_normal", out resp, null, 0, ho3);
            Assert.AreEqual(resp.Body, "/handler_L3_normal");

            // ==============================================

            Handle.GET("/handler_multi_a", () => { return "/handler_multi_a_0"; }, ho0);
            Handle.GET("/handler_multi_a", () => { return "/handler_multi_a_1"; }, ho1);
            Handle.GET("/handler_multi_a", () => { return "/handler_multi_a_2"; }, ho2);
            Handle.GET("/handler_multi_a", () => { return "/handler_multi_a_3"; }, ho3);

            X.GET("/handler_multi_a", out resp, null, 0, ho0);
            Assert.AreEqual(resp.Body, "/handler_multi_a_0");

            X.GET("/handler_multi_a", out resp, null, 0, ho1);
            Assert.AreEqual(resp.Body, "/handler_multi_a_1");

            X.GET("/handler_multi_a", out resp, null, 0, ho2);
            Assert.AreEqual(resp.Body, "/handler_multi_a_2");

            X.GET("/handler_multi_a", out resp, null, 0, ho3);
            Assert.AreEqual(resp.Body, "/handler_multi_a_3");

            // ==============================================

            X.GET("/handler_multi_a", out resp);
            Assert.AreEqual(resp.Body, "/handler_multi_a_0");

            // ==============================================

            Handle.GET("/handler_multi_b", () => { return HandlerStatus.NotHandled; }, ho0);
            Handle.GET("/handler_multi_b", () => { return HandlerStatus.NotHandled; }, ho1);
            Handle.GET("/handler_multi_b", () => { return "/handler_multi_b"; }, ho2);

            X.GET("/handler_multi_b", out resp);
            Assert.AreEqual(resp.Body, "/handler_multi_b");

            X.GET("/handler_multi_b", out resp, null, 0, ho0);
            Assert.AreEqual(resp, null);

            X.GET("/handler_multi_b", out resp, null, 0, ho1);
            Assert.AreEqual(resp, null);
        }

        [Test]
        public static void TestMapperHandlers() {
            UriHandlersManager.ResetUriHandlersManagers();
            HandlerOptions.Reset();

            Handle.GET("/handler_normal", () => { return "/handler_normal0"; });

            // Adding new handlers level and setting it to default.
            Handlers.AddExtraHandlerLevel();
            HandlerOptions.DefaultHandlerLevel = 1;

            HandlerOptions ho1 = new HandlerOptions() { HandlerLevel = 1 };

            Handle.GET("/handler_normal", () => { return "/handler_normal1"; }, ho1);

            Response resp;
            X.GET("/handler_normal", out resp);
            Assert.AreEqual(resp.Body, "/handler_normal0");
        }
    }
}