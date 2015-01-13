using System;
using Starcounter.Internal.Web;
using NUnit.Framework;
using Starcounter.Advanced;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using Starcounter.Rest;
using Starcounter.Advanced.XSON;
using Starcounter.Templates;
using PolyjuiceNamespace;

namespace Starcounter.Internal.Tests
{
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

            // Not actually a merger anymore but linker of sibling Json parts.
            Handle.MergeResponses((Request req, List<Response> responses) => {

                var mainResponse = responses[0];
                Int32 mainResponseId = 0;

                // Searching for the current application in responses.
                /*for (Int32 i = 0; i < responses.Count; i++) {

                    if (responses[i].AppName == StarcounterEnvironment.AppName) {

                        mainResponse = responses[i];
                        mainResponseId = i;
                        break;
                    }
                }*/

                var json = mainResponse.Resource as Json;

                if ((json != null) && (mainResponse.AppName != StarcounterConstants.LauncherAppName)) {

                    json.SetAppName(mainResponse.AppName);

                    for (Int32 i = 0; i < responses.Count; i++) {

                        if (mainResponseId != i) {

                            ((Json)responses[i].Resource).SetAppName(responses[i].AppName);
                            json.AddStepSibling((Json)responses[i].Resource);
                        }
                    }
                }

                return mainResponse;
            });

            StarcounterEnvironment.AppName = "app1";
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class PolyjuiceTests
    {
        /// <summary>
        /// Creates an emulated Society Objects tree.
        /// </summary>
        static void InitSocietyObjects() {
            PolyjuiceNamespace.Polyjuice.Init();
        }

        /// <summary>
        /// 
        /// </summary>
        [Test]
        public static void SimplePolyjuiceTests() {

            InitSocietyObjects();

            var googleMapsTemplate = new TObject();
            googleMapsTemplate.Add<TString>("Longitude");
            googleMapsTemplate.Add<TString>("Latitude");
            googleMapsTemplate.Add<TString>("Html");

            dynamic googleMapsObj = new Json();
            googleMapsObj.Template = googleMapsTemplate;
            googleMapsObj.Longitude = "34";
            googleMapsObj.Latitude = "67";
            googleMapsObj.Html = "Map.html";

            var skypeUserTemplate = new TObject();
            skypeUserTemplate.Add<TString>("FirstName");
            skypeUserTemplate.Add<TString>("LastName");
            skypeUserTemplate.Add<TString>("Age");
            skypeUserTemplate.Add<TString>("Html");
            skypeUserTemplate.Add<TString>("Gender");

            dynamic skypeUserObj = new Json();
            skypeUserObj.Template = skypeUserTemplate;
            skypeUserObj.FirstName = "John";
            skypeUserObj.LastName = "Lennon";
            skypeUserObj.Age = "43";
            skypeUserObj.Gender = "Male";
            skypeUserObj.Html = "SkypeUser.html";

            var employeeTemplate = new TObject();
            employeeTemplate.Add<TString>("FullName");
            employeeTemplate.Add<TString>("Html");
            employeeTemplate.Add<TString>("Position");
            employeeTemplate.Add<TString>("Salary");

            dynamic salaryAppObj = new Json();
            salaryAppObj.Template = employeeTemplate;
            salaryAppObj.Position = "Director";
            salaryAppObj.FullName = "John Lennon";
            salaryAppObj.Salary = "43453";
            salaryAppObj.Html = "Employee.html";
            
            var facebookProfileTemplate = new TObject();
            facebookProfileTemplate.Add<TString>("FirstName");
            facebookProfileTemplate.Add<TString>("LastName");
            facebookProfileTemplate.Add<TString>("Age");
            facebookProfileTemplate.Add<TString>("Html");

            dynamic facebookProfileObj = new Json();
            facebookProfileObj.Template = facebookProfileTemplate;
            facebookProfileObj.FirstName = "John";
            facebookProfileObj.LastName = "Lennon";
            facebookProfileObj.Age = "43";
            facebookProfileObj.Html = "Profile.html";

            // Logging handler calls.
            Handle.GET("{?}", (String str) => {

                Console.WriteLine("Called handler " + str);

                return null;

            }, HandlerOptions.FilteringLevel);

            StarcounterEnvironment.AppName = "googlemap";

            Handle.GET("/googlemap/object/{?}", (String id) => {

                // TODO!
                ((Json)googleMapsObj)._stepSiblings = null;
                ((Json)googleMapsObj)._stepParent = null;

                return googleMapsObj;
            });

            StarcounterEnvironment.AppName = "skyper";

            Handle.GET("/skyper/skypeuser/{?}", (String id) => {

                // TODO!
                ((Json)skypeUserObj)._stepSiblings = null;
                ((Json)skypeUserObj)._stepParent = null;

                return skypeUserObj;
            });

            StarcounterEnvironment.AppName = "salaryapp";

            Handle.GET("/salaryapp/employee/{?}", (String id) => {

                // TODO!
                ((Json)salaryAppObj)._stepSiblings = null;
                ((Json)salaryAppObj)._stepParent = null;

                return salaryAppObj;
            });

            StarcounterEnvironment.AppName = "facebook";

            Handle.GET("/facebook/person/{?}", (String id) => {

                // TODO!
                ((Json)facebookProfileObj)._stepSiblings = null;
                ((Json)facebookProfileObj)._stepParent = null;

                return facebookProfileObj;
            });
            
            Polyjuice.Map("/googlemap/object/@w", "/so/something/@w",
                (String appObjectId) => { return appObjectId + "456"; },
                (String soObjectId) => { return soObjectId + "789"; });

            Polyjuice.Map("/salaryapp/employee/@w", "/so/person/@w",
                (String appObjectId) => { return appObjectId + "456"; },
                (String soObjectId) => { return soObjectId + "789"; });

            Polyjuice.Map("/skyper/skypeuser/@w", "/so/person/@w",
                (String appObjectId) => { return appObjectId + "456"; },
                (String soObjectId) => { return soObjectId + "789"; });

            Polyjuice.Map("/facebook/person/@w", "/so/person/@w",
                (String appObjectId) => { return appObjectId + "456"; },
                (String soObjectId) => { return soObjectId + "789"; });

            Response resp = null, resp1 = null, resp2 = null, resp3 = null, resp4 = null;

            StarcounterEnvironment.AppName = "app1";

            X.GET("/so/something/123", out resp1); // map only
            X.GET("/so/person/123", out resp2); // all
            X.GET("/so/legalentity/123", out resp3); // map only

            Assert.IsTrue(resp1.Body.Equals(resp3.Body));

            X.GET("/googlemap/object/123", out resp1); // map only
            X.GET("/salaryapp/employee/123", out resp2); // all
            X.GET("/skyper/skypeuser/123", out resp3); // all
            X.GET("/facebook/person/123", out resp4); // all

            Assert.IsTrue(resp2.Body.Equals(resp3.Body));
            Assert.IsTrue(resp3.Body.Equals(resp4.Body));
            Assert.IsFalse(resp1.Body.Equals(resp2.Body));
            
            X.GET("/facebook/person/123", out resp);
            X.GET("/so/person/123", out resp2);

            StarcounterEnvironment.AppName = "";
            Assert.IsTrue(((Json)resp.Resource).ToJson() == ((Json)resp4.Resource).ToJson());

            StarcounterEnvironment.AppName = "facebook";
            Assert.IsTrue(((Json)resp.Resource).ToJson() == facebookProfileObj.ToJson());
            Assert.IsTrue(((Json)resp2.Resource).ToJson() == facebookProfileObj.ToJson());

            StarcounterEnvironment.AppName = "salaryapp";
            Assert.IsTrue(((Json)resp.Resource).ToJson() == salaryAppObj.ToJson());
            Assert.IsTrue(((Json)resp2.Resource).ToJson() == salaryAppObj.ToJson());

            StarcounterEnvironment.AppName = "skyper";
            Assert.IsTrue(((Json)resp.Resource).ToJson() == skypeUserObj.ToJson());
            Assert.IsTrue(((Json)resp2.Resource).ToJson() == skypeUserObj.ToJson());

            StarcounterEnvironment.AppName = "googlemap";
            Assert.IsTrue(((Json)resp.Resource).ToJson() == googleMapsObj.ToJson());
            Assert.IsTrue(((Json)resp2.Resource).ToJson() == googleMapsObj.ToJson());

            X.GET("/googlemap/object/123", out resp);
            X.GET("/so/something/123", out resp2);
            X.GET("/so/legalentity/123", out resp3);

            StarcounterEnvironment.AppName = "facebook";
            Assert.IsTrue(((Json)resp.Resource).ToJson() == ((Json)resp1.Resource).ToJson());
            Assert.IsTrue(((Json)resp2.Resource).ToJson() == ((Json)resp1.Resource).ToJson());
            Assert.IsTrue(((Json)resp3.Resource).ToJson() == ((Json)resp1.Resource).ToJson());

            StarcounterEnvironment.AppName = "salaryapp";
            Assert.IsTrue(((Json)resp.Resource).ToJson() == ((Json)resp1.Resource).ToJson());
            Assert.IsTrue(((Json)resp2.Resource).ToJson() == ((Json)resp1.Resource).ToJson());
            Assert.IsTrue(((Json)resp3.Resource).ToJson() == ((Json)resp1.Resource).ToJson());

            StarcounterEnvironment.AppName = "skyper";
            Assert.IsTrue(((Json)resp.Resource).ToJson() == ((Json)resp1.Resource).ToJson());
            Assert.IsTrue(((Json)resp2.Resource).ToJson() == ((Json)resp1.Resource).ToJson());
            Assert.IsTrue(((Json)resp3.Resource).ToJson() == ((Json)resp1.Resource).ToJson());

            StarcounterEnvironment.AppName = "googlemap";
            Assert.IsTrue(((Json)resp.Resource).ToJson() == googleMapsObj.ToJson());
            Assert.IsTrue(((Json)resp2.Resource).ToJson() == googleMapsObj.ToJson());
            Assert.IsTrue(((Json)resp3.Resource).ToJson() == googleMapsObj.ToJson());

            StarcounterEnvironment.AppName = "";
            Assert.IsTrue(((Json)resp.Resource).ToJson() == ((Json)resp1.Resource).ToJson());
            Assert.IsTrue(((Json)resp2.Resource).ToJson() == ((Json)resp1.Resource).ToJson());
            Assert.IsTrue(((Json)resp3.Resource).ToJson() == ((Json)resp1.Resource).ToJson());

            X.GET("/salaryapp/employee/123", out resp);
            X.GET("/so/person/123", out resp2);

            StarcounterEnvironment.AppName = "";
            Assert.IsTrue(((Json)resp.Resource).ToJson() == ((Json)resp4.Resource).ToJson());
            Assert.IsTrue(((Json)resp2.Resource).ToJson() == ((Json)resp4.Resource).ToJson());

            StarcounterEnvironment.AppName = "facebook";
            Assert.IsTrue(((Json)resp.Resource).ToJson() == facebookProfileObj.ToJson());
            Assert.IsTrue(((Json)resp2.Resource).ToJson() == facebookProfileObj.ToJson());

            StarcounterEnvironment.AppName = "salaryapp";
            Assert.IsTrue(((Json)resp.Resource).ToJson() == salaryAppObj.ToJson());
            Assert.IsTrue(((Json)resp2.Resource).ToJson() == salaryAppObj.ToJson());

            StarcounterEnvironment.AppName = "skyper";
            Assert.IsTrue(((Json)resp.Resource).ToJson() == skypeUserObj.ToJson());
            Assert.IsTrue(((Json)resp2.Resource).ToJson() == skypeUserObj.ToJson());

            StarcounterEnvironment.AppName = "googlemap";
            Assert.IsTrue(((Json)resp.Resource).ToJson() == googleMapsObj.ToJson());
            Assert.IsTrue(((Json)resp2.Resource).ToJson() == googleMapsObj.ToJson());
        }
    }
}