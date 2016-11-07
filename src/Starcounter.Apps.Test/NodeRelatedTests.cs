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

            Http.GetEndpointFromUri("endpoint", out endpoint, out relativeUri);
            Assert.IsTrue("endpoint" == endpoint);
            Assert.IsTrue("/" == relativeUri);

            Http.GetEndpointFromUri("endpoint/", out endpoint, out relativeUri);
            Assert.IsTrue("endpoint" == endpoint);
            Assert.IsTrue("/" == relativeUri);

            Http.GetEndpointFromUri("http://endpoint/", out endpoint, out relativeUri);
            Assert.IsTrue("endpoint" == endpoint);
            Assert.IsTrue("/" == relativeUri);

            Http.GetEndpointFromUri("http://endpoint", out endpoint, out relativeUri);
            Assert.IsTrue("endpoint" == endpoint);
            Assert.IsTrue("/" == relativeUri);

            Http.GetEndpointFromUri("http://endpoint/", out endpoint, out relativeUri);
            Assert.IsTrue("endpoint" == endpoint);
            Assert.IsTrue("/" == relativeUri);

            Http.GetEndpointFromUri("http://endpoint/a", out endpoint, out relativeUri);
            Assert.IsTrue("endpoint" == endpoint);
            Assert.IsTrue("/a" == relativeUri);

            Http.GetEndpointFromUri("http://endpoint/a/", out endpoint, out relativeUri);
            Assert.IsTrue("endpoint" == endpoint);
            Assert.IsTrue("/a/" == relativeUri);

            Http.GetEndpointFromUri("http://www.starcounter.com", out endpoint, out relativeUri);
            Assert.IsTrue("www.starcounter.com" == endpoint);
            Assert.IsTrue("/" == relativeUri);

            Http.GetEndpointFromUri("ws://www.starcounter.com", out endpoint, out relativeUri);
            Assert.IsTrue("www.starcounter.com" == endpoint);
            Assert.IsTrue("/" == relativeUri);

            Http.GetEndpointFromUri("file://teamcity.starcounter.org/FTP/SCDev/BuildSystem/Logs/StatisticsEngine/default.htm", out endpoint, out relativeUri);
            Assert.IsTrue("teamcity.starcounter.org" == endpoint);
            Assert.IsTrue("/FTP/SCDev/BuildSystem/Logs/StatisticsEngine/default.htm" == relativeUri);

            Http.GetEndpointFromUri("/emails", out endpoint, out relativeUri);
            Assert.IsTrue("127.0.0.1" == endpoint);
            Assert.IsTrue("/emails" == relativeUri);

            Http.GetEndpointFromUri("/", out endpoint, out relativeUri);
            Assert.IsTrue("127.0.0.1" == endpoint);
            Assert.IsTrue("/" == relativeUri);

            Http.GetEndpointFromUri("123.456.789.000", out endpoint, out relativeUri);
            Assert.IsTrue("123.456.789.000" == endpoint);
            Assert.IsTrue("/" == relativeUri);

            Node node;
            Http.GetNodeFromUri(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "http://somehost", out node, out relativeUri);
            Assert.IsTrue("/" == relativeUri);
            Assert.IsTrue("somehost" == node.HostName);
            Assert.IsTrue("somehost:80" == node.Endpoint);
            Assert.IsTrue(80 == node.PortNumber);

            Http.GetNodeFromUri(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "http://somehost/", out node, out relativeUri);
            Assert.IsTrue("/" == relativeUri);
            Assert.IsTrue("somehost" == node.HostName);
            Assert.IsTrue("somehost:80" == node.Endpoint);
            Assert.IsTrue(80 == node.PortNumber);

            Http.GetNodeFromUri(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "http://somehost//", out node, out relativeUri);
            Assert.IsTrue("//" == relativeUri);
            Assert.IsTrue("somehost" == node.HostName);
            Assert.IsTrue("somehost:80" == node.Endpoint);
            Assert.IsTrue(80 == node.PortNumber);

            Http.GetNodeFromUri(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "http://somehost/someuri", out node, out relativeUri);
            Assert.IsTrue("/someuri" == relativeUri);
            Assert.IsTrue("somehost" == node.HostName);
            Assert.IsTrue("somehost:80" == node.Endpoint);
            Assert.IsTrue(80 == node.PortNumber);

            Http.GetNodeFromUri(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "http://somehost:1234/someuri/", out node, out relativeUri);
            Assert.IsTrue("/someuri/" == relativeUri);
            Assert.IsTrue("somehost" == node.HostName);
            Assert.IsTrue("somehost:1234" == node.Endpoint);
            Assert.IsTrue(1234 == node.PortNumber);

            Http.GetNodeFromUri(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "http://www.starcounter.com:8081", out node, out relativeUri);
            Assert.IsTrue("/" == relativeUri);
            Assert.IsTrue("www.starcounter.com" == node.HostName);
            Assert.IsTrue("www.starcounter.com:8081" == node.Endpoint);
            Assert.IsTrue(8081 == node.PortNumber);

            Http.GetNodeFromUri(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, "http://192.168.8.183:8585/upload", out node, out relativeUri);
            Assert.IsTrue("/upload" == relativeUri);
            Assert.IsTrue("192.168.8.183" == node.HostName);
            Assert.IsTrue("192.168.8.183:8585" == node.Endpoint);
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
            s = Self.GET<String>("/test8080");
            Assert.IsTrue(s == "test8080");

            s = Self.GET<String>(123, "/test123");
            Assert.IsTrue(s == "test123");

            s = Self.GET<String>(123, "/test123");
            Assert.IsTrue(s == "test123");
        }
    }
}
