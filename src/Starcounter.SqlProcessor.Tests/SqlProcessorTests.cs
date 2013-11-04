using System;
using NUnit.Framework;
using Starcounter.SqlProcessor;

namespace Starcounter.SqlProcessor.Tests {
    [TestFixture]
    public class SqlProcessorTests {

        private static unsafe void ProcessQuery(uint expectedError, string query) {
            uint err = SqlProcessor.scsql_process_query(query);
            if (err != 0) {
                ScError* fullError = SqlProcessor.scsql_get_error();
                uint errF = fullError->scerrorcode;
                SqlProcessor.scsql_free_memory();
                Assert.AreEqual(err, errF);
            }
            Assert.AreEqual(expectedError, err);
        }

        [Test]
        public static void HelloProcessor() {
            ProcessQuery(0, "select * from user where col = 1");
            ProcessQuery(7021, "");
            ProcessQuery(7021, "select");
            ProcessQuery(0, "select * from user");
        }

        [Test]
        public static void SqlSyntax() {
            ProcessQuery(0, "SELECT 1");
            ProcessQuery(0, "SELECT Abc FROM tbL");
            ProcessQuery(0, "SELECT Å");
            ProcessQuery(0, "SELECT У");
            ProcessQuery(0, "SELECT -(-1.2E+02), -(-1.2), -(+1), +(-2), -(+.2E+02)");
            ProcessQuery(0, "select a -- /* asdfasdf asdlfkjaskldfj");
            ProcessQuery(0, "select b /* asdfasd -- lkjlkj */");
            ProcessQuery(7021, "select b /* asdfasd -- lkjlkj ");
            ProcessQuery(0, "select u from Accounttest.auser u");
            ProcessQuery(7021, "select ");
            ProcessQuery(7021, "selec");
            ProcessQuery(0, "select * from t1");
            ProcessQuery(7021, "select sd order byd");
            ProcessQuery(7021, "select * from tble limit 1, 2");
            ProcessQuery(7021, "(SELECT foo ORDER BY bar) ORDER BY baz");
            ProcessQuery(0, "SELECT * from table1 offsetkey 4");
            ProcessQuery(7021, "select * from [table] where [offset] = 3");
            ProcessQuery(0, "select * from tbl where col = $1");
            ProcessQuery(7021, "select * from table");
            ProcessQuery(0, "select * from \"table\"");
            //ProcessQuery(7021, "SELECT E'\\xDEADBEEF'"); // BUG here, #1237
            ProcessQuery(0, "select * from tbl option index (tbl indx1)");
            ProcessQuery(7021, "select * from tbl option index tbl indx1");
            ProcessQuery(7021, "select * from tbl option index tbl");
            ProcessQuery(0, "select * from tbl1 union select * from tbl option index (tbl indx1)");// MUST FAIL
            ProcessQuery(0, "select * from tbl1 union (select * from tbl option index (tbl indx1))");
            ProcessQuery(7021, "select * from tbl option join order tbl");
            ProcessQuery(0, "select * from tbl option join order (tbl)");
            ProcessQuery(0, "select * from tbl option index (tbl indx1), join order (tbl)");
            ProcessQuery(0, "SELECT d.Name, e.LastName FROM Department d JOIN Employee e ON e.Department = d WHERE e.FirstName = 'Bob' OPTION JOIN ORDER (e,d)");
            ProcessQuery(0, "select (f.d()).d.f");
            ProcessQuery(0, "select f.d().f.d.s()");
            ProcessQuery(0, "select (f.d()).d.f()");
            //ProcessQuery(7021, "select cast(col as typ<typ2>) from tble"); // BUG here
            //ProcessQuery(7021, "select nm.typ<typ2>.fn()"); // BUG here
            ProcessQuery(0, "select $1.f()");
            ProcessQuery(0, "select cast(col as type).ref1.ref2 from tbl1");
            ProcessQuery(0, "select I(A)");
            ProcessQuery(7021, "select (I(A).F()");
            ProcessQuery(7021, "select (I(A).J(A,B).F()");
            ProcessQuery(7021, "select (I(A,B,C).J(A,B).F(A)()");
            ProcessQuery(0, "select I.J(A,B).F()");
            ProcessQuery(0, "select I.J(A,B).F<A>()");
            ProcessQuery(0, "select I.J.F()");
            ProcessQuery(0, "select I.J(A,B).F<A<C>,T>()");
            ProcessQuery(0, "select I.J(A)");
            ProcessQuery(0, "select I<A>()");
            ProcessQuery(0, "select I.J<A>.f<A>(g)");
            ProcessQuery(0, "select (I.J(a)).T<K>(v)");
            //ProcessQuery(7021, "select a < b > c"); // BUG here
            //ProcessQuery(7021, "select a < b , c >"); // BUG here
            ProcessQuery(7021, "select f()()");
            ProcessQuery(0, "select f<typ1>()");
            ProcessQuery(0, "select cast(a+b as c)");
            ProcessQuery(0, "select cast(a+b as int)");
            ProcessQuery(7021, "select cast(a+b as c.d(i)");
            ProcessQuery(7021, "select cast(a+b as c.d(i))");
            ProcessQuery(0, "select cast(a+b as c.d[])");
            ProcessQuery(0, "select cast(a+b as c.d<i,d>)");
            ProcessQuery(0, "select cast(a+b as c.d<i<g>,d>)");
            ProcessQuery(0, "select DATE '2012-02-02'");
            ProcessQuery(0, "select col1.fn().col2 = DATE '2012-02-02' from ns1.tbl1");
            ProcessQuery(0, "select ?.f()");
            ProcessQuery(0, "select * from tbl where col1 = ?");
            ProcessQuery(7021, "select binary 'x'");
            ProcessQuery(0, "select BINARY 'AB14093FE'");
            ProcessQuery(0, "SELECT e FROM Employee e WHERE e.Commission = TRUE");
            ProcessQuery(0, "SELECT e FROM Employee e WHERE e.Salary = 5000");
            ProcessQuery(0, "SELECT e FROM Employee e WHERE e.Salary = 5000.00");
            ProcessQuery(0, "SELECT e FROM Employee e WHERE e.Salary = 5.0E3");
            ProcessQuery(0, "SELECT p FROM Photo p WHERE p.Description = 'Smith''s family'");
            ProcessQuery(0, "SELECT e FROM Employee e WHERE e.HireDate = DATE '2006-11-01'");
            ProcessQuery(0, "SELECT e FROM Employee e WHERE e.HireDate = TIMESTAMP '2006-11-01 00:00:00'");
            ProcessQuery(0, "SELECT l FROM Department d WHERE d.BinaryId = BINARY 'D91FA24E19FB065A'");
            ProcessQuery(0, "SELECT e FROM Employee e WHERE e = OBJECT 123");
            ProcessQuery(0, "SELECT * from tbl where col1 STARTS WITH 'Start with me'");
            ProcessQuery(0, "select t from tbl t where str starts with ?");
            ProcessQuery(0, "select getextension<myperson>() from example.person");
            ProcessQuery(0, "select e from example.employee e where e.getextension<example.myperson>().myname =?");
            ProcessQuery(0, "select u from User u; select d from Department d");
            ProcessQuery(7021, "select u from User u where u.name = u&'\\asdf'");
            ProcessQuery(7021, "SELECT p FROM Photo p WHERE p.Description = 'Smith\\'s family'");
            ProcessQuery(7021, "select E'\\''");
            ProcessQuery(7021, "ALTER DEFAULT PRIVILEGES IN SCHEMA myschema REVOKE GRANT OPTION FOR SELECT ON TABLES FROM internal");
            ProcessQuery(0, "select A.D.B<C>.F.G<A>(d,A<d>.D<s>())");
            ProcessQuery(0, "select 2<3 and 3>4");
            ProcessQuery(0, "select 2<A.B and A.B>(A<B>(F))");
            ProcessQuery(0, "select fr<A.B>(A<B>(F))");
            ProcessQuery(0, "select F(G<A,B>(7))");
            ProcessQuery(0, "select F(G<A,B>>(7))");
            ProcessQuery(0, "select F(G<A,B>7)");
            ProcessQuery(0, "select F(G<A,B>>7)");
            ProcessQuery(0, "select A+ <B>F()");
            ProcessQuery(0, "select A+ <B>F(A,B)");
            ProcessQuery(0, "select A+ D<A,F>.<B>F()");
            ProcessQuery(0, "select A+ M.D.<B>F(A,B)");
            ProcessQuery(0, "select G<A,B>(7)");
            ProcessQuery(0, "select nullif(G<A,B>(7))");
            ProcessQuery(0, "DROP INDEX UserLN ON AccountTest.user");
            ProcessQuery(0, "create index SalaryEmployee_NSalaryInt64 on SqlTest.EmployeeDb.SalaryEmployee (NSalaryInt64)");
            ProcessQuery(0, "SELECT prop1, prop2 FROM table1");
            ProcessQuery(0, "SELECT ROW(prop1, prop2) FROM table1");
            ProcessQuery(0, "SELECT ROW(valueProp) FROM table1");
            ProcessQuery(0, "SELECT valueProp FROM table1");
            ProcessQuery(0, "select * from t [ 2]");
            ProcessQuery(0, "select * from t \"table\"");
            ProcessQuery(0, "select t.u as USER from t as ORDER");
            ProcessQuery(0, "select order() from order() as order order by order()");
            ProcessQuery(0, "select order from order as order order by order");
            ProcessQuery(0, "select a<b, a<c from t");
            ProcessQuery(0, "SELECT USER");
            ProcessQuery(0, "select user as user from user.user() order by user");
            ProcessQuery(0, "select order as order from order.order() order by order");
            ProcessQuery(0, "select u from user u group by u");
            ProcessQuery(0, "select group as group from group.group() group by group order by group ");
            ProcessQuery(0, "select window from window.window().window group by window window abc as (cdv) order by window");
            ProcessQuery(0, "select union from union.union().union group by union union select union from union order by union");
            ProcessQuery(0, "select option as option from option.option().option as option group by option OPTION JOIN ORDER (e, m), INDEX (e MyIndexOnLastName), INDEX (m MyIndexOnFirstName)");
            ProcessQuery(0, "select cast(al.col as cl)");
            ProcessQuery(0, "select cast(al.col as cl<tp>)");
            ProcessQuery(0, "select cast(al.col as ns.cl<tp>)");
            ProcessQuery(0, "drop index indx on ns.tbl1");
            ProcessQuery(0, "drop index indx on ns.tbl1, ind2 on tbl2, inx3 on ns1.ns2.tbl3<t>");
            ProcessQuery(0, "select typeof(nm.nm2.type<A,B>).FullName fullname");
            ProcessQuery(0, "select CURRENT_DATE");
            ProcessQuery(0, "select CURRENT_TIME, CURRENT_TIME(2)");
            ProcessQuery(0, "select CURRENT_TIMESTAMP, CURRENT_TIMESTAMP(2)");
            ProcessQuery(0, "select LOCALTIME, LOCALTIME(2)");
            ProcessQuery(0, "select LOCALTIMESTAMP, LOCALTIMESTAMP(2)");
            ProcessQuery(0, "select CURRENT_ROLE");
            ProcessQuery(0, "select CURRENT_USER");
            ProcessQuery(0, "select SESSION_USER");
            ProcessQuery(0, "select CURRENT_CATALOG");
            ProcessQuery(0, "select NULLIF(a+b,B+A)");
            ProcessQuery(0, "select COALESCE(a,b,c)");
            ProcessQuery(0, "select GREATEST(a+b,b+c,c+a)");
            ProcessQuery(0, "select LEAST(a+b,b+c,c+a)");
            ProcessQuery(0, "select foreign, primary, where, column from index, limit where use");
            ProcessQuery(0, "select analyze, analyse");
            ProcessQuery(0, "select absolute, aggregate, also, assertion, assignment, attribute, backward, called");
            ProcessQuery(0, "select\r\na\tfrom/* comment ** comment*/ \ft -- Comment\nwhere k>2");
            ProcessQuery(0, "select å from öl");
            ProcessQuery(0, "select \u0066");
            ProcessQuery(0, "SELECT -(-1.2E+02), -(-1.2), -(+1), +(-2), -(+.2E+02)");
            ProcessQuery(0, "select * from tbl where col like '\\n and \\u and \\\\'");
            ProcessQuery(0, "select * from tbl where a >= b and c != d and e <> k /* comment */ and tt -- kk */");
            ProcessQuery(0, "select 'ghf k kjn''jgk''''k''kjkjh'");
            ProcessQuery(0, "select B'010010', X'FD13A', N'Text'");
            //ProcessQuery(7021, "select B'010A010'"); // BUG here
            //ProcessQuery(7021, "select X'FID13A'"); // BUG here
            ProcessQuery(0, "select 'Константа' from \"Таблица\" таблица");
            ProcessQuery(0, "select 9223372036854775807");
            ProcessQuery(0, "select 345, -234, 123 from mytable t where c = -2343 and t = 4520904907");
            ProcessQuery(0, "select 324.23423, 342, -234.234e+12");
            ProcessQuery(7021, "asdfqad");
            ProcessQuery(7021, "1342");
            ProcessQuery(7021, "()");
            ProcessQuery(7021, "");
            ProcessQuery(7021, " ");
            ProcessQuery(7021, "\n");
            ProcessQuery(0, "select a from account a where accountid > ? fetch ?");
            ProcessQuery(0, "select a from account a where accountid > ? fetch ? offset ?");
            ProcessQuery(0, "select a from account a where accountid > ? fetch ? offsetkey ?");
            ProcessQuery(0, "select a from account a join user u where a.client = u and u.prop = ?");
            ProcessQuery(0, "select asdfas from tbl1 join tbl2 join tbl3 on tbl2.f = tbl3.f join (tbl4 join tbl5 on tbl4.s = tbl5.s) on tbl2.k = tbl4.k where tbl1.m = tbl4.m");
            ProcessQuery(0, "select asdfas from tbl1 join (tbl2 join tbl3) on tbl2.f = tbl3.f join (tbl4 join tbl5 on tbl4.s = tbl5.s) on tbl2.k = tbl4.k where tbl1.m = tbl4.m");
            //ProcessQuery(7021, "select u1 from user u1 join user u2 on u1 != u2 and u1.useridnr = u2.useridn2 + ?"); // BUG here
            ProcessQuery(0, "select ?a where a=2");
            ProcessQuery(0, "select $1a");
            ProcessQuery(0, "select?a");
            ProcessQuery(7021, "select$1a");
        }
    }
}
