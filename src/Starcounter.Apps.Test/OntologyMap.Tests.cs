﻿using System;
using Starcounter.Internal.Web;
using NUnit.Framework;
using Starcounter.Advanced;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using Starcounter.Rest;
using Starcounter.Advanced.XSON;
using Starcounter.Templates;
using System.Collections.Concurrent;

namespace Starcounter.Internal.Tests
{
    /// <summary>
    /// 
    /// </summary>
    public class MappingTests
    {
        /// <summary>
        /// Testing ontology maps.
        /// </summary>
        //[Test]
        public static void MappingSeveralHandlersFromSameAppTest() {

            var googleMapsTemplate = new TObject();
            googleMapsTemplate.Add<TString>("Longitude", "34");
            googleMapsTemplate.Add<TString>("Latitude", "67");
            googleMapsTemplate.Add<TString>("Html", "Map.html");

            dynamic o1 = new Json();
            o1.Template = googleMapsTemplate;

            dynamic o2 = new Json();
            o2.Template = googleMapsTemplate;

            Handle.GET("/GoogleMapsApp/object1/{?}", (String id) => {
                return o1;
            });

            Handle.GET("/GoogleMapsApp/object2/{?}", (String id) => {
                return o2;
            });

            UriMapping.Map("/GoogleMapsApp/object1/{?}", "/sc/mapping/gmap/{?}");
            Response resp = Self.GET("/sc/mapping/gmap/1");
            Assert.IsTrue(o1 == resp.Resource);

            UriMapping.Map("/GoogleMapsApp/object2/{?}", "/sc/mapping/gmap/{?}");

            resp = Self.GET("/sc/mapping/gmap/1");
            Assert.IsTrue(o1 == resp.Resource);

            resp = Self.GET("/GoogleMapsApp/object2/1");
            Assert.IsTrue(o2 == resp.Resource);
        }

        /// <summary>
        /// Testing ontology maps.
        /// </summary>
            //[Test]
        public static void SimpleMappingTests() {

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

            UriMapping.Init();
            
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

            Json json = null;

            // Simulating external call.
            StarcounterEnvironment.AppName = null;

            json = Self.GET<Json>("/GoogleMapsApp/object/12345"); // maps

            Assert.IsTrue(json.ToJson() == googleMapsWrappedRef.ToJson());
            Assert.IsTrue(googleMapsTemplate == json.Template);

            json = Self.GET<Json>("/SkypeApp/skypeuser/12345"); // skype

            Assert.IsTrue(json.ToJson() == skypeUserObjWrappedRef.ToJson());
            Assert.IsTrue(skypeUserTemplate == json.Template);

            json = Self.GET<Json>("/SalaryApp/employee/12345"); // salary

            Assert.IsTrue(json.ToJson() == salaryAppObjRefWrapped.ToJson());
            Assert.IsTrue(salaryTemplate == json.Template);

            json = Self.GET<Json>("/FacebookApp/person/12345"); // facebook

            Assert.IsTrue(json.ToJson() == facebookProfileObjWrappedRef.ToJson());
            Assert.IsTrue(facebookProfileTemplate == json.Template);

            Page page = new Page() {
                Html = "/my.html"
            };

            json = Self.GET<Json>("/GoogleMapsApp/object/12345", () => {
                return page;
            }); // page

            Assert.IsTrue(json == page);
            Assert.IsTrue(json.ToJson() == page.ToJson());

            // Testing wrapped application outputs.
            StarcounterEnvironment.AppName = SkypeAppName;

            json = Self.GET<Json>("/GoogleMapsApp/object/12345"); // null
            Assert.IsTrue(json == null);

            json = Self.GET<Json>("/GoogleMapsApp/object/12345", () => {
                return page;
            }); // page

            Assert.IsTrue(json == page);
            Assert.IsTrue(json.ToJson() == page.ToJson());

            json = Self.GET<Json>("/SkypeApp/skypeuser/12345"); // skype

            Assert.IsTrue(json.ToJson() == skypeUserObjRef.ToJson());
            Assert.IsTrue(skypeUserTemplate == json.Template);

            // Hierarchy: something -> legalentity -> person.

            // Setting no application just to be able to map.
            StarcounterEnvironment.AppName = null;

            UriMapping.OntologyMap("/GoogleMapsApp/object/@w", "/so/something/@w",
                (String appObjectId) => { return appObjectId + "456"; },
                (String soObjectId) => { return soObjectId + "789"; });

            UriMapping.OntologyMap("/SalaryApp/employee/@w", "/so/person/@w",
                (String appObjectId) => { return appObjectId + "456"; },
                (String soObjectId) => { return soObjectId + "789"; });

            UriMapping.OntologyMap("/SkypeApp/skypeuser/@w", "/so/person/@w",
                (String appObjectId) => { return appObjectId + "456"; },
                (String soObjectId) => { return soObjectId + "789"; });

            UriMapping.OntologyMap("/FacebookApp/person/@w", "/so/person/@w",
                (String appObjectId) => { return appObjectId + "456"; },
                (String soObjectId) => { return soObjectId + "789"; });

            Json json1 = Self.GET("/so/something/123"); // maps
            Assert.IsTrue(googleMapsTemplate == json1.Template);
            Assert.IsTrue(json1.ToJson() == googleMapsWrappedRef.ToJson());

            Json json3 = Self.GET("/so/legalentity/123"); // maps
            Assert.IsTrue(json3.ToJson() == googleMapsWrappedRef.ToJson());
            Assert.IsTrue(googleMapsTemplate == json3.Template);

            json = Self.GET<Json>("/GoogleMapsApp/object/12345", () => {
                return page;
            }); // page

            Assert.IsTrue(json == page);
            Assert.IsTrue(json.ToJson() == page.ToJson());

            Json json2 = Self.GET("/so/person/123"); // all
            Assert.IsTrue(json2.ToJson() == allObjWrappedRef.ToJson());

            Assert.IsTrue(json1.Template == json3.Template);

            json1 = Self.GET("/GoogleMapsApp/object/123"); // maps
            Assert.IsTrue(json1.ToJson() == googleMapsWrappedRef.ToJson());
            Assert.IsTrue(googleMapsTemplate == json1.Template);

            json2 = Self.GET("/SalaryApp/employee/123"); // all
            Assert.IsTrue(json2.ToJson() == allObjWrappedRef.ToJson());

            json3 = Self.GET("/SkypeApp/skypeuser/123"); // all
            Assert.IsTrue(json3.ToJson() == allObjWrappedRef.ToJson());

            Json json4 = Self.GET("/FacebookApp/person/123"); // all
            Assert.IsTrue(json4.ToJson() == allObjWrappedRef.ToJson());

            Assert.IsTrue(json2.ToJson() == json3.ToJson());
            Assert.IsTrue(json3.ToJson() == json4.ToJson());
            Assert.IsTrue(json1.ToJson() != json2.ToJson());

            StarcounterEnvironment.AppName = FacebookAppName;

            json = Self.GET("/FacebookApp/person/123"); // facebook
            Assert.IsTrue(json.ToJson() == facebookProfileObjRef.ToJson());
            Assert.IsTrue(facebookProfileTemplate == json.Template);

            StarcounterEnvironment.AppName = null;

            json1 = Self.GET<Json>("/FacebookApp/person/123"); // all
            Assert.IsTrue(json1.ToJson() == allObjWrappedRef.ToJson());

            json3 = Self.GET<Json>("/FacebookApp/person/123", () => {
                return page;
            }); // page

            Assert.IsTrue(json3 == page);

            json2 = Self.GET("/so/person/123"); // all
            Assert.IsTrue(json2.ToJson() == allObjWrappedRef.ToJson());

            StarcounterEnvironment.AppName = SomeAppName;

            json = Self.GET<Json>("/GoogleMapsApp/object/123"); // maps
            json2 = Self.GET<Json>("/so/something/123"); // maps
            json3 = Self.GET<Json>("/so/legalentity/123"); // maps

            Assert.IsTrue(null == json);
            Assert.IsTrue(null == json2);
            Assert.IsTrue(null == json3);

            json = Self.GET<Json>("/GoogleMapsApp/object/123", () => {
                return page;
            }); // page

            json2 = Self.GET<Json>("/so/something/123", () => {
                return page;
            }); // page

            json3 = Self.GET<Json>("/so/legalentity/123", () => {
                return page;
            }); // page

            Assert.IsTrue(json == page);
            Assert.IsTrue(json2 == page);
            Assert.IsTrue(json3 == page);

            Assert.IsTrue(json.ToJson() == page.ToJson());
            Assert.IsTrue(json2.ToJson() == page.ToJson());
            Assert.IsTrue(json3.ToJson() == page.ToJson());

            StarcounterEnvironment.AppName = GoogleMapsAppName;

            json1 = Self.GET<Json>("/GoogleMapsApp/object/123"); // maps
            json2 = Self.GET<Json>("/so/something/123"); // maps
            json3 = Self.GET<Json>("/so/legalentity/123"); // maps
            json = Self.GET<Json>("/FacebookApp/person/123"); // maps
            json4 = Self.GET<Json>("/so/person/123"); // maps

            Assert.IsTrue(json2.ToJson() == json1.ToJson());
            Assert.IsTrue(json3.ToJson() == json1.ToJson());
            Assert.IsTrue(json4.ToJson() == json1.ToJson());
            Assert.IsTrue(json.ToJson() == json1.ToJson());
        }

        /// <summary>
        /// Testing ordinary maps.
        /// </summary>
        //[Test]
        public static void OrdinaryMapsTests() {

            UriMapping.Init();

            StarcounterEnvironment.AppName = "SomeApp";

            //////////////////////////////////////
            // Testing with GET.
            //////////////////////////////////////

            Handle.GET("/SomeApp/map1", (Request req) => {

                Assert.IsTrue("/SomeApp/map1" == req.Uri);
                Assert.IsTrue("GET" == req.Method);

                return "/map1";
            });

            Handle.GET("/SomeApp/map2", (Request req) => {

                Assert.IsTrue("/SomeApp/map2" == req.Uri);
                Assert.IsTrue("GET" == req.Method);

                return "/map2";
            });

            Handle.GET("/SomeApp/map3", (Request req) => {

                Assert.IsTrue("/SomeApp/map3" == req.Uri);
                Assert.IsTrue("GET" == req.Method);

                return "/map3";
            });

            UriMapping.Map("/SomeApp/map1", "/sc/mapping/mapped");
            UriMapping.Map("/SomeApp/map2", "/sc/mapping/mapped");
            UriMapping.Map("/SomeApp/map3", "/sc/mapping/mapped");

            String r = Self.GET<String>("/SomeApp/map1");
            Assert.IsTrue("/map1" == r);

            r = Self.GET<String>("/SomeApp/map2");
            Assert.IsTrue("/map1" == r);

            r = Self.GET<String>("/SomeApp/map3");
            Assert.IsTrue("/map1" == r);

            r = Self.GET<String>("/sc/mapping/mapped");
            Assert.IsTrue("/map1" == r);

            //////////////////////////////////////
            // Testing with POST.
            //////////////////////////////////////

            String body = "Here is my cool body!!!";

            Handle.POST("/SomeApp/map1", (Request req) => {

                Assert.IsTrue("/SomeApp/map1" == req.Uri);
                Assert.IsTrue(body == req.Body);
                Assert.IsTrue("POST" == req.Method);

                return "/map1";
            });

            Handle.POST("/SomeApp/map2", (Request req) => {

                Assert.IsTrue("/SomeApp/map2" == req.Uri);
                Assert.IsTrue(body == req.Body);
                Assert.IsTrue("POST" == req.Method);

                return "/map2";
            });

            Handle.POST("/SomeApp/map3", (Request req) => {

                Assert.IsTrue("/SomeApp/map3" == req.Uri);
                Assert.IsTrue(body == req.Body);
                Assert.IsTrue("POST" == req.Method);

                return "/map3";
            });

            UriMapping.Map("/SomeApp/map1", "/sc/mapping/mapped", "POST");
            UriMapping.Map("/SomeApp/map2", "/sc/mapping/mapped", "POST");
            UriMapping.Map("/SomeApp/map3", "/sc/mapping/mapped", "POST");

            Response resp = Self.POST("/SomeApp/map1", body, null);
            Assert.IsTrue("/map1" == resp.Body);

            resp = Self.POST("/SomeApp/map2", body, null);
            Assert.IsTrue("/map1" == resp.Body);

            resp = Self.POST("/SomeApp/map3", body, null);
            Assert.IsTrue("/map1" == resp.Body);

            resp = Self.POST("/sc/mapping/mapped", body, null);
            Assert.IsTrue("/map1" == resp.Body);

            //////////////////////////////////////
            // Testing with last parameter.
            //////////////////////////////////////
            
            String param = "12345";

            Handle.GET("/SomeApp/map1/{?}", (Request req, String p) => {

                Assert.IsTrue(param == p);
                Assert.IsTrue("/SomeApp/map1/" + param == req.Uri);

                return "/map1/" + p;
            });

            Handle.GET("/SomeApp/map2/{?}", (Request req, String p) => {

                Assert.IsTrue(param == p);
                Assert.IsTrue("/SomeApp/map2/" + param == req.Uri);

                return "/map2/" + p;
            });

            Handle.GET("/SomeApp/map3/{?}", (Request req, String p) => {

                Assert.IsTrue(param == p);
                Assert.IsTrue("/SomeApp/map3/" + param == req.Uri);
                
                return "/map3/" + p;
            });

            UriMapping.Map("/SomeApp/map1/@w", "/sc/mapping/mapped/@w");
            UriMapping.Map("/SomeApp/map2/@w", "/sc/mapping/mapped/@w");
            UriMapping.Map("/SomeApp/map3/@w", "/sc/mapping/mapped/@w");

            r = Self.GET<String>("/SomeApp/map1/" + param);
            Assert.IsTrue("/map1/" + param == r);

            r = Self.GET<String>("/SomeApp/map2/" + param);
            Assert.IsTrue("/map1/" + param == r);

            r = Self.GET<String>("/SomeApp/map3/" + param);
            Assert.IsTrue("/map1/" + param == r);

            r = Self.GET<String>("/sc/mapping/mapped/" + param);
            Assert.IsTrue("/map1/" + param == r);

            //////////////////////////////////////
            // Testing with POST and parameter.
            //////////////////////////////////////

            Handle.POST("/SomeApp/map1/{?}", (Request req, String p) => {
                
                Assert.IsTrue("/SomeApp/map1/" + param == req.Uri);
                Assert.IsTrue(body == req.Body);
                Assert.IsTrue(param == p);
                Assert.IsTrue("POST" == req.Method);
                
                return "/map1/" + p;
            });

            Handle.POST("/SomeApp/map2/{?}", (Request req, String p) => {

                Assert.IsTrue("/SomeApp/map2/" + param == req.Uri);
                Assert.IsTrue(body == req.Body);
                Assert.IsTrue(param == p);
                Assert.IsTrue("POST" == req.Method);

                return "/map2/" + p;
            });

            Handle.POST("/SomeApp/map3/{?}", (Request req, String p) => {

                Assert.IsTrue("/SomeApp/map3/" + param == req.Uri);
                Assert.IsTrue(body == req.Body);
                Assert.IsTrue(param == p);
                Assert.IsTrue("POST" == req.Method);

                return "/map3/" + p;
            });

            UriMapping.Map("/SomeApp/map1/@w", "/sc/mapping/mapped/@w", "POST");
            UriMapping.Map("/SomeApp/map2/@w", "/sc/mapping/mapped/@w", "POST");
            UriMapping.Map("/SomeApp/map3/@w", "/sc/mapping/mapped/@w", "POST");

            resp = Self.POST("/SomeApp/map1/" + param, body, null);
            Assert.IsTrue("/map1/" + param == resp.Body);

            resp = Self.POST("/SomeApp/map2/" + param, body, null);
            Assert.IsTrue("/map1/" + param == resp.Body);

            resp = Self.POST("/SomeApp/map3/" + param, body, null);
            Assert.IsTrue("/map1/" + param == resp.Body);

            resp = Self.POST("/sc/mapping/mapped/" + param, body, null);
            Assert.IsTrue("/map1/" + param == resp.Body);
        }
    }
}