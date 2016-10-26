using System;
using System.Threading;
using NUnit.Framework;

namespace Starcounter.SqlProcessor.Tests {
    [TestFixture]
    public static class MultiThreadedMemoryLeakTests {
        static String[] Queries =
            {
                "select * from (select col1.ref1.ref2.ref3, ns1.ns2.ns3.ns4.fun(col2) as ncol, avg(col3) from tbl1, ns5.ns6.ns7.tbl2 group by ncol, col1.ref1.ref2.ref3 having ncol > 4) ntbl where col2 > 5",
                "select distinct col1, col2 from tbl1, tbl2 where col2 in (select col9 from tbl2 where col1 order by col3.ref1.ref2.ref3.ref4)",
                "select avg(col1),sum(col2),ns1.ns2.ns3.ns4.uagg(col5) ncol1 from tbl1 natural left outer join (select * from tbl1 join tbl2 on col1 = col2 where prop) as ntbl where bfun(col2)",
                "select l, l.* from SqlTest.Test1.Location l",
                "select sum(cast(tabl1.col1 as typ1)), avg(col1.ref1.col2), ns1.ns2.func1(col3) as coln1" +
                " from ns1.ns2.tabl1, (select distinct col5 from tabl2 where col9 = 43 order by col6) as tabl2"+
                " where col3 LIKE '%name%' and ns1.ns2.fun3(tabl2.col5)"+
                " group by coln1 having ns1.ns2.ns3.fn3(col3) order by col9",
                "select sum(cast(tabl1.col1 as typ1)), avg(col1.ref1.col2), ns1.ns2.func1(col3) as coln1" +
                " from ns1.ns2.tabl1, (select distinct col5 from tabl2 where col9 = 43 order by col6) as tabl2"+
                " where col3 LIKE '%name%' and ns1.ns2.fun3(tabl2.col5)"+
                " group by coln1 having ns1.ns2.ns3.fn3(col3) order by col9",
                "select sum(cast(tabl1.col1 as typ1)), avg(col1.ref1.col2), ns1.ns2.func1(col3) as coln1" +
                " from ns1.ns2.tabl1, (select distinct col5 from tabl2 where col9 = 43 order by col6) as tabl2"+
                " where col3 LIKE '%name%' and ns1.ns2.fun3(tabl2.col5)"+
                " group by coln1 having ns1.ns2.ns3.fn3(col3) order by col9",
                "select d, d.* from SqlTest.Test1.Department d",
                "select e1, e2 from SqlTest.Test1.Employee e1, SqlTest.Test1.Employee e2 where e1.HireDate < e2.HireDate and e1.Department = e2.Department",
                "select * from (select col1.ref1.ref2.ref3, ns1.ns2.ns3.ns4.fun(col2) as ncol, avg(col3) from tbl1, ns5.ns6.ns7.tbl2 group by ncol, col1.ref1.ref2.ref3 having ncol > 4) ntbl where col2 > 5",
                "select distinct col1, col2 from tbl1, tbl2 where col2 in (select col9 from tbl2 where col1 order by col3.ref1.ref2.ref3.ref4)",
                "select avg(col1),sum(col2),ns1.ns2.ns3.ns4.uagg(col5) ncol1 from tbl1 natural left outer join (select * from tbl1 join tbl2 on col1 = col2 where prop) as ntbl where bfun(col2)",
                "select l, l.* from SqlTest.Test1.Location l",
                "select d, d.* from SqlTest.Test1.Department d",
                "SELECT e.LastName, e.FirstName FROM Employee e FETCH 5",
                "select loooooooooooooooooooooooangcoooooooooooooooooooooooooooooooooooolumnnaaaaaaaaaaaaaaaaaaaaaaaaaaame,"+
                    "colname as newloooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooonggggggggggggggname"+
                    " from veeeeeeeeeeeeeeeeeeryloooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooongtaaaaaaaaaaaaaaaaaaaaaaaaaaaaaablenaaaaaaaaaaame",
//                "select ",
                "select e1, e2 from SqlTest.Test1.Employee e1, SqlTest.Test1.Employee e2 where e1.HireDate < e2.HireDate and e1.Department = e2.Department"//,
                //"select d from Employee e left join Department d on e.Department = d",
                //"select cast(p.Father as Employee).Department from SqlTest.Test1.Person p where cast(p.Father as Employee).Department.Name = 'Server'"
            };

        static Exception[] exceptions = new Exception[Queries.Length];

        internal static bool IsFromCMD = false;

        [Test]
        public static void MultithreadedTest() {
            Thread[] threads = new Thread[Queries.Length];
            for (int i = 0; i < Queries.Length; i++) {
                threads[i] = new Thread(TestQuery);
                threads[i].Start(i);
            }
            for (int i = 0; i < Queries.Length; i++)
                threads[i].Join();
            if (IsFromCMD)
                for (int i = 0; i < Queries.Length; i++) {
                    Console.WriteLine(exceptions[i].Data[ErrorCode.EC_TRANSPORT_KEY]);
                    if ((uint)exceptions[i].Data[ErrorCode.EC_TRANSPORT_KEY] != SqlProcessorTests.ParseOK)
                        Console.WriteLine(exceptions[i].Message);
                }
            for (int i = 0; i < Queries.Length; i++) {
                Assert.NotNull(exceptions[i], "Query " + i + ": " + Queries[i]);
                Assert.AreEqual(SqlProcessorTests.ParseOK, exceptions[i].Data[ErrorCode.EC_TRANSPORT_KEY],
                    "Exception for query " + i + ": " + exceptions[i].Message);
            }
#if false
            Assert.AreEqual(0, SqlProcessor.scsql_dump_memory_leaks());
#endif
            Console.WriteLine(Queries.Length + " queries are executed in " + Queries.Length + " threads.");
        }

        [Test]
        public static void SequentialTest() {
            for (int i = 0; i < Queries.Length; i++) {
                Exception ex = SqlProcessor.CallSqlProcessor(Queries[i]);
                Assert.AreEqual(SqlProcessorTests.ParseOK, ex.Data[ErrorCode.EC_TRANSPORT_KEY], ex.Message);
            }
#if false
            Assert.AreEqual(0, SqlProcessor.scsql_dump_memory_leaks());
#endif
        }

        private static void TestQuery(object o) {
            int i = (int)o;
            exceptions[i] = SqlProcessor.CallSqlProcessor(Queries[i]);
        }
    }
}
