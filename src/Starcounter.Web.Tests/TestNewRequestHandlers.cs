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

        public static String UserFunc6(Int64 p1, Decimal p2, Int32 p3, Int32 p4, Message m)
        {
            Assert.IsTrue(-3853984 == p1);
            Assert.IsTrue(-3535m == p2);
            Assert.IsTrue(1234 == p3);
            Assert.IsTrue(-78 == p4);

            return "UserFunc6!";
        }

        public static String UserFunc7(Message m, Int64 p1, Decimal p2, Int32 p3, Int32 p4, HttpRequest r)
        {
            Assert.IsTrue(-3853984 == p1);
            Assert.IsTrue(-3535m == p2);
            Assert.IsTrue(1234 == p3);
            Assert.IsTrue(-78 == p4);

            return "UserFunc7!";
        }

        public static String UserFunc8(Message m, dynamic dj, Int64 p1, Decimal p2, Int32 p3, Int32 p4, HttpRequest r)
        {
            Assert.IsTrue(-3853984 == p1);
            Assert.IsTrue(-3535m == p2);
            Assert.IsTrue(1234 == p3);
            Assert.IsTrue(-78 == p4);

            return "UserFunc8!";
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
            Byte[] stringParamsBytes = Encoding.ASCII.GetBytes(stringParams);

            UInt16 curOffset = 0, prevOffset = 0;
            for (Int32 i = 0; i < numTests; i++)
            {
                curOffset = (UInt16)stringParams.IndexOf('#', prevOffset);
                paramsInfo[i].offset_ = prevOffset;
                paramsInfo[i].len_ = (UInt16)(curOffset - prevOffset);
                prevOffset = (UInt16)(curOffset + 1);
            }

            Func<HttpRequest, IntPtr, IntPtr, Object> genDel1 = NewUserHandlers.UHC.GenerateParsingDelegate(80, "GET /", new Func<Int32, Int64, HttpRequest, String, Object>(UserHttpDelegateTests.UserFunc1));
            Func<HttpRequest, IntPtr, IntPtr, Object> genDel2 = NewUserHandlers.UHC.GenerateParsingDelegate(80, "GET /", new Func<Int32, Int64, String, Decimal, Object>(UserHttpDelegateTests.UserFunc2));
            Func<HttpRequest, IntPtr, IntPtr, Object> genDel3 = NewUserHandlers.UHC.GenerateParsingDelegate(80, "GET /", new Func<Int32, Int64, String, HttpRequest, Decimal, Double, Boolean, Object>(UserHttpDelegateTests.UserFunc3));
            Func<HttpRequest, IntPtr, IntPtr, Object> genDel4 = NewUserHandlers.UHC.GenerateParsingDelegate(80, "GET /", new Func<Int32, Int64, String, Decimal, Double, Boolean, DateTime, HttpRequest, Object>(UserHttpDelegateTests.UserFunc4));
            Func<HttpRequest, IntPtr, IntPtr, Object> genDel5 = NewUserHandlers.UHC.GenerateParsingDelegate(80, "GET /", new Func<Int64, Decimal, Int32, Int32, Object>(UserHttpDelegateTests.UserFunc5));
            Func<HttpRequest, IntPtr, IntPtr, Object> genDel6 = NewUserHandlers.UHC.GenerateParsingDelegate(80, "GET /", new Func<Int64, Decimal, Int32, Int32, Message, Object>(UserHttpDelegateTests.UserFunc6));
            Func<HttpRequest, IntPtr, IntPtr, Object> genDel7 = NewUserHandlers.UHC.GenerateParsingDelegate(80, "GET /", new Func<Message, Int64, Decimal, Int32, Int32, HttpRequest, Object>(UserHttpDelegateTests.UserFunc7));
            Func<HttpRequest, IntPtr, IntPtr, Object> genDel8 = NewUserHandlers.UHC.GenerateParsingDelegate(80, "GET /", new Func<Message, Object, Int64, Decimal, Int32, Int32, HttpRequest, Object>(UserHttpDelegateTests.UserFunc8));

            unsafe
            {
                fixed (Byte* p1 = stringParamsBytes)
                {
                    fixed (MixedCodeConstants.UserDelegateParamInfo* p2 = paramsInfo)
                    {
                        Byte[] r = Encoding.ASCII.GetBytes("GET /dashboard/123\r\n\r\n");
                        Assert.IsTrue("UserFunc1!" == (String)genDel1(new HttpRequest(r), (IntPtr)p1, (IntPtr)p2));
                        Assert.IsTrue("UserFunc2!" == (String)genDel2(null, (IntPtr)p1, (IntPtr)p2));
                        Assert.IsTrue("UserFunc3!" == (String)genDel3(new HttpRequest(r), (IntPtr)p1, (IntPtr)p2));
                        Assert.IsTrue("UserFunc4!" == (String)genDel4(new HttpRequest(r), (IntPtr)p1, (IntPtr)p2));
                        Assert.IsTrue("UserFunc5!" == (String)genDel5(null, (IntPtr)p1, (IntPtr)(p2 + 7)));
                        Assert.IsTrue("UserFunc6!" == (String)genDel6(null, (IntPtr)p1, (IntPtr)(p2 + 7)));
                        Assert.IsTrue("UserFunc7!" == (String)genDel7(new HttpRequest(r), (IntPtr)p1, (IntPtr)(p2 + 7)));
                        Assert.IsTrue("UserFunc8!" == (String)genDel8(new HttpRequest(r), (IntPtr)p1, (IntPtr)(p2 + 7)));
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