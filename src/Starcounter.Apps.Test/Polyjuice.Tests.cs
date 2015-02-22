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
            Response.ResponsesMergerRoutine_ = Polyjuice.DefaultMerger;
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
        //[Test]
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

            dynamic googleMapsWrappedRef = new Json();
            googleMapsWrappedRef.Template = googleMapsWrappedTemplate;
            googleMapsWrappedRef.GoogleMapsApp = googleMapsObjRef;

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

            dynamic skypeUserObjWrappedRef = new Json();
            skypeUserObjWrappedRef.Template = skypeUserWrappedTemplate;
            skypeUserObjWrappedRef.SkypeApp = skypeUserObjRef;

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

            dynamic facebookProfileObjWrappedRef = new Json();
            facebookProfileObjWrappedRef.Template = facebookProfileWrappedTemplate;
            facebookProfileObjWrappedRef.FacebookApp = facebookProfileObjRef;

            var allWrappedTemplate = new TObject();
            allWrappedTemplate.Add<TObject>(SalaryAppName);
            allWrappedTemplate.Add<TObject>(SkypeAppName);
            allWrappedTemplate.Add<TObject>(FacebookAppName);
            allWrappedTemplate.Add<TObject>(GoogleMapsAppName);

            dynamic allObjWrappedRef = new Json();
            allObjWrappedRef.Template = allWrappedTemplate;
            allObjWrappedRef.SalaryApp = new Json() { Template = salaryTemplate };
            allObjWrappedRef.SkypeApp = new Json() { Template = skypeUserTemplate };
            allObjWrappedRef.FacebookApp = new Json() { Template = facebookProfileTemplate };
            allObjWrappedRef.GoogleMapsApp = new Json() { Template = googleMapsTemplate };

            // Logging all handler calls.
            Handle.GET("{?}", (Request req, String str) => {

                Assert.IsTrue(str == req.Uri);

                Console.WriteLine("Called handler " + str);

                return null;

            }, new HandlerOptions() {
                HandlerLevel = HandlerOptions.HandlerLevels.FilteringLevel
            });
            
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

            X.GET("/GoogleMapsApp/object/12345", out resp); // maps wrapped
            Assert.IsTrue(((Json)resp.Resource).ToJson() == googleMapsWrappedRef.ToJson());
            Assert.IsTrue(googleMapsTemplate == ((Json)resp.Resource).Template);

            X.GET("/SkypeApp/skypeuser/12345", out resp); // skype wrapped
            Assert.IsTrue(((Json)resp.Resource).ToJson() == skypeUserObjWrappedRef.ToJson());
            Assert.IsTrue(skypeUserTemplate == ((Json)resp.Resource).Template);

            X.GET("/SalaryApp/employee/12345", out resp); // salary wrapped
            Assert.IsTrue(((Json)resp.Resource).ToJson() == salaryAppObjRefWrapped.ToJson());
            Assert.IsTrue(salaryTemplate == ((Json)resp.Resource).Template);

            X.GET("/FacebookApp/person/12345", out resp); // facebook wrapped
            Assert.IsTrue(((Json)resp.Resource).ToJson() == facebookProfileObjWrappedRef.ToJson());
            Assert.IsTrue(facebookProfileTemplate == ((Json)resp.Resource).Template);
            
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

            X.GET("/so/something/123", out resp1); // maps only
            Assert.IsTrue(googleMapsTemplate == ((Json)resp1.Resource).Template);
            Assert.IsTrue(((Json)resp1.Resource).ToJson() == googleMapsWrappedRef.ToJson());

            X.GET("/so/legalentity/123", out resp3); // maps only
            Assert.IsTrue(((Json)resp3.Resource).ToJson() == googleMapsWrappedRef.ToJson());
            Assert.IsTrue(googleMapsTemplate == ((Json)resp3.Resource).Template);

            X.GET("/so/person/123", out resp2); // all
            Assert.IsTrue(((Json)resp2.Resource).ToJson() == allObjWrappedRef.ToJson());

            Assert.IsTrue(resp1.Body.Equals(resp3.Body));

            X.GET("/GoogleMapsApp/object/123", out resp1); // maps only
            Assert.IsTrue(((Json)resp1.Resource).ToJson() == googleMapsWrappedRef.ToJson());
            Assert.IsTrue(googleMapsTemplate == ((Json)resp1.Resource).Template);

            X.GET("/SalaryApp/employee/123", out resp2); // all
            Assert.IsTrue(((Json)resp2.Resource).ToJson() == allObjWrappedRef.ToJson());

            X.GET("/SkypeApp/skypeuser/123", out resp3); // all
            Assert.IsTrue(((Json)resp3.Resource).ToJson() == allObjWrappedRef.ToJson());

            X.GET("/FacebookApp/person/123", out resp4); // all
            Assert.IsTrue(((Json)resp4.Resource).ToJson() == allObjWrappedRef.ToJson());

            Assert.IsTrue(resp2.Body.Equals(resp3.Body));
            Assert.IsTrue(resp3.Body.Equals(resp4.Body));
            Assert.IsFalse(resp1.Body.Equals(resp2.Body));

            StarcounterEnvironment.AppName = FacebookAppName;

            X.GET("/FacebookApp/person/123", out resp); // facebook
            Assert.IsTrue(((Json)resp.Resource).ToJson() == facebookProfileObjRef.ToJson());
            Assert.IsTrue(facebookProfileTemplate == ((Json)resp.Resource).Template);

            StarcounterEnvironment.AppName = SomeAppName;

            X.GET("/FacebookApp/person/123", out resp); // all
            Assert.IsTrue(((Json)resp2.Resource).ToJson() == allObjWrappedRef.ToJson());

            X.GET("/so/person/123", out resp2); // all
            Assert.IsTrue(((Json)resp2.Resource).ToJson() == allObjWrappedRef.ToJson());

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

            StarcounterEnvironment.AppName = SomeAppName;

            X.GET("/GoogleMapsApp/object/123", out resp); // maps only
            X.GET("/so/something/123", out resp2); // maps only
            X.GET("/so/legalentity/123", out resp3); // maps only

            Assert.IsTrue(resp.Body.Equals(resp2.Body));
            Assert.IsTrue(resp3.Body.Equals(resp2.Body));
            Assert.IsTrue(((Json)resp2.Resource).ToJson() == googleMapsWrappedRef.ToJson());

            StarcounterEnvironment.AppName = FacebookAppName;
            Assert.IsTrue(((Json)resp.Resource).ToJson() == googleMapsWrappedRef.ToJson());
            Assert.IsTrue(((Json)resp2.Resource).ToJson() == googleMapsWrappedRef.ToJson());
            Assert.IsTrue(((Json)resp3.Resource).ToJson() == googleMapsWrappedRef.ToJson());

            StarcounterEnvironment.AppName = SalaryAppName;
            Assert.IsTrue(((Json)resp.Resource).ToJson() == googleMapsWrappedRef.ToJson());
            Assert.IsTrue(((Json)resp2.Resource).ToJson() == googleMapsWrappedRef.ToJson());
            Assert.IsTrue(((Json)resp3.Resource).ToJson() == googleMapsWrappedRef.ToJson());

            StarcounterEnvironment.AppName = SkypeAppName;
            Assert.IsTrue(((Json)resp.Resource).ToJson() == googleMapsWrappedRef.ToJson());
            Assert.IsTrue(((Json)resp2.Resource).ToJson() == googleMapsWrappedRef.ToJson());
            Assert.IsTrue(((Json)resp3.Resource).ToJson() == googleMapsWrappedRef.ToJson());

            StarcounterEnvironment.AppName = GoogleMapsAppName;
            Assert.IsTrue(((Json)resp.Resource).ToJson() == googleMapsObjRef.ToJson());
            Assert.IsTrue(((Json)resp2.Resource).ToJson() == googleMapsObjRef.ToJson());
            Assert.IsTrue(((Json)resp3.Resource).ToJson() == googleMapsObjRef.ToJson());

            StarcounterEnvironment.AppName = SomeAppName;
            Assert.IsTrue(((Json)resp.Resource).ToJson() == googleMapsWrappedRef.ToJson());
            Assert.IsTrue(((Json)resp2.Resource).ToJson() == googleMapsWrappedRef.ToJson());
            Assert.IsTrue(((Json)resp3.Resource).ToJson() == googleMapsWrappedRef.ToJson());

            X.GET("/SalaryApp/employee/123", out resp); // all
            X.GET("/so/person/123", out resp2); // all

            Assert.IsTrue(resp.Body.Equals(resp2.Body));

            StarcounterEnvironment.AppName = SomeAppName;
            Assert.IsTrue(((Json)resp.Resource).ToJson() == allObjWrappedRef.ToJson());
            Assert.IsTrue(((Json)resp2.Resource).ToJson() == allObjWrappedRef.ToJson());

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