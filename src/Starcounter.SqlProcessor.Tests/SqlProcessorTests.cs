using System;
using NUnit.Framework;
using Starcounter.SqlProcessor;

namespace Starcounter.SqlProcessor.Tests {
    [TestFixture]
    public class SqlProcessorTests {

        const uint parseOK = Error.SCERRSQLNOTIMPLEMENTED;

        public static unsafe void ProcessQuery(uint expectedError, string query) {
            Exception ex = SqlProcessor.CallSqlProcessor(query);
            Assert.AreEqual(expectedError, ex.Data[ErrorCode.EC_TRANSPORT_KEY], ex.Message);
        }

        [Test]
        public static void HelloProcessor() {
            ProcessQuery(parseOK, "select * from user where col = 1");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select");
            ProcessQuery(parseOK, "select * from user");
        }

        [Test]
        public static void SqlSyntax() {
            ProcessQuery(parseOK, "SELECT 1");
            ProcessQuery(parseOK, "SELECT Abc FROM tbL");
            ProcessQuery(parseOK, "SELECT Å");
            ProcessQuery(parseOK, "SELECT У");
            ProcessQuery(parseOK, "SELECT -(-1.2E+02), -(-1.2), -(+1), +(-2), -(+.2E+02)");
            ProcessQuery(parseOK, "select a -- /* asdfasdf asdlfkjaskldfj");
            ProcessQuery(parseOK, "select b /* asdfasd -- lkjlkj */");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select b /* asdfasd -- lkjlkj ");
            ProcessQuery(parseOK, "select u from Accounttest.auser u");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select ");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "selec");
            ProcessQuery(parseOK, "select * from t1");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select sd od bd");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select sd order byd");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select * from tble limit 1, 2");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "(SELECT foo ORDER BY bar) ORDER BY baz");
            ProcessQuery(parseOK, "SELECT * from table1 offsetkey 4");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select * from [table] where [offset] = 3");
            ProcessQuery(parseOK, "select * from tbl where col = $1");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select * from table");
            ProcessQuery(parseOK, "select * from \"table\"");
            //ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "SELECT E'\\xDEADBEEF'"); // BUG here, #1237
            ProcessQuery(parseOK, "select * from tbl option index (tbl indx1)");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select * from tbl option index tbl indx1");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select * from tbl option index tbl");
            ProcessQuery(parseOK, "select * from tbl1 union select * from tbl option index (tbl indx1)");// MUST FAIL
            ProcessQuery(parseOK, "select * from tbl1 union (select * from tbl option index (tbl indx1))");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select * from tbl option join order tbl");
            ProcessQuery(parseOK, "select * from tbl option join order (tbl)");
            ProcessQuery(parseOK, "select * from tbl option index (tbl indx1), join order (tbl)");
            ProcessQuery(parseOK, "SELECT d.Name, e.LastName FROM Department d JOIN Employee e ON e.Department = d WHERE e.FirstName = 'Bob' OPTION JOIN ORDER (e,d)");
            ProcessQuery(parseOK, "select (f.d()).d.f");
            ProcessQuery(parseOK, "select f.d().f.d.s()");
            ProcessQuery(parseOK, "select (f.d()).d.f()");
            //ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select cast(col as typ<typ2>) from tble"); // BUG here
            //ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select nm.typ<typ2>.fn()"); // BUG here
            ProcessQuery(parseOK, "select $1.f()");
            ProcessQuery(parseOK, "select cast(col as type).ref1.ref2 from tbl1");
            ProcessQuery(parseOK, "select I(A)");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select (I(A).F()");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select (I(A).J(A,B).F()");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select (I(A,B,C).J(A,B).F(A)()");
            ProcessQuery(parseOK, "select I.J(A,B).F()");
            ProcessQuery(parseOK, "select I.J(A,B).F<A>()");
            ProcessQuery(parseOK, "select I.J.F()");
            ProcessQuery(parseOK, "select I.J(A,B).F<A<C>,T>()");
            ProcessQuery(parseOK, "select I.J(A)");
            ProcessQuery(parseOK, "select I<A>()");
            ProcessQuery(parseOK, "select I.J<A>.f<A>(g)");
            ProcessQuery(parseOK, "select (I.J(a)).T<K>(v)");
            //ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select a < b > c"); // BUG here
            //ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select a < b , c >"); // BUG here
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select f()()");
            ProcessQuery(parseOK, "select f<typ1>()");
            ProcessQuery(parseOK, "select cast(a+b as c)");
            ProcessQuery(parseOK, "select cast(a+b as int)");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select cast(a+b as c.d(i)");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select cast(a+b as c.d(i))");
            ProcessQuery(parseOK, "select cast(a+b as c.d[])");
            ProcessQuery(parseOK, "select cast(a+b as c.d<i,d>)");
            ProcessQuery(parseOK, "select cast(a+b as c.d<i<g>,d>)");
            ProcessQuery(parseOK, "select DATE '2012-02-02'");
            ProcessQuery(parseOK, "select col1.fn().col2 = DATE '2012-02-02' from ns1.tbl1");
            ProcessQuery(parseOK, "select ?.f()");
            ProcessQuery(parseOK, "select * from tbl where col1 = ?");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select binary 'x'");
            ProcessQuery(parseOK, "select BINARY 'AB14093FE'");
            ProcessQuery(parseOK, "SELECT e FROM Employee e WHERE e.Commission = TRUE");
            ProcessQuery(parseOK, "SELECT e FROM Employee e WHERE e.Salary = 5000");
            ProcessQuery(parseOK, "SELECT e FROM Employee e WHERE e.Salary = 5000.00");
            ProcessQuery(parseOK, "SELECT e FROM Employee e WHERE e.Salary = 5.0E3");
            ProcessQuery(parseOK, "SELECT p FROM Photo p WHERE p.Description = 'Smith''s family'");
            ProcessQuery(parseOK, "SELECT e FROM Employee e WHERE e.HireDate = DATE '2006-11-01'");
            ProcessQuery(parseOK, "SELECT e FROM Employee e WHERE e.HireDate = TIMESTAMP '2006-11-01 00:00:00'");
            ProcessQuery(parseOK, "SELECT l FROM Department d WHERE d.BinaryId = BINARY 'D91FA24E19FB065A'");
            ProcessQuery(parseOK, "SELECT e FROM Employee e WHERE e = OBJECT 123");
            ProcessQuery(parseOK, "SELECT * from tbl where col1 STARTS WITH 'Start with me'");
            ProcessQuery(parseOK, "select t from tbl t where str starts with ?");
            ProcessQuery(parseOK, "select getextension<myperson>() from example.person");
            ProcessQuery(parseOK, "select e from example.employee e where e.getextension<example.myperson>().myname =?");
            ProcessQuery(parseOK, "select u from User u; select d from Department d");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select u from User u where u.name = u&'\\asdf'");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "SELECT p FROM Photo p WHERE p.Description = 'Smith\\'s family'");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select E'\\''");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "ALTER DEFAULT PRIVILEGES IN SCHEMA myschema REVOKE GRANT OPTION FOR SELECT ON TABLES FROM internal");
            ProcessQuery(parseOK, "select A.D.B<C>.F.G<A>(d,A<d>.D<s>())");
            ProcessQuery(parseOK, "select 2<3 and 3>4");
            ProcessQuery(parseOK, "select 2<A.B and A.B>(A<B>(F))");
            ProcessQuery(parseOK, "select fr<A.B>(A<B>(F))");
            ProcessQuery(parseOK, "select F(G<A,B>(7))");
            ProcessQuery(parseOK, "select F(G<A,B>>(7))");
            ProcessQuery(parseOK, "select F(G<A,B>7)");
            ProcessQuery(parseOK, "select F(G<A,B>>7)");
            ProcessQuery(parseOK, "select A+ <B>F()");
            ProcessQuery(parseOK, "select A+ <B>F(A,B)");
            ProcessQuery(parseOK, "select A+ D<A,F>.<B>F()");
            ProcessQuery(parseOK, "select A+ M.D.<B>F(A,B)");
            ProcessQuery(parseOK, "select G<A,B>(7)");
            ProcessQuery(parseOK, "select nullif(G<A,B>(7))");
            ProcessQuery(parseOK, "DROP INDEX UserLN ON AccountTest.user");
            ProcessQuery(parseOK, "create index SalaryEmployee_NSalaryInt64 on SqlTest.EmployeeDb.SalaryEmployee (NSalaryInt64)");
            ProcessQuery(parseOK, "SELECT prop1, prop2 FROM table1");
            ProcessQuery(parseOK, "SELECT ROW(prop1, prop2) FROM table1");
            ProcessQuery(parseOK, "SELECT ROW(valueProp) FROM table1");
            ProcessQuery(parseOK, "SELECT valueProp FROM table1");
            ProcessQuery(parseOK, "select * from t [ 2]");
            ProcessQuery(parseOK, "select * from t \"table\"");
            ProcessQuery(parseOK, "select t.u as USER from t as ORDER");
            ProcessQuery(parseOK, "select order() from order() as order order by order()");
            ProcessQuery(parseOK, "select order from order as order order by order");
            ProcessQuery(parseOK, "select a<b, a<c from t");
            ProcessQuery(parseOK, "SELECT USER");
            ProcessQuery(parseOK, "select user as user from user.user() order by user");
            ProcessQuery(parseOK, "select order as order from order.order() order by order");
            ProcessQuery(parseOK, "select u from user u group by u");
            ProcessQuery(parseOK, "select group as group from group.group() group by group order by group ");
            ProcessQuery(parseOK, "select window from window.window().window group by window window abc as (cdv) order by window");
            ProcessQuery(parseOK, "select union from union.union().union group by union union select union from union order by union");
            ProcessQuery(parseOK, "select option as option from option.option().option as option group by option OPTION JOIN ORDER (e, m), INDEX (e MyIndexOnLastName), INDEX (m MyIndexOnFirstName)");
            ProcessQuery(parseOK, "select cast(al.col as cl)");
            ProcessQuery(parseOK, "select cast(al.col as cl<tp>)");
            ProcessQuery(parseOK, "select cast(al.col as ns.cl<tp>)");
            ProcessQuery(parseOK, "drop index indx on ns.tbl1");
            ProcessQuery(parseOK, "drop index indx on ns.tbl1, ind2 on tbl2, inx3 on ns1.ns2.tbl3<t>");
            ProcessQuery(parseOK, "select typeof(nm.nm2.type<A,B>).FullName fullname");
            ProcessQuery(parseOK, "select CURRENT_DATE");
            ProcessQuery(parseOK, "select CURRENT_TIME, CURRENT_TIME(2)");
            ProcessQuery(parseOK, "select CURRENT_TIMESTAMP, CURRENT_TIMESTAMP(2)");
            ProcessQuery(parseOK, "select LOCALTIME, LOCALTIME(2)");
            ProcessQuery(parseOK, "select LOCALTIMESTAMP, LOCALTIMESTAMP(2)");
            ProcessQuery(parseOK, "select CURRENT_ROLE");
            ProcessQuery(parseOK, "select CURRENT_USER");
            ProcessQuery(parseOK, "select SESSION_USER");
            ProcessQuery(parseOK, "select CURRENT_CATALOG");
            ProcessQuery(parseOK, "select NULLIF(a+b,B+A)");
            ProcessQuery(parseOK, "select COALESCE(a,b,c)");
            ProcessQuery(parseOK, "select GREATEST(a+b,b+c,c+a)");
            ProcessQuery(parseOK, "select LEAST(a+b,b+c,c+a)");
            ProcessQuery(parseOK, "select foreign, primary, where, column from index, limit where use");
            ProcessQuery(parseOK, "select analyze, analyse");
            ProcessQuery(parseOK, "select absolute, aggregate, also, assertion, assignment, attribute, backward, called");
            ProcessQuery(parseOK, "select\r\na\tfrom/* comment ** comment*/ \ft -- Comment\nwhere k>2");
            ProcessQuery(parseOK, "select å from öl");
            ProcessQuery(parseOK, "select \u0066");
            ProcessQuery(parseOK, "SELECT -(-1.2E+02), -(-1.2), -(+1), +(-2), -(+.2E+02)");
            ProcessQuery(parseOK, "select * from tbl where col like '\\n and \\u and \\\\'");
            ProcessQuery(parseOK, "select * from tbl where a >= b and c != d and e <> k /* comment */ and tt -- kk */");
            ProcessQuery(parseOK, "select 'ghf k kjn''jgk''''k''kjkjh'");
            ProcessQuery(parseOK, "select B'010010', X'FD13A', N'Text'");
            //ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select B'010A010'"); // BUG here
            //ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select X'FID13A'"); // BUG here
            ProcessQuery(parseOK, "select 'Константа' from \"Таблица\" таблица");
            ProcessQuery(parseOK, "select 9223372036854775807");
            ProcessQuery(parseOK, "select 345, -234, 123 from mytable t where c = -2343 and t = 4520904907");
            ProcessQuery(parseOK, "select 324.23423, 342, -234.234e+12");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "asdfqad");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "1342");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "()");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, " ");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "\n");
            ProcessQuery(parseOK, "select a from account a where accountid > ? fetch ?");
            ProcessQuery(parseOK, "select a from account a where accountid > ? fetch ? offset ?");
            ProcessQuery(parseOK, "select a from account a where accountid > ? fetch ? offsetkey ?");
            ProcessQuery(parseOK, "select a from account a join user u where a.client = u and u.prop = ?");
            ProcessQuery(parseOK, "select asdfas from tbl1 join tbl2 join tbl3 on tbl2.f = tbl3.f join (tbl4 join tbl5 on tbl4.s = tbl5.s) on tbl2.k = tbl4.k where tbl1.m = tbl4.m");
            ProcessQuery(parseOK, "select asdfas from tbl1 join (tbl2 join tbl3) on tbl2.f = tbl3.f join (tbl4 join tbl5 on tbl4.s = tbl5.s) on tbl2.k = tbl4.k where tbl1.m = tbl4.m");
            //ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select u1 from user u1 join user u2 on u1 != u2 and u1.useridnr = u2.useridn2 + ?"); // BUG here
            ProcessQuery(parseOK, "select ?a where a=2");
            ProcessQuery(parseOK, "select $1a");
            ProcessQuery(parseOK, "select?a");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select$1a");
            ProcessQuery(parseOK, "select 'System.Reflection.TargetInvocationException: Obiekt docelowy wywoÅ‚ania zgÅ‚osiÅ‚ wyjÄ…tek'");
            ProcessQuery(parseOK, "select NULL, 231");
            ProcessQuery(parseOK, "SELECT d.Name, e.LastName FROM Department d JOIN Employee e ON e.Department = d WHERE e.FirstName = 'Bob' OPTION JOIN ORDER (e,d)");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "SELECT d.Name, e.LastName FROM Department d JOIN Employee e ON e.Department = d WHERE e.FirstName = 'Bob' OPTION JOIN ORDERs (e,d)");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "SELECT d.Name, e.LastName FROM Department d JOIN Employee e ON e.Department = d WHERE e.FirstName = 'Bob' OPTION JOIN dORDER (e,d)");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "SELECT d.Name, e.LastName FROM Department d JOIN Employee e ON e.Department = d WHERE e.FirstName = 'Bob' OPTION dJOIN ORDER (e,d)");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "SELECT d.Name, e.LastName FROM Department d JOIN Employee e ON e.Department = d WHERE e.FirstName = 'Bob' OPTION JOINs ORDER (e,d)");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "SELECT d.Name, e.LastName FROM Department d JOIN Employee e ON e.Department = d WHERE e.FirstName = 'Bob' OPTION JOINORDER (e,d)");
            ProcessQuery(parseOK, "select t from myt t where t.\"when\" = ?");
            ProcessQuery(parseOK, "select t from myt t where t.\"when\" = $1");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select t from myt t where t.when = :1");
            ProcessQuery(parseOK, "select t from myt t where t.when = ?");
            ProcessQuery(parseOK, "select 1 * 2");
            ProcessQuery(parseOK, "select c from dsfa c where c.sadf * ? and 1 *c.sdfa");
            ProcessQuery(parseOK, "select t from myt t where t.column = $4");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select t from myt t where when = ?");
            ProcessQuery(parseOK, "SELECT e, m FROM Employee e JOIN Employee m ON e.Manager = m WHERE e.FirstName = 'John' AND e.LastName = 'Smith' AND m.FirstName = 'David' AND m.LastName = 'King' OPTION INDEX (e MyIndexOnLastName), INDEX(m MyIndexOnFirstName)");
            ProcessQuery(parseOK, "SELECT x.Name FROM Person a, Person x, Parent p WHERE a = p.property AND p.object = x AND a.Name = 'Alice'");
            ProcessQuery(parseOK, "SELECT a[Parent][Name] FROM Person a WHERE a[Name] = 'Alice'");
            ProcessQuery(parseOK, "SELECT a[Parent].Name FROM Person a WHERE a[Name] = 'Alice'");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "SELECT a[Parent]Name FROM Person a WHERE a[Name] = 'Alice'");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "SELECT a[Parent]Name] FROM Person a WHERE a[Name] = 'Alice'");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "SELECT a[Parent][[Name] FROM Person a WHERE aName] = 'Alice'");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "SELECT a[Parent].[Name] FROM Person a WHERE a[Name] = 'Alice'");
            ProcessQuery(parseOK, "SELECT a[MyNamespace.Father].Name FROM Person a WHERE a.Name = 'Alice';SELECT a[MyNamespace.Father].Name FROM Person a WHERE a.Name = 'Alice'");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "SELECT x FROM Person b, Person x WHERE { b Father+ x } AND b.Name = 'Bob'");
            ProcessQuery(parseOK, "SELECT b.Father+.Name FROM Person b WHERE b.Name = 'Bob'");
            ProcessQuery(parseOK, "SELECT b.Father+Name FROM Person b WHERE b.Name = 'Bob'");
            ProcessQuery(parseOK, "SELECT b[Father]+.Name FROM Person b WHERE b[Name] = 'Bob'");
            ProcessQuery(parseOK, "SELECT b[Father]+Name FROM Person b WHERE b[Name] = 'Bob'");
            ProcessQuery(parseOK, "SELECT b[Father]+Name FROM Person b WHERE b[Name] = 'Bob'+?");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "SELECT b[Father]+Name FROM Person b WHERE b+[Name] = 'Bob'");
            ProcessQuery(parseOK, "SELECT x FROM Person x WHERE x.Employer.^Employer.Name = 'Alice'");
            ProcessQuery(parseOK, "SELECT x FROM Person x WHERE x.Employer^Employer.Name = 'Alice'");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "SELECT ^x FROM Person x WHERE x.Employer.^Employer.Name = 'Alice'");
            ProcessQuery(parseOK, "select 2^2=4");
            ProcessQuery(parseOK, "SELECT x FROM Person x WHERE x[Employer]^[Employer][Name] = 'Alice'");
            ProcessQuery(parseOK, "SELECT x FROM Person x WHERE x[Employer]^Employer[Name] = 'Alice'");
            ProcessQuery(parseOK, "SELECT x FROM Person x WHERE x[Employer^Employer][Name] = 'Alice'");
            ProcessQuery(parseOK, "SELECT x FROM Person x WHERE x[Employer^Employer][Name] = 'Alice'");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "SELECT x FROM Person x WHERE x[Employer]^Employer][Name] = 'Alice'");
            ProcessQuery(parseOK, "SELECT x FROM Person x WHERE x.(Friend|Employer.^Employer)*.Name = 'Alice'");
            ProcessQuery(parseOK, "SELECT x FROM Person x WHERE x.([Friend]|[Employer]^[Employer])*[Name] = 'Alice'");
            ProcessQuery(parseOK, "SELECT x FROM Person x WHERE x.([Friend]|t[Employer]^[Employer])*[Name] = 'Alice'");
            ProcessQuery(parseOK, "SELECT x FROM Person x WHERE (t[Friend]+t[Employer]^d[Employer])*v[Name] = 'Alice'");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "SELECT x FROM Person x WHERE (t[Friend]|t[Employer]^d[Employer])*v[Name] = 'Alice'");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "SELECT x FROM Person x WHERE (t[Friend]|t[Employer]^[Employer])*v[Name] = 'Alice'");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "SELECT x FROM Person x WHERE x(t[Friend]|t[Employer]^[Employer])*v[Name] = 'Alice'");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "SELECT x FROM Person x WHERE x(t[Friend]|t[Employer]^[Employer])*[Name] = 'Alice'");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "SELECT x FROM Person x WHERE x([Friend]|t[Employer]^[Employer])*t[Name] = 'Alice'");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select 1|2");
            ProcessQuery(parseOK, "select * from \"table\" where name = 'Person'");
            ProcessQuery(parseOK, "select * from \"table\" t, materializedtable m where t.name = 'Person' and t.materializedtable = m");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select * from table where name = 'Person'");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select * from table t, materializedtable m where t.name = 'Person' and t.materializedtable = m");
            // Fails on _SC_ASSERT_DEBUG in native SQL processor, since not meta-data tables are created
            //ProcessQuery(1004, "INSERT INTO Visit(Id, Company, Start, End, " +
            //    "Spent, PageViewCount, Ip, Referer, UserAgent, TrackingCookie, " +
            //    "Protocol, DomainName) VALUES (" +
            //    "100, 'Starcounter Svenska AB', TIMESTAMP '2014-01-21 00:12:24', " +
            //    "TIMESTAMP '2014-01-21 00:13:55', 91, 5, '192.82.291.432', '231.122.431.19'," +
            //    "'Firefox', 'adfsafsfas23424525', 'protocol', 'somewhere.com')");
        }
    }
}
