using NUnit.Framework;
using Starcounter.Advanced;
using Starcounter.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Starcounter.Internal.Tests;

namespace Starcounter.Internal.Test
{
    /// <summary>
    /// Tests user HTTP delegates registration and usage.
    /// </summary>
    [TestFixture]
    public class UserHttpDelegateTests
    {
        public static String UserFunc1(Int32 p1, Int64 p2, HttpRequest r, String p3)
        {
            Assert.IsTrue(123 == p1);
            Assert.IsTrue(21 == p2);
            Assert.IsTrue("hehe!" == p3);
            Assert.IsTrue("/dashboard/123" == r.Uri);

            return "UserFunc1!";
        }

        public static String UserFunc2(Int32 p1, Int64 p2, String p3, Decimal p4)
        {
            Assert.IsTrue(123 == p1);
            Assert.IsTrue(21 == p2);
            Assert.IsTrue("hehe!" == p3);
            Assert.IsTrue(3.141m == p4);

            return "UserFunc2!";
        }

        public static String UserFunc3(Int32 p1, Int64 p2, String p3, HttpRequest r, Decimal p4, Double p5, Boolean p6)
        {
            Assert.IsTrue(123 == p1);
            Assert.IsTrue(21 == p2);
            Assert.IsTrue("hehe!" == p3);
            Assert.IsTrue(3.141m == p4);
            Assert.IsTrue(9.8 == p5);
            Assert.IsTrue(true == p6);
            Assert.IsTrue("/dashboard/123" == r.Uri);

            return "UserFunc3!";
        }

        public static String UserFunc4(Int32 p1, Int64 p2, String p3, Decimal p4, Double p5, Boolean p6, DateTime p7, HttpRequest r)
        {
            Assert.IsTrue(123 == p1);
            Assert.IsTrue(21 == p2);
            Assert.IsTrue("hehe!" == p3);
            Assert.IsTrue(3.141m == p4);
            Assert.IsTrue(9.8 == p5);
            Assert.IsTrue(true == p6);
            Assert.IsTrue(0 == p7.CompareTo(DateTime.Parse("10-1-2009 19:34")));
            Assert.IsTrue("/dashboard/123" == r.Uri);

            return "UserFunc4!";
        }

        public static String UserFunc5(Int64 p1, Decimal p2, Int32 p3, Int32 p4)
        {
            Assert.IsTrue(-3853984 == p1);
            Assert.IsTrue(-3535m == p2);
            Assert.IsTrue(1234 == p3);
            Assert.IsTrue(-78 == p4);

            return "UserFunc5!";
        }

        public static String UserFunc6(Int64 p1, Decimal p2, Int32 p3, Int32 p4, PersonMessage m)
        {
            Assert.IsTrue(-3853984 == p1);
            Assert.IsTrue(-3535m == p2);
            Assert.IsTrue(1234 == p3);
            Assert.IsTrue(-78 == p4);

            dynamic dj = m; // Using dynamic just to use properties instead of template lookups.
            Assert.AreEqual("Allan", dj.FirstName);
            Assert.AreEqual("Ballan", dj.LastName);
            Assert.AreEqual(19, dj.Age);
            Assert.AreEqual("123-555-7890", dj.PhoneNumbers[0].Number);

            return "UserFunc6!";
        }

        public static String UserFunc7(PersonMessage m, Int64 p1, Decimal p2, Int32 p3, Int32 p4, HttpRequest r)
        {
            Assert.IsTrue(-3853984 == p1);
            Assert.IsTrue(-3535m == p2);
            Assert.IsTrue(1234 == p3);
            Assert.IsTrue(-78 == p4);

            dynamic dj = m; // Using dynamic just to use properties instead of template lookups.
            Assert.AreEqual("Allan", dj.FirstName);
            Assert.AreEqual("Ballan", dj.LastName);
            Assert.AreEqual(19, dj.Age);
            Assert.AreEqual("123-555-7890", dj.PhoneNumbers[0].Number);

            return "UserFunc7!";
        }

        public static String UserFunc8(PersonMessage m, dynamic dj, Int64 p1, Decimal p2, Int32 p3, Int32 p4, HttpRequest r)
        {
            Assert.IsTrue(-3853984 == p1);
            Assert.IsTrue(-3535m == p2);
            Assert.IsTrue(1234 == p3);
            Assert.IsTrue(-78 == p4);

            return "UserFunc8!";
        }

        public static String UserFunc9(dynamic dj, HttpRequest r) {
            Assert.AreEqual("Allan", dj.FirstName);
            Assert.AreEqual("Ballan", dj.LastName);
            Assert.AreEqual(19, dj.Age);
            Assert.AreEqual("123-555-7890", dj.PhoneNumbers[0].Number);
            
            return "UserFunc9!";
        }

        public static String UserFunc10(PersonMessage m, HttpRequest r) {
            dynamic dj = m; // Using dynamic just to use properties instead of template lookups.
            Assert.AreEqual("Allan", dj.FirstName);
            Assert.AreEqual("Ballan", dj.LastName);
            Assert.AreEqual(19, dj.Age);
            Assert.AreEqual("123-555-7890", dj.PhoneNumbers[0].Number);

            return "UserFunc10!";
        }


        /// <summary>
        /// Tests simple correct HTTP request.
        /// </summary>
        [Test]
        public void TestUserHttpHandlers()
        {
            const Int32 numTests = 11;
            MixedCodeConstants.UserDelegateParamInfo[] paramsInfo = new MixedCodeConstants.UserDelegateParamInfo[numTests]
            {
                new MixedCodeConstants.UserDelegateParamInfo(0, 0),
                new MixedCodeConstants.UserDelegateParamInfo(0, 0),
                new MixedCodeConstants.UserDelegateParamInfo(0, 0),
                new MixedCodeConstants.UserDelegateParamInfo(0, 0),
                new MixedCodeConstants.UserDelegateParamInfo(0, 0),
                new MixedCodeConstants.UserDelegateParamInfo(0, 0),
                new MixedCodeConstants.UserDelegateParamInfo(0, 0),
                new MixedCodeConstants.UserDelegateParamInfo(0, 0),
                new MixedCodeConstants.UserDelegateParamInfo(0, 0),
                new MixedCodeConstants.UserDelegateParamInfo(0, 0),
                new MixedCodeConstants.UserDelegateParamInfo(0, 0)
            };


            String stringParams = "123#21#hehe!#3.141#9.8#true#10-1-2009 19:34#-3853984#-03535#0001234#-000078#";
            String jsonContent = "{\"FirstName\":\"Allan\",\"LastName\":\"Ballan\",\"Age\":19,\"PhoneNumbers\":[{\"Number\":\"123-555-7890\"}]}";
            Byte[] stringParamsBytes = Encoding.ASCII.GetBytes(stringParams);

            UInt16 curOffset = 0, prevOffset = 0;
            for (Int32 i = 0; i < numTests; i++)
            {
                curOffset = (UInt16)stringParams.IndexOf('#', prevOffset);
                paramsInfo[i].offset_ = prevOffset;
                paramsInfo[i].len_ = (UInt16)(curOffset - prevOffset);
                prevOffset = (UInt16)(curOffset + 1);
            }

            Func<HttpRequest, IntPtr, IntPtr, Object> genDel1 = UserHandlerCodegen.NewNativeUriCodegen.GenerateParsingDelegate(80, "GET /", new Func<Int32, Int64, HttpRequest, String, Object>(UserHttpDelegateTests.UserFunc1));
            Func<HttpRequest, IntPtr, IntPtr, Object> genDel2 = UserHandlerCodegen.NewNativeUriCodegen.GenerateParsingDelegate(80, "GET /", new Func<Int32, Int64, String, Decimal, Object>(UserHttpDelegateTests.UserFunc2));
            Func<HttpRequest, IntPtr, IntPtr, Object> genDel3 = UserHandlerCodegen.NewNativeUriCodegen.GenerateParsingDelegate(80, "GET /", new Func<Int32, Int64, String, HttpRequest, Decimal, Double, Boolean, Object>(UserHttpDelegateTests.UserFunc3));
            Func<HttpRequest, IntPtr, IntPtr, Object> genDel4 = UserHandlerCodegen.NewNativeUriCodegen.GenerateParsingDelegate(80, "GET /", new Func<Int32, Int64, String, Decimal, Double, Boolean, DateTime, HttpRequest, Object>(UserHttpDelegateTests.UserFunc4));
            Func<HttpRequest, IntPtr, IntPtr, Object> genDel5 = UserHandlerCodegen.NewNativeUriCodegen.GenerateParsingDelegate(80, "GET /", new Func<Int64, Decimal, Int32, Int32, Object>(UserHttpDelegateTests.UserFunc5));
            Func<HttpRequest, IntPtr, IntPtr, Object> genDel6 = UserHandlerCodegen.NewNativeUriCodegen.GenerateParsingDelegate(80, "GET /", new Func<Int64, Decimal, Int32, Int32, PersonMessage, Object>(UserHttpDelegateTests.UserFunc6));
            Func<HttpRequest, IntPtr, IntPtr, Object> genDel7 = UserHandlerCodegen.NewNativeUriCodegen.GenerateParsingDelegate(80, "GET /", new Func<PersonMessage, Int64, Decimal, Int32, Int32, HttpRequest, Object>(UserHttpDelegateTests.UserFunc7));
            Func<HttpRequest, IntPtr, IntPtr, Object> genDel8 = UserHandlerCodegen.NewNativeUriCodegen.GenerateParsingDelegate(80, "GET /", new Func<PersonMessage, Object, Int64, Decimal, Int32, Int32, HttpRequest, Object>(UserHttpDelegateTests.UserFunc8));

            Func<HttpRequest, IntPtr, IntPtr, Object> genDel9 = UserHandlerCodegen.NewNativeUriCodegen.GenerateParsingDelegate(80, "GET /", new Func<Object, HttpRequest, Object>(UserHttpDelegateTests.UserFunc9));
            Func<HttpRequest, IntPtr, IntPtr, Object> genDel10 = UserHandlerCodegen.NewNativeUriCodegen.GenerateParsingDelegate(80, "GET /", new Func<PersonMessage, HttpRequest, Object>(UserHttpDelegateTests.UserFunc10));

            unsafe
            {
                fixed (Byte* p1 = stringParamsBytes)
                {
                    fixed (MixedCodeConstants.UserDelegateParamInfo* p2 = paramsInfo)
                    {
                        Byte[] requestStrNoContent = Encoding.ASCII.GetBytes("GET /dashboard/123\r\n\r\n");
                        Byte[] requestStrWithContent = Encoding.ASCII.GetBytes("GET /dashboard/123\r\nContent-Length:" 
                                                            + Encoding.ASCII.GetByteCount(jsonContent) 
                                                            + "\r\n\r\n" 
                                                            + jsonContent);

                        Assert.IsTrue("UserFunc1!" == (String)genDel1(new HttpRequest(requestStrNoContent), (IntPtr)p1, (IntPtr)p2));
                        Assert.IsTrue("UserFunc2!" == (String)genDel2(null, (IntPtr)p1, (IntPtr)p2));
                        Assert.IsTrue("UserFunc3!" == (String)genDel3(new HttpRequest(requestStrNoContent), (IntPtr)p1, (IntPtr)p2));
                        Assert.IsTrue("UserFunc4!" == (String)genDel4(new HttpRequest(requestStrNoContent), (IntPtr)p1, (IntPtr)p2));
                        Assert.IsTrue("UserFunc5!" == (String)genDel5(null, (IntPtr)p1, (IntPtr)(p2 + 7)));

                        HttpRequest req = new HttpRequest(requestStrWithContent);
                        req.ArgMessageObjectType = typeof(PersonMessage);

                        Assert.IsTrue("UserFunc6!" == (String)genDel6(req, (IntPtr)p1, (IntPtr)(p2 + 7)));
                        Assert.IsTrue("UserFunc7!" == (String)genDel7(req, (IntPtr)p1, (IntPtr)(p2 + 7)));
                        Assert.IsTrue("UserFunc8!" == (String)genDel8(req, (IntPtr)p1, (IntPtr)(p2 + 7)));

                        Assert.IsTrue("UserFunc9!" == (String)genDel9(new HttpRequest(requestStrWithContent), (IntPtr)p1, (IntPtr)(p2 + 7)));
                        Assert.IsTrue("UserFunc10!" == (String)genDel10(req, (IntPtr)p1, (IntPtr)(p2 + 7)));
                    }
                }
            }
        }

        /// <summary>
        /// Tests performance.
        /// </summary>
        [Test]
        public void TestUserHttpHandlersPerformance()
        {
            /*
            String intString = "-12345";
            Byte[] intStringBytes = Encoding.ASCII.GetBytes(intString);

            Stopwatch sw = new Stopwatch();

            sw.Start();
            Int64 r = 0;
            for (Int64 i = 0; i < 100000000; i++)
            {
                unsafe
                {
                    fixed (Byte* b = intStringBytes)
                    {
                        IntPtr ib = (IntPtr)b;
                        r += UserHandlerCodegen.FastParseInt(ib, 6); ///Int64.Parse(intString)
                    }
                }
            }

            sw.Stop();
            Console.WriteLine("Time ms: " + sw.ElapsedMilliseconds + " result: " + r);
            */
        }
    }
}