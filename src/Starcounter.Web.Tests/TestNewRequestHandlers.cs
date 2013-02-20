using NUnit.Framework;
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
        public static String UserFunc1(Int32 p1, Int64 p2, String p3)
        {
            Assert.IsTrue(123 == p1);
            Assert.IsTrue(21 == p2);
            Assert.IsTrue("hehe!" == p3);

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

        public static String UserFunc3(Int32 p1, Int64 p2, String p3, Decimal p4, Double p5, Boolean p6)
        {
            Assert.IsTrue(123 == p1);
            Assert.IsTrue(21 == p2);
            Assert.IsTrue("hehe!" == p3);
            Assert.IsTrue(3.141m == p4);
            Assert.IsTrue(9.8 == p5);
            Assert.IsTrue(true == p6);

            return "UserFunc3!";
        }

        public static String UserFunc4(Int32 p1, Int64 p2, String p3, Decimal p4, Double p5, Boolean p6, DateTime p7)
        {
            Assert.IsTrue(123 == p1);
            Assert.IsTrue(21 == p2);
            Assert.IsTrue("hehe!" == p3);
            Assert.IsTrue(3.141m == p4);
            Assert.IsTrue(9.8 == p5);
            Assert.IsTrue(true == p6);
            Assert.IsTrue(0 == p7.CompareTo(DateTime.Parse("10-1-2009 19:34")));

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

        /// <summary>
        /// Tests simple correct HTTP request.
        /// </summary>
        [Test]
        public void TestUserHttpHandlers()
        {
            const Int32 numTests = 11;
            UserHandlerParams[] paramsInfo = new UserHandlerParams[numTests]
            {
                new UserHandlerParams(0, 0, (Byte)ArgumentTypes.ARG_INT32, 0),
                new UserHandlerParams(0, 0, (Byte)ArgumentTypes.ARG_INT64, 0),
                new UserHandlerParams(0, 0, (Byte)ArgumentTypes.ARG_STRING, 0),
                new UserHandlerParams(0, 0, (Byte)ArgumentTypes.ARG_DECIMAL, 0),
                new UserHandlerParams(0, 0, (Byte)ArgumentTypes.ARG_DOUBLE, 0),
                new UserHandlerParams(0, 0, (Byte)ArgumentTypes.ARG_BOOLEAN, 0),
                new UserHandlerParams(0, 0, (Byte)ArgumentTypes.ARG_DATETIME, 0),
                new UserHandlerParams(0, 0, (Byte)ArgumentTypes.ARG_INT64, 0),
                new UserHandlerParams(0, 0, (Byte)ArgumentTypes.ARG_DECIMAL, 0),
                new UserHandlerParams(0, 0, (Byte)ArgumentTypes.ARG_INT32, 0),
                new UserHandlerParams(0, 0, (Byte)ArgumentTypes.ARG_INT32, 0)
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

            Func<IntPtr, IntPtr, Int32, Object> genDel1 = UserHandlerCodegen.uhc.GenerateParsingDelegate(new Func<Int32, Int64, String, Object>(UserHttpDelegateTests.UserFunc1));
            Func<IntPtr, IntPtr, Int32, Object> genDel2 = UserHandlerCodegen.uhc.GenerateParsingDelegate(new Func<Int32, Int64, String, Decimal, Object>(UserHttpDelegateTests.UserFunc2));
            Func<IntPtr, IntPtr, Int32, Object> genDel3 = UserHandlerCodegen.uhc.GenerateParsingDelegate(new Func<Int32, Int64, String, Decimal, Double, Boolean, Object>(UserHttpDelegateTests.UserFunc3));
            Func<IntPtr, IntPtr, Int32, Object> genDel4 = UserHandlerCodegen.uhc.GenerateParsingDelegate(new Func<Int32, Int64, String, Decimal, Double, Boolean, DateTime, Object>(UserHttpDelegateTests.UserFunc4));
            Func<IntPtr, IntPtr, Int32, Object> genDel5 = UserHandlerCodegen.uhc.GenerateParsingDelegate(new Func<Int64, Decimal, Int32, Int32, Object>(UserHttpDelegateTests.UserFunc5));

            unsafe
            {
                fixed (Byte* p1 = stringParamsBytes)
                {
                    fixed (UserHandlerParams* p2 = paramsInfo)
                    {
                        Assert.IsTrue("UserFunc1!" == (String)genDel1((IntPtr)p1, (IntPtr)p2, 3));
                        Assert.IsTrue("UserFunc2!" == (String)genDel2((IntPtr)p1, (IntPtr)p2, 4));
                        Assert.IsTrue("UserFunc3!" == (String)genDel3((IntPtr)p1, (IntPtr)p2, 6));
                        Assert.IsTrue("UserFunc4!" == (String)genDel4((IntPtr)p1, (IntPtr)p2, 7));
                        Assert.IsTrue("UserFunc5!" == (String)genDel5((IntPtr)p1, (IntPtr)(p2 + 7), 4));
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