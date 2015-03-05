using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Internal.Tests
{
    [TestFixture]
    class NodeRelatedTests
    {
        [Test]
        public static void TestEndpointExtractor()
        {
            String endpoint, relativeUri;

            X.GetEndpointFromUri("endpoint", out endpoint, out relativeUri);
            Assert.IsTrue("endpoint" == endpoint);
            Assert.IsTrue("/" == relativeUri);

            X.GetEndpointFromUri("endpoint/", out endpoint, out relativeUri);
            Assert.IsTrue("endpoint" == endpoint);
            Assert.IsTrue("/" == relativeUri);

            X.GetEndpointFromUri("http://endpoint/", out endpoint, out relativeUri);
            Assert.IsTrue("endpoint" == endpoint);
            Assert.IsTrue("/" == relativeUri);

            X.GetEndpointFromUri("http://endpoint", out endpoint, out relativeUri);
            Assert.IsTrue("endpoint" == endpoint);
            Assert.IsTrue("/" == relativeUri);

            X.GetEndpointFromUri("http://endpoint/", out endpoint, out relativeUri);
            Assert.IsTrue("endpoint" == endpoint);
            Assert.IsTrue("/" == relativeUri);

            X.GetEndpointFromUri("http://endpoint/a", out endpoint, out relativeUri);
            Assert.IsTrue("endpoint" == endpoint);
            Assert.IsTrue("/a" == relativeUri);

            X.GetEndpointFromUri("http://endpoint/a/", out endpoint, out relativeUri);
            Assert.IsTrue("endpoint" == endpoint);
            Assert.IsTrue("/a/" == relativeUri);

            X.GetEndpointFromUri("http://www.starcounter.com", out endpoint, out relativeUri);
            Assert.IsTrue("www.starcounter.com" == endpoint);
            Assert.IsTrue("/" == relativeUri);

            X.GetEndpointFromUri("ws://www.starcounter.com", out endpoint, out relativeUri);
            Assert.IsTrue("www.starcounter.com" == endpoint);
            Assert.IsTrue("/" == relativeUri);

            X.GetEndpointFromUri("file://scbuildserver/FTP/SCDev/BuildSystem/Logs/StatisticsEngine/default.htm", out endpoint, out relativeUri);
            Assert.IsTrue("scbuildserver" == endpoint);
            Assert.IsTrue("/FTP/SCDev/BuildSystem/Logs/StatisticsEngine/default.htm" == relativeUri);

            X.GetEndpointFromUri("/emails", out endpoint, out relativeUri);
            Assert.IsTrue("127.0.0.1" == endpoint);
            Assert.IsTrue("/emails" == relativeUri);

            X.GetEndpointFromUri("/", out endpoint, out relativeUri);
            Assert.IsTrue("127.0.0.1" == endpoint);
            Assert.IsTrue("/" == relativeUri);

            X.GetEndpointFromUri("123.456.789.000", out endpoint, out relativeUri);
            Assert.IsTrue("123.456.789.000" == endpoint);
            Assert.IsTrue("/" == relativeUri);

            Node node;
            X.GetNodeFromUri(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "/someuri", out node, out relativeUri);
            Assert.IsTrue("/someuri" == relativeUri);
            Assert.IsTrue("localhost" == node.HostName);
            Assert.IsTrue(StarcounterEnvironment.Default.UserHttpPort == node.PortNumber);

            X.GetNodeFromUri(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "somehost:1234/someuri/", out node, out relativeUri);
            Assert.IsTrue("/someuri/" == relativeUri);
            Assert.IsTrue("somehost" == node.HostName);
            Assert.IsTrue(1234 == node.PortNumber);

            X.GetNodeFromUri(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "www.starcounter.com:8081", out node, out relativeUri);
            Assert.IsTrue("/" == relativeUri);
            Assert.IsTrue("www.starcounter.com" == node.HostName);
            Assert.IsTrue(8081 == node.PortNumber);

            X.GetNodeFromUri(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "http://192.168.8.183:8585/upload", out node, out relativeUri);
            Assert.IsTrue("/upload" == relativeUri);
            Assert.IsTrue("192.168.8.183" == node.HostName);
            Assert.IsTrue(8585 == node.PortNumber);
        }

        [Test]
        public static void TestDifferentPorts() {

            Handle.GET(123, "/test123", () => {
                return "test123";
            });

            Handle.GET("/test8080", () => {
                return "test8080";
            });

            String s = null;
            s = X.GET<String>("/test8080");
            Assert.IsTrue(s == "test8080");

            s = X.GET<String>(123, "/test123");
            Assert.IsTrue(s == "test123");

            s = X.GET<String>(123, "/test123");
            Assert.IsTrue(s == "test123");
        }
    }
}
