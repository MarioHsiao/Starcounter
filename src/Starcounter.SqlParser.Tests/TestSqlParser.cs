using System;
using System.Threading;
using NUnit.Framework;
using Starcounter;
using Starcounter.Query.RawParserAnalyzer;

namespace Starcounter.SqlParser.Tests
{
    [TestFixture]
    public static class TestSqlParser {
        [Test]
        public static void ParseQueriesForErrors() {
            ParserAnalyzer analyzer = new ParserAnalyzer();
            analyzer.ParseQuery("select ", true);
            analyzer.ParseQuery("selec", true);
            analyzer.ParseQuery("select * from t1");
            analyzer.ParseQuery("select sd order byd", true);
            analyzer.ParseQuery("select * from tbl where col like '\\n and \\u and \\\\'");
            analyzer.ParseQuery("select * from tble limit 1, 2", true);
            analyzer.ParseQuery("(SELECT foo ORDER BY bar) ORDER BY baz", true);
            analyzer.ParseQuery("SELECT * from table1 offsetkey 4");
            analyzer.ParseQuery("select * from [table] where [offset] = 3", true);
            analyzer.ParseQuery("select * from tbl where col = $1");
            analyzer.ParseQuery("select * from table", true);
            analyzer.ParseQuery("select * from \"table\"");
            analyzer.ParseQuery("SELECT E'\\xDEADBEEF'");
            analyzer.ParseQuery("select * from tbl option index (tbl indx1)");
            analyzer.ParseQuery("select * from tbl option index tbl indx1", true);
            analyzer.ParseQuery("select * from tbl option index tbl", true);
            analyzer.ParseQuery("select * from tbl1 union select * from tbl option index (tbl indx1)");// MUST FAIL
            analyzer.ParseQuery("select * from tbl1 union (select * from tbl option index (tbl indx1))");
            analyzer.ParseQuery("select * from tbl option join order tbl", true);
            analyzer.ParseQuery("select * from tbl option join order (tbl)");
            analyzer.ParseQuery("select * from tbl option index (tbl indx1), join order (tbl)");
            analyzer.ParseQuery("SELECT d.Name, e.LastName FROM Department d JOIN Employee e ON e.Department = d WHERE e.FirstName = 'Bob' OPTION JOIN ORDER (e,d)");
            analyzer.ParseQuery("select (f.d()).d.f");
            analyzer.ParseQuery("select f.d().f.d.s()");
            analyzer.ParseQuery("select (f.d()).d.f()");
            analyzer.ParseQuery("select cast(col as typ<typ2>) from tble", true);
            analyzer.ParseQuery("select nm.typ<typ2>.fn()", true);
            analyzer.ParseQuery("select $1.f()");
            analyzer.ParseQuery("select cast(col as type).ref1.ref2 from tbl1");
            analyzer.ParseQuery("select I(A)");
            analyzer.ParseQuery("select (I(A).F()", true);
            analyzer.ParseQuery("select (I(A).J(A,B).F()", true);
            analyzer.ParseQuery("select (I(A,B,C).J(A,B).F(A)()", true);
            analyzer.ParseQuery("select I.J(A,B).F()");
            analyzer.ParseQuery("select I.J(A,B).F<A>()");
            analyzer.ParseQuery("select I.J.F()");
            analyzer.ParseQuery("select I.J(A,B).F<A<C>,T>()");
            analyzer.ParseQuery("select I.J(A)");
            analyzer.ParseQuery("select I<A>()");
            analyzer.ParseQuery("select I.J<A>.f<A>(g)");
            analyzer.ParseQuery("select (I.J(a)).T<K>(v)");
            analyzer.ParseQuery("select a < b > c", true);
            analyzer.ParseQuery("select a < b , c >", true);
            analyzer.ParseQuery("select f()()", true);
            analyzer.ParseQuery("select f<typ1>()");
            analyzer.ParseQuery("select cast(a+b as c)");
            analyzer.ParseQuery("select cast(a+b as int)");
            analyzer.ParseQuery("select cast(a+b as c.d(i)", true);
            analyzer.ParseQuery("select cast(a+b as c.d(i))", true);
            analyzer.ParseQuery("select cast(a+b as c.d[])");
            analyzer.ParseQuery("select cast(a+b as c.d<i,d>)");
            analyzer.ParseQuery("select cast(a+b as c.d<i<g>,d>)");
            analyzer.ParseQuery("select DATE '2012-02-02'");
            analyzer.ParseQuery("select col1.fn().col2 = DATE '2012-02-02' from ns1.tbl1");
            analyzer.ParseQuery("select ?.f()");
            analyzer.ParseQuery("select * from tbl where col1 = ?");
            analyzer.ParseQuery("select binary 'x'", true);
            analyzer.ParseQuery("select BINARY 'AB14093FE'");
            analyzer.ParseQuery("SELECT e FROM Employee e WHERE e.Commission = TRUE");
            analyzer.ParseQuery("SELECT e FROM Employee e WHERE e.Salary = 5000");
            analyzer.ParseQuery("SELECT e FROM Employee e WHERE e.Salary = 5000.00");
            analyzer.ParseQuery("SELECT e FROM Employee e WHERE e.Salary = 5.0E3");
            analyzer.ParseQuery("SELECT p FROM Photo p WHERE p.Description = 'Smith''s family'");
            analyzer.ParseQuery("SELECT e FROM Employee e WHERE e.HireDate = DATE '2006-11-01'");
            analyzer.ParseQuery("SELECT e FROM Employee e WHERE e.HireDate = TIMESTAMP '2006-11-01 00:00:00'");
            analyzer.ParseQuery("SELECT l FROM Department d WHERE d.BinaryId = BINARY 'D91FA24E19FB065A'");
            analyzer.ParseQuery("SELECT e FROM Employee e WHERE e = OBJECT 123");
            analyzer.ParseQuery("SELECT * from tbl where col1 STARTS WITH 'Start with me'");
            analyzer.ParseQuery("select t from tbl t where str starts with ?");
            analyzer.ParseQuery("select getextension<myperson>() from example.person");
            analyzer.ParseQuery("select e from example.employee e where e.getextension<example.myperson>().myname =?");
            analyzer.ParseQuery("select u from User u; select d from Department d");
            analyzer.ParseQuery("select u from User u where u.name = u&'\\asdf'", true);
            analyzer.ParseQuery("SELECT p FROM Photo p WHERE p.Description = 'Smith\\'s family'", true);
            analyzer.ParseQuery("select E'\\''");
            analyzer.ParseQuery("ALTER DEFAULT PRIVILEGES IN SCHEMA myschema REVOKE GRANT OPTION FOR SELECT ON TABLES FROM internal", true);
            analyzer.ParseQuery("select A.D.B<C>.F.G<A>(d,A<d>.D<s>())");
            analyzer.ParseQuery("select 2<3 and 3>4");
            analyzer.ParseQuery("select 2<A.B and A.B>(A<B>(F))");
            analyzer.ParseQuery("select fr<A.B>(A<B>(F))");
            analyzer.ParseQuery("select F(G<A,B>(7))");
            analyzer.ParseQuery("select F(G<A,B>>(7))");
            analyzer.ParseQuery("select F(G<A,B>7)");
            analyzer.ParseQuery("select F(G<A,B>>7)");
        }

        [Test]
        public static void MultithreadedTest() {
            String[] queries =
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
            Thread[] threads = new Thread[queries.Length];
            for (int i = 0; i < queries.Length; i++) {
                threads[i] = new Thread(CallParser);
                threads[i].Start(queries[i]);
            }
            for (int i = 0; i < queries.Length; i++)
                threads[i].Join();
#if DEBUG
            Assert.IsFalse(UnmanagedParserInterface.DumpMemoryLeaks());
#endif
            Console.WriteLine(queries.Length + " queries are executed in " + queries.Length + " threads.");
        }

        private static void CallParser(object query) {
            ParserAnalyzer analyzer = new ParserAnalyzer();
            analyzer.ParseQuery((String)query);
        }
    }
}
