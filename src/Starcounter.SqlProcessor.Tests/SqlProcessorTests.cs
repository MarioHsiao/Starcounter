using System;
using NUnit.Framework;
using Starcounter.SqlProcessor;

namespace Starcounter.SqlProcessor.Tests {
    [TestFixture]
    public class SqlProcessorTests {
        [Test]
        public static void HelloProcessor() {
            Assert.AreEqual(7021, SqlProcessor.CallSqlProcessor(""));
            Assert.AreEqual(7021, SqlProcessor.CallSqlProcessor("select"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select * from user"));
        }

        [Test]
        public static void SqlSyntax() {
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("SELECT 1"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("SELECT Abc FROM tbL"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("SELECT Å"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("SELECT У"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("SELECT -(-1.2E+02), -(-1.2), -(+1), +(-2), -(+.2E+02)"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select a -- /* asdfasdf asdlfkjaskldfj"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select b /* asdfasd -- lkjlkj */"));
            Assert.AreEqual(7021, SqlProcessor.CallSqlProcessor("select b /* asdfasd -- lkjlkj "));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select u from Accounttest.auser u"));
            Assert.AreEqual(7021, SqlProcessor.CallSqlProcessor("select "));
            Assert.AreEqual(7021, SqlProcessor.CallSqlProcessor("selec"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select * from t1"));
            Assert.AreEqual(7021, SqlProcessor.CallSqlProcessor("select sd order byd"));
            Assert.AreEqual(7021, SqlProcessor.CallSqlProcessor("select * from tble limit 1, 2"));
            Assert.AreEqual(7021, SqlProcessor.CallSqlProcessor("(SELECT foo ORDER BY bar) ORDER BY baz"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("SELECT * from table1 offsetkey 4"));
            Assert.AreEqual(7021, SqlProcessor.CallSqlProcessor("select * from [table] where [offset] = 3"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select * from tbl where col = $1"));
            Assert.AreEqual(7021, SqlProcessor.CallSqlProcessor("select * from table"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select * from \"table\""));
            //Assert.AreEqual(7021, SqlProcessor.CallSqlProcessor("SELECT E'\\xDEADBEEF'")); // BUG here, #1237
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select * from tbl option index (tbl indx1)"));
            Assert.AreEqual(7021, SqlProcessor.CallSqlProcessor("select * from tbl option index tbl indx1"));
            Assert.AreEqual(7021, SqlProcessor.CallSqlProcessor("select * from tbl option index tbl"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select * from tbl1 union select * from tbl option index (tbl indx1)"));// MUST FAIL
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select * from tbl1 union (select * from tbl option index (tbl indx1))"));
            Assert.AreEqual(7021, SqlProcessor.CallSqlProcessor("select * from tbl option join order tbl"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select * from tbl option join order (tbl)"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select * from tbl option index (tbl indx1), join order (tbl)"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("SELECT d.Name, e.LastName FROM Department d JOIN Employee e ON e.Department = d WHERE e.FirstName = 'Bob' OPTION JOIN ORDER (e,d)"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select (f.d()).d.f"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select f.d().f.d.s()"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select (f.d()).d.f()"));
            //Assert.AreEqual(7021, SqlProcessor.CallSqlProcessor("select cast(col as typ<typ2>) from tble")); // BUG here
            //Assert.AreEqual(7021, SqlProcessor.CallSqlProcessor("select nm.typ<typ2>.fn()")); // BUG here
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select $1.f()"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select cast(col as type).ref1.ref2 from tbl1"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select I(A)"));
            Assert.AreEqual(7021, SqlProcessor.CallSqlProcessor("select (I(A).F()"));
            Assert.AreEqual(7021, SqlProcessor.CallSqlProcessor("select (I(A).J(A,B).F()"));
            Assert.AreEqual(7021, SqlProcessor.CallSqlProcessor("select (I(A,B,C).J(A,B).F(A)()"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select I.J(A,B).F()"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select I.J(A,B).F<A>()"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select I.J.F()"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select I.J(A,B).F<A<C>,T>()"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select I.J(A)"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select I<A>()"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select I.J<A>.f<A>(g)"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select (I.J(a)).T<K>(v)"));
            //Assert.AreEqual(7021, SqlProcessor.CallSqlProcessor("select a < b > c")); // BUG here
            //Assert.AreEqual(7021, SqlProcessor.CallSqlProcessor("select a < b , c >")); // BUG here
            Assert.AreEqual(7021, SqlProcessor.CallSqlProcessor("select f()()"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select f<typ1>()"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select cast(a+b as c)"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select cast(a+b as int)"));
            Assert.AreEqual(7021, SqlProcessor.CallSqlProcessor("select cast(a+b as c.d(i)"));
            Assert.AreEqual(7021, SqlProcessor.CallSqlProcessor("select cast(a+b as c.d(i))"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select cast(a+b as c.d[])"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select cast(a+b as c.d<i,d>)"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select cast(a+b as c.d<i<g>,d>)"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select DATE '2012-02-02'"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select col1.fn().col2 = DATE '2012-02-02' from ns1.tbl1"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select ?.f()"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select * from tbl where col1 = ?"));
            Assert.AreEqual(7021, SqlProcessor.CallSqlProcessor("select binary 'x'"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select BINARY 'AB14093FE'"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("SELECT e FROM Employee e WHERE e.Commission = TRUE"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("SELECT e FROM Employee e WHERE e.Salary = 5000"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("SELECT e FROM Employee e WHERE e.Salary = 5000.00"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("SELECT e FROM Employee e WHERE e.Salary = 5.0E3"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("SELECT p FROM Photo p WHERE p.Description = 'Smith''s family'"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("SELECT e FROM Employee e WHERE e.HireDate = DATE '2006-11-01'"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("SELECT e FROM Employee e WHERE e.HireDate = TIMESTAMP '2006-11-01 00:00:00'"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("SELECT l FROM Department d WHERE d.BinaryId = BINARY 'D91FA24E19FB065A'"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("SELECT e FROM Employee e WHERE e = OBJECT 123"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("SELECT * from tbl where col1 STARTS WITH 'Start with me'"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select t from tbl t where str starts with ?"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select getextension<myperson>() from example.person"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select e from example.employee e where e.getextension<example.myperson>().myname =?"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select u from User u; select d from Department d"));
            Assert.AreEqual(7021, SqlProcessor.CallSqlProcessor("select u from User u where u.name = u&'\\asdf'"));
            Assert.AreEqual(7021, SqlProcessor.CallSqlProcessor("SELECT p FROM Photo p WHERE p.Description = 'Smith\\'s family'"));
            Assert.AreEqual(7021, SqlProcessor.CallSqlProcessor("select E'\\''"));
            Assert.AreEqual(7021, SqlProcessor.CallSqlProcessor("ALTER DEFAULT PRIVILEGES IN SCHEMA myschema REVOKE GRANT OPTION FOR SELECT ON TABLES FROM internal"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select A.D.B<C>.F.G<A>(d,A<d>.D<s>())"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select 2<3 and 3>4"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select 2<A.B and A.B>(A<B>(F))"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select fr<A.B>(A<B>(F))"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select F(G<A,B>(7))"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select F(G<A,B>>(7))"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select F(G<A,B>7)"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select F(G<A,B>>7)"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select A+ <B>F()"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select A+ <B>F(A,B)"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select A+ D<A,F>.<B>F()"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select A+ M.D.<B>F(A,B)"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select G<A,B>(7)"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select nullif(G<A,B>(7))"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("DROP INDEX UserLN ON AccountTest.user"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("create index SalaryEmployee_NSalaryInt64 on SqlTest.EmployeeDb.SalaryEmployee (NSalaryInt64)"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("SELECT prop1, prop2 FROM table1"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("SELECT ROW(prop1, prop2) FROM table1"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("SELECT ROW(valueProp) FROM table1"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("SELECT valueProp FROM table1"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select * from t [ 2]"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select * from t \"table\""));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select t.u as USER from t as ORDER"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select order() from order() as order order by order()"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select order from order as order order by order"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select a<b, a<c from t"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("SELECT USER"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select user as user from user.user() order by user"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select order as order from order.order() order by order"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select u from user u group by u"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select group as group from group.group() group by group order by group "));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select window from window.window().window group by window window abc as (cdv) order by window"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select union from union.union().union group by union union select union from union order by union"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select option as option from option.option().option as option group by option OPTION JOIN ORDER (e, m), INDEX (e MyIndexOnLastName), INDEX (m MyIndexOnFirstName)"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select cast(al.col as cl)"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select cast(al.col as cl<tp>)"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select cast(al.col as ns.cl<tp>)"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("drop index indx on ns.tbl1"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("drop index indx on ns.tbl1, ind2 on tbl2, inx3 on ns1.ns2.tbl3<t>"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select typeof(nm.nm2.type<A,B>).FullName fullname"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select CURRENT_DATE"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select CURRENT_TIME, CURRENT_TIME(2)"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select CURRENT_TIMESTAMP, CURRENT_TIMESTAMP(2)"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select LOCALTIME, LOCALTIME(2)"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select LOCALTIMESTAMP, LOCALTIMESTAMP(2)"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select CURRENT_ROLE"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select CURRENT_USER"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select SESSION_USER"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select CURRENT_CATALOG"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select NULLIF(a+b,B+A)"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select COALESCE(a,b,c)"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select GREATEST(a+b,b+c,c+a)"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select LEAST(a+b,b+c,c+a)"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select foreign, primary, where, column from index, limit where use"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select analyze, analyse"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select absolute, aggregate, also, assertion, assignment, attribute, backward, called"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select\r\na\tfrom/* comment ** comment*/ \ft -- Comment\nwhere k>2"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select å from öl"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select \u0066"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("SELECT -(-1.2E+02), -(-1.2), -(+1), +(-2), -(+.2E+02)"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select * from tbl where col like '\\n and \\u and \\\\'"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select * from tbl where a >= b and c != d and e <> k /* comment */ and tt -- kk */"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select 'ghf k kjn''jgk''''k''kjkjh'"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select B'010010', X'FD13A', N'Text'"));
            //Assert.AreEqual(7021, SqlProcessor.CallSqlProcessor("select B'010A010'")); // BUG here
            //Assert.AreEqual(7021, SqlProcessor.CallSqlProcessor("select X'FID13A'")); // BUG here
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select 'Константа' from \"Таблица\" таблица"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select 9223372036854775807"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select 345, -234, 123 from mytable t where c = -2343 and t = 4520904907"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select 324.23423, 342, -234.234e+12"));
            Assert.AreEqual(7021, SqlProcessor.CallSqlProcessor("asdfqad"));
            Assert.AreEqual(7021, SqlProcessor.CallSqlProcessor("1342"));
            Assert.AreEqual(7021, SqlProcessor.CallSqlProcessor("()"));
            Assert.AreEqual(7021, SqlProcessor.CallSqlProcessor(""));
            Assert.AreEqual(7021, SqlProcessor.CallSqlProcessor(" "));
            Assert.AreEqual(7021, SqlProcessor.CallSqlProcessor("\n"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select a from account a where accountid > ? fetch ?"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select a from account a where accountid > ? fetch ? offset ?"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select a from account a where accountid > ? fetch ? offsetkey ?"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select a from account a join user u where a.client = u and u.prop = ?"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select asdfas from tbl1 join tbl2 join tbl3 on tbl2.f = tbl3.f join (tbl4 join tbl5 on tbl4.s = tbl5.s) on tbl2.k = tbl4.k where tbl1.m = tbl4.m"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select asdfas from tbl1 join (tbl2 join tbl3) on tbl2.f = tbl3.f join (tbl4 join tbl5 on tbl4.s = tbl5.s) on tbl2.k = tbl4.k where tbl1.m = tbl4.m"));
            //Assert.AreEqual(7021, SqlProcessor.CallSqlProcessor("select u1 from user u1 join user u2 on u1 != u2 and u1.useridnr = u2.useridn2 + ?")); // BUG here
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select ?a where a=2"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select $1a"));
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor("select?a"));
            Assert.AreEqual(7021, SqlProcessor.CallSqlProcessor("select$1a"));
        }
    }
}
