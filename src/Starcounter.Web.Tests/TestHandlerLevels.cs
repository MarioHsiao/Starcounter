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

            UriManagedHandlersCodegen.Setup(null, null, null, null, appServer.RunDelegateAndProcessResponse);
            Node.InjectHostedImpl(UriManagedHandlersCodegen.RunUriMatcherAndCallHandler, null);

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
            
            String handlerUri = "/HandlerLevel";

            Handle.GET(handlerUri + "0", () => { return handlerUri + "0"; }, new HandlerOptions());
            Handle.GET(handlerUri + "1", () => { return handlerUri + "1"; }, new HandlerOptions() { HandlerLevel = HandlerOptions.HandlerLevels.ApplicationLevel });
            Handle.GET(handlerUri + "2", () => { return handlerUri + "2"; }, new HandlerOptions() { HandlerLevel = HandlerOptions.HandlerLevels.ApplicationExtraLevel });

            Response resp;
            X.GET(handlerUri + "0", out resp, null, 0);
            Assert.AreEqual(resp.Body, handlerUri + "0");

            X.GET(handlerUri + "0", out resp, null, 0, new HandlerOptions());
            Assert.AreEqual(resp.Body, handlerUri + "0");

            X.GET(handlerUri + "1", out resp, null, 0, new HandlerOptions() { HandlerLevel = HandlerOptions.HandlerLevels.ApplicationLevel });
            Assert.AreEqual(resp.Body, handlerUri + "1");

            X.GET(handlerUri + "2", out resp, null, 0, new HandlerOptions() { HandlerLevel = HandlerOptions.HandlerLevels.ApplicationExtraLevel });
            Assert.AreEqual(resp.Body, handlerUri + "2");

            // ==============================================

            handlerUri = "/HandlerMultiA";

            Handle.GET(handlerUri, () => { return handlerUri + "0"; }, new HandlerOptions());
            Handle.GET(handlerUri, () => { return handlerUri + "1"; }, new HandlerOptions() { HandlerLevel = HandlerOptions.HandlerLevels.ApplicationLevel });
            Handle.GET(handlerUri, () => { return handlerUri + "2"; }, new HandlerOptions() { HandlerLevel = HandlerOptions.HandlerLevels.ApplicationExtraLevel });

            X.GET(handlerUri, out resp, null, 0, new HandlerOptions());
            Assert.AreEqual(resp.Body, handlerUri + "0");

            X.GET(handlerUri, out resp, null, 0, new HandlerOptions() { HandlerLevel = HandlerOptions.HandlerLevels.ApplicationLevel });
            Assert.AreEqual(resp.Body, handlerUri + "1");

            X.GET(handlerUri, out resp, null, 0, new HandlerOptions() { HandlerLevel = HandlerOptions.HandlerLevels.ApplicationExtraLevel });
            Assert.AreEqual(resp.Body, handlerUri + "2");

            X.GET(handlerUri, out resp);
            Assert.AreEqual(resp.Body, handlerUri + "0");

            // ==============================================

            handlerUri = "/HandlerMultiB";

            Handle.GET(handlerUri, () => { return null; }, new HandlerOptions());
            Handle.GET(handlerUri, () => { return null; }, new HandlerOptions() { HandlerLevel = HandlerOptions.HandlerLevels.ApplicationLevel });
            Handle.GET(handlerUri, () => { return handlerUri; }, new HandlerOptions() { HandlerLevel = HandlerOptions.HandlerLevels.ApplicationExtraLevel });

            X.GET(handlerUri, out resp);
            Assert.AreEqual(null, resp);

            X.GET(handlerUri, out resp, null, 0, new HandlerOptions());
            Assert.AreEqual(null, resp);

            X.GET(handlerUri, out resp, null, 0, new HandlerOptions() { HandlerLevel = HandlerOptions.HandlerLevels.ApplicationLevel });
            Assert.AreEqual(null, resp);

            X.GET(handlerUri, out resp, null, 0, new HandlerOptions() { HandlerLevel = HandlerOptions.HandlerLevels.ApplicationExtraLevel });
            Assert.AreEqual(handlerUri, resp.Body);
        }

        [Test]
        public static void TestMapperHandlers() {

            UriHandlersManager.ResetUriHandlersManagers();

            String handlerUri = "/Handler";

            Handle.GET(handlerUri, () => { return handlerUri + "0"; });

            Response resp;
            X.GET(handlerUri, out resp);
            Assert.AreEqual(resp.Body, handlerUri + "0");

            X.GET(handlerUri, out resp, null, 0, new HandlerOptions());
            Assert.AreEqual(resp.Body, handlerUri + "0");

            X.GET(handlerUri, out resp, null, 0, new HandlerOptions() { HandlerLevel = HandlerOptions.HandlerLevels.ApplicationLevel });
            Assert.AreEqual(resp, null);

            X.GET(handlerUri, out resp, null, 0, new HandlerOptions() { HandlerLevel = HandlerOptions.HandlerLevels.ApplicationExtraLevel });
            Assert.AreEqual(null, resp);
        }

        [Test]
        public static void TestAppNameHandlers() {

            UriHandlersManager.ResetUriHandlersManagers();

            String handlerUri = "/HandlerNormal";
            
            Handle.GET(handlerUri, () => {
                Assert.AreEqual("nunit.core", StarcounterEnvironment.AppName);
                return handlerUri + "0";
            });

            Handle.GET(handlerUri, () => {
                Assert.AreEqual("nunit.core", StarcounterEnvironment.AppName);
                return handlerUri + "1";
            }, new HandlerOptions() { HandlerLevel = HandlerOptions.HandlerLevels.ApplicationLevel });

            Handle.GET(handlerUri, () => {
                Assert.AreEqual("nunit.core", StarcounterEnvironment.AppName);
                return handlerUri + "2";
            }, new HandlerOptions() { HandlerLevel = HandlerOptions.HandlerLevels.ApplicationExtraLevel });

            Response resp;
            X.GET(handlerUri, out resp);
            Assert.AreEqual(resp.Body, handlerUri + "0");

            X.GET(handlerUri, out resp, null, 0, new HandlerOptions() { HandlerLevel = HandlerOptions.HandlerLevels.ApplicationLevel });
            Assert.AreEqual(resp.Body, handlerUri + "1");

            X.GET(handlerUri, out resp, null, 0, new HandlerOptions() { HandlerLevel = HandlerOptions.HandlerLevels.ApplicationExtraLevel });
            Assert.AreEqual(resp.Body, handlerUri + "2");
        }
    }
}