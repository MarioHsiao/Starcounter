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

            const String FacebookAppName = "FacebookApp";
            const String GoogleMapsAppName = "GoogleMapsApp";
            const String SkypeAppName = "SkypeApp";
            const String SalaryAppName = "SalaryApp";
            const String SomeAppName = "SomeOtherApp";

            var googleMapsTemplate = new TObject();
            googleMapsTemplate.Add<TString>("Longitude", "34");
            googleMapsTemplate.Add<TString>("Latitude", "67");
            googleMapsTemplate.Add<TString>("Html", "Map.html");

            dynamic googleMapsObjRef = new Json();
            googleMapsObjRef.Template = googleMapsTemplate;

            var googleMapsWrappedTemplate = new TObject();
            googleMapsWrappedTemplate.Add<TObject>(GoogleMapsAppName);

            dynamic googleMapsObjRefWrapped = new Json();
            googleMapsObjRefWrapped.Template = googleMapsWrappedTemplate;
            googleMapsObjRefWrapped.GoogleMapsApp = googleMapsObjRef;

            var skypeUserTemplate = new TObject();
            skypeUserTemplate.Add<TString>("FirstName", "John");
            skypeUserTemplate.Add<TString>("LastName", "Lennon");
            skypeUserTemplate.Add<TString>("Age", "43");
            skypeUserTemplate.Add<TString>("Gender", "Male");
            skypeUserTemplate.Add<TString>("Html", "SkypeUser.html");

            dynamic skypeUserObjRef = new Json();
            skypeUserObjRef.Template = skypeUserTemplate;

            var skypeUserWrappedTemplate = new TObject();
            skypeUserWrappedTemplate.Add<TObject>(SkypeAppName);

            dynamic skypeUserObjRefWrapped = new Json();
            skypeUserObjRefWrapped.Template = skypeUserWrappedTemplate;
            skypeUserObjRefWrapped.SkypeApp = skypeUserObjRef;

            var salaryTemplate = new TObject();
            salaryTemplate.Add<TString>("FullName", "John Lennon");
            salaryTemplate.Add<TString>("Position", "Director");
            salaryTemplate.Add<TString>("Salary", "54321");
            salaryTemplate.Add<TString>("Html", "Employee.html");

            dynamic salaryAppObjRef = new Json();
            salaryAppObjRef.Template = salaryTemplate;

            var salaryWrappedTemplate = new TObject();
            salaryWrappedTemplate.Add<TObject>(SalaryAppName);

            dynamic salaryAppObjRefWrapped = new Json();
            salaryAppObjRefWrapped.Template = salaryWrappedTemplate;
            salaryAppObjRefWrapped.SalaryApp = salaryAppObjRef;
            
            var facebookProfileTemplate = new TObject();
            facebookProfileTemplate.Add<TString>("FirstName", "John");
            facebookProfileTemplate.Add<TString>("LastName", "Lennon");
            facebookProfileTemplate.Add<TString>("Age", "45");
            facebookProfileTemplate.Add<TString>("Html", "Profile.html");

            dynamic facebookProfileObjRef = new Json();
            facebookProfileObjRef.Template = facebookProfileTemplate;

            var facebookProfileWrappedTemplate = new TObject();
            facebookProfileWrappedTemplate.Add<TObject>(FacebookAppName);

            dynamic facebookProfileObjRefWrapped = new Json();
            facebookProfileObjRefWrapped.Template = facebookProfileWrappedTemplate;
            facebookProfileObjRefWrapped.FacebookApp = facebookProfileObjRef;

            var allWrappedTemplate = new TObject();
            allWrappedTemplate.Add<TObject>(SalaryAppName);
            allWrappedTemplate.Add<TObject>(SkypeAppName);
            allWrappedTemplate.Add<TObject>(FacebookAppName);
            allWrappedTemplate.Add<TObject>(GoogleMapsAppName);

            dynamic allObjRefWrapped = new Json();
            allObjRefWrapped.Template = allWrappedTemplate;
            allObjRefWrapped.SalaryApp = new Json() { Template = salaryTemplate };
            allObjRefWrapped.SkypeApp = new Json() { Template = skypeUserTemplate };
            allObjRefWrapped.FacebookApp = new Json() { Template = facebookProfileTemplate };
            allObjRefWrapped.GoogleMapsApp = new Json() { Template = googleMapsTemplate };

            // Logging handler calls.
            Handle.GET("{?}", (Request req, String str) => {

                Assert.IsTrue(str == req.Uri);

                Console.WriteLine("Called handler " + str);

                return null;

            }, HandlerOptions.FilteringLevel);
            
            StarcounterEnvironment.AppName = GoogleMapsAppName;

            Handle.GET("/GoogleMapsApp/object/{?}", (String id) => {

                dynamic o = new Json();
                o.Template = googleMapsTemplate;

                return o;
            });

            StarcounterEnvironment.AppName = SkypeAppName;

            Handle.GET("/SkypeApp/skypeuser/{?}", (String id) => {

                dynamic o = new Json();
                o.Template = skypeUserTemplate;

                return o;
            });
            
            StarcounterEnvironment.AppName = SalaryAppName;

            Handle.GET("/SalaryApp/employee/{?}", (String id) => {

                dynamic o = new Json();
                o.Template = salaryTemplate;

                return o;
            });

            StarcounterEnvironment.AppName = FacebookAppName;

            Handle.GET("/FacebookApp/person/{?}", (String id) => {

                dynamic o = new Json();
                o.Template = facebookProfileTemplate;

                return o;
            });

            StarcounterEnvironment.AppName = SomeAppName;

            Response resp = null;

            // Testing wrapped application outputs.

            X.GET("/GoogleMapsApp/object/12345", out resp);
            Assert.IsTrue(((Json)resp.Resource).ToJson() == googleMapsObjRefWrapped.ToJson());

            X.GET("/SkypeApp/skypeuser/12345", out resp);
            Assert.IsTrue(((Json)resp.Resource).ToJson() == skypeUserObjRefWrapped.ToJson());

            X.GET("/SalaryApp/employee/12345", out resp);
            Assert.IsTrue(((Json)resp.Resource).ToJson() == salaryAppObjRefWrapped.ToJson());

            X.GET("/FacebookApp/person/12345", out resp);
            Assert.IsTrue(((Json)resp.Resource).ToJson() == facebookProfileObjRefWrapped.ToJson());
            
            // Hierarchy: something -> legalentity -> person.

            Polyjuice.Map("/GoogleMapsApp/object/@w", "/so/something/@w",
                (String appObjectId) => { return appObjectId + "456"; },
                (String soObjectId) => { return soObjectId + "789"; });

            Polyjuice.Map("/SalaryApp/employee/@w", "/so/person/@w",
                (String appObjectId) => { return appObjectId + "456"; },
                (String soObjectId) => { return soObjectId + "789"; });

            Polyjuice.Map("/SkypeApp/skypeuser/@w", "/so/person/@w",
                (String appObjectId) => { return appObjectId + "456"; },
                (String soObjectId) => { return soObjectId + "789"; });

            Polyjuice.Map("/FacebookApp/person/@w", "/so/person/@w",
                (String appObjectId) => { return appObjectId + "456"; },
                (String soObjectId) => { return soObjectId + "789"; });

            Response resp1 = null, resp2 = null, resp3 = null, resp4 = null;

            X.GET("/so/something/123", out resp1); // google maps only
            Assert.IsTrue(((Json)resp1.Resource).ToJson() == googleMapsObjRefWrapped.ToJson());

            X.GET("/so/legalentity/123", out resp3); // map only
            Assert.IsTrue(((Json)resp3.Resource).ToJson() == googleMapsObjRefWrapped.ToJson());

            X.GET("/so/person/123", out resp2); // all
            Assert.IsTrue(((Json)resp2.Resource).ToJson() == allObjRefWrapped.ToJson());

            Assert.IsTrue(resp1.Body.Equals(resp3.Body));

            X.GET("/GoogleMapsApp/object/123", out resp1); // map only
            X.GET("/SalaryApp/employee/123", out resp2); // all
            X.GET("/SkypeApp/skypeuser/123", out resp3); // all
            X.GET("/FacebookApp/person/123", out resp4); // all

            Assert.IsTrue(resp2.Body.Equals(resp3.Body));
            Assert.IsTrue(resp3.Body.Equals(resp4.Body));
            Assert.IsFalse(resp1.Body.Equals(resp2.Body));
            
            X.GET("/FacebookApp/person/123", out resp);
            X.GET("/so/person/123", out resp2);

            StarcounterEnvironment.AppName = "";
            Assert.IsTrue(((Json)resp.Resource).ToJson() == ((Json)resp4.Resource).ToJson());

            StarcounterEnvironment.AppName = FacebookAppName;
            Assert.IsTrue(((Json)resp.Resource).ToJson() == facebookProfileObjRef.ToJson());
            Assert.IsTrue(((Json)resp2.Resource).ToJson() == facebookProfileObjRef.ToJson());

            StarcounterEnvironment.AppName = SalaryAppName;
            Assert.IsTrue(((Json)resp.Resource).ToJson() == salaryAppObjRef.ToJson());
            Assert.IsTrue(((Json)resp2.Resource).ToJson() == salaryAppObjRef.ToJson());

            StarcounterEnvironment.AppName = SkypeAppName;
            Assert.IsTrue(((Json)resp.Resource).ToJson() == skypeUserObjRef.ToJson());
            Assert.IsTrue(((Json)resp2.Resource).ToJson() == skypeUserObjRef.ToJson());

            StarcounterEnvironment.AppName = GoogleMapsAppName;
            Assert.IsTrue(((Json)resp.Resource).ToJson() == googleMapsObjRef.ToJson());
            Assert.IsTrue(((Json)resp2.Resource).ToJson() == googleMapsObjRef.ToJson());

            X.GET("/GoogleMapsApp/object/123", out resp);
            X.GET("/so/something/123", out resp2);
            X.GET("/so/legalentity/123", out resp3);

            StarcounterEnvironment.AppName = FacebookAppName;
            Assert.IsTrue(((Json)resp.Resource).ToJson() == ((Json)resp1.Resource).ToJson());
            Assert.IsTrue(((Json)resp2.Resource).ToJson() == ((Json)resp1.Resource).ToJson());
            Assert.IsTrue(((Json)resp3.Resource).ToJson() == ((Json)resp1.Resource).ToJson());

            StarcounterEnvironment.AppName = SalaryAppName;
            Assert.IsTrue(((Json)resp.Resource).ToJson() == ((Json)resp1.Resource).ToJson());
            Assert.IsTrue(((Json)resp2.Resource).ToJson() == ((Json)resp1.Resource).ToJson());
            Assert.IsTrue(((Json)resp3.Resource).ToJson() == ((Json)resp1.Resource).ToJson());

            StarcounterEnvironment.AppName = SkypeAppName;
            Assert.IsTrue(((Json)resp.Resource).ToJson() == ((Json)resp1.Resource).ToJson());
            Assert.IsTrue(((Json)resp2.Resource).ToJson() == ((Json)resp1.Resource).ToJson());
            Assert.IsTrue(((Json)resp3.Resource).ToJson() == ((Json)resp1.Resource).ToJson());

            StarcounterEnvironment.AppName = GoogleMapsAppName;
            Assert.IsTrue(((Json)resp.Resource).ToJson() == googleMapsObjRef.ToJson());
            Assert.IsTrue(((Json)resp2.Resource).ToJson() == googleMapsObjRef.ToJson());
            Assert.IsTrue(((Json)resp3.Resource).ToJson() == googleMapsObjRef.ToJson());

            StarcounterEnvironment.AppName = "";
            Assert.IsTrue(((Json)resp.Resource).ToJson() == ((Json)resp1.Resource).ToJson());
            Assert.IsTrue(((Json)resp2.Resource).ToJson() == ((Json)resp1.Resource).ToJson());
            Assert.IsTrue(((Json)resp3.Resource).ToJson() == ((Json)resp1.Resource).ToJson());

            X.GET("/SalaryApp/employee/123", out resp);
            X.GET("/so/person/123", out resp2);

            StarcounterEnvironment.AppName = "";
            Assert.IsTrue(((Json)resp.Resource).ToJson() == ((Json)resp4.Resource).ToJson());
            Assert.IsTrue(((Json)resp2.Resource).ToJson() == ((Json)resp4.Resource).ToJson());

            StarcounterEnvironment.AppName = FacebookAppName;
            Assert.IsTrue(((Json)resp.Resource).ToJson() == facebookProfileObjRef.ToJson());
            Assert.IsTrue(((Json)resp2.Resource).ToJson() == facebookProfileObjRef.ToJson());

            StarcounterEnvironment.AppName = SalaryAppName;
            Assert.IsTrue(((Json)resp.Resource).ToJson() == salaryAppObjRef.ToJson());
            Assert.IsTrue(((Json)resp2.Resource).ToJson() == salaryAppObjRef.ToJson());

            StarcounterEnvironment.AppName = SkypeAppName;
            Assert.IsTrue(((Json)resp.Resource).ToJson() == skypeUserObjRef.ToJson());
            Assert.IsTrue(((Json)resp2.Resource).ToJson() == skypeUserObjRef.ToJson());

            StarcounterEnvironment.AppName = GoogleMapsAppName;
            Assert.IsTrue(((Json)resp.Resource).ToJson() == googleMapsObjRef.ToJson());
            Assert.IsTrue(((Json)resp2.Resource).ToJson() == googleMapsObjRef.ToJson());
        }
    }
}