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

            NodeX.GetEndpointFromUri("endpoint", out endpoint, out relativeUri);
            Assert.IsTrue("endpoint" == endpoint);
            Assert.IsTrue("/" == relativeUri);

            NodeX.GetEndpointFromUri("endpoint/", out endpoint, out relativeUri);
            Assert.IsTrue("endpoint" == endpoint);
            Assert.IsTrue("/" == relativeUri);

            NodeX.GetEndpointFromUri("http://endpoint/", out endpoint, out relativeUri);
            Assert.IsTrue("endpoint" == endpoint);
            Assert.IsTrue("/" == relativeUri);

            NodeX.GetEndpointFromUri("http://endpoint", out endpoint, out relativeUri);
            Assert.IsTrue("endpoint" == endpoint);
            Assert.IsTrue("/" == relativeUri);

            NodeX.GetEndpointFromUri("http://endpoint/", out endpoint, out relativeUri);
            Assert.IsTrue("endpoint" == endpoint);
            Assert.IsTrue("/" == relativeUri);

            NodeX.GetEndpointFromUri("http://endpoint/a", out endpoint, out relativeUri);
            Assert.IsTrue("endpoint" == endpoint);
            Assert.IsTrue("/a" == relativeUri);

            NodeX.GetEndpointFromUri("http://endpoint/a/", out endpoint, out relativeUri);
            Assert.IsTrue("endpoint" == endpoint);
            Assert.IsTrue("/a/" == relativeUri);

            NodeX.GetEndpointFromUri("http://www.starcounter.com", out endpoint, out relativeUri);
            Assert.IsTrue("www.starcounter.com" == endpoint);
            Assert.IsTrue("/" == relativeUri);

            NodeX.GetEndpointFromUri("ws://www.starcounter.com", out endpoint, out relativeUri);
            Assert.IsTrue("www.starcounter.com" == endpoint);
            Assert.IsTrue("/" == relativeUri);

            NodeX.GetEndpointFromUri("file://scbuildserver/FTP/SCDev/BuildSystem/Logs/StatisticsEngine/default.htm", out endpoint, out relativeUri);
            Assert.IsTrue("scbuildserver" == endpoint);
            Assert.IsTrue("/FTP/SCDev/BuildSystem/Logs/StatisticsEngine/default.htm" == relativeUri);

            NodeX.GetEndpointFromUri("/emails", out endpoint, out relativeUri);
            Assert.IsTrue("127.0.0.1" == endpoint);
            Assert.IsTrue("/emails" == relativeUri);

            NodeX.GetEndpointFromUri("/", out endpoint, out relativeUri);
            Assert.IsTrue("127.0.0.1" == endpoint);
            Assert.IsTrue("/" == relativeUri);

            NodeX.GetEndpointFromUri("123.456.789.000", out endpoint, out relativeUri);
            Assert.IsTrue("123.456.789.000" == endpoint);
            Assert.IsTrue("/" == relativeUri);

            Node node;
            NodeX.GetNodeFromUri("/someuri", out node, out relativeUri);
            Assert.IsTrue("127.0.0.1" == node.HostName);
            Assert.IsTrue(NewConfig.Default.UserHttpPort == node.PortNumber);

            NodeX.GetNodeFromUri("somehost:1234/someuri/", out node, out relativeUri);
            Assert.IsTrue("somehost:1234" == node.HostName);
            Assert.IsTrue(1234 == node.PortNumber);

            NodeX.GetNodeFromUri("www.starcounter.com:8081", out node, out relativeUri);
            Assert.IsTrue("www.starcounter.com:8081" == node.HostName);
            Assert.IsTrue(8081 == node.PortNumber);
        }
    }
}
