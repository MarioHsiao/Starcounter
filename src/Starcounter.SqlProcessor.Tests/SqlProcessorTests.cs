using System;
using NUnit.Framework;
using Starcounter.SqlProcessor;

namespace Starcounter.SqlProcessor.Tests {
    [TestFixture]
    public class SqlProcessorTests {

        internal const uint ParseOK = Error.SCERRSQLNOTIMPLEMENTED;

        public static unsafe void ProcessQuery(uint expectedError, string query) {
            Exception ex = SqlProcessor.CallSqlProcessor(query);
            Assert.AreEqual(expectedError, ex.Data[ErrorCode.EC_TRANSPORT_KEY], ex.Message);
        }

        [Test]
        public static void HelloProcessor() {
            ProcessQuery(ParseOK, "select * from user where col = 1");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select");
            ProcessQuery(ParseOK, "select * from user");
        }

        [Test]
        public static void SqlSyntax() {
            ProcessQuery(ParseOK, "SELECT 1");
            ProcessQuery(ParseOK, "SELECT Abc FROM tbL");
            ProcessQuery(ParseOK, "SELECT Å");
            ProcessQuery(ParseOK, "SELECT У");
            ProcessQuery(ParseOK, "SELECT -(-1.2E+02), -(-1.2), -(+1), +(-2), -(+.2E+02)");
            ProcessQuery(ParseOK, "select a -- /* asdfasdf asdlfkjaskldfj");
            ProcessQuery(ParseOK, "select b /* asdfasd -- lkjlkj */");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select b /* asdfasd -- lkjlkj ");
            ProcessQuery(ParseOK, "select u from Accounttest.auser u");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select ");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "selec");
            ProcessQuery(ParseOK, "select * from t1");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select sd od bd");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select sd order byd");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select * from tble limit 1, 2");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "(SELECT foo ORDER BY bar) ORDER BY baz");
            ProcessQuery(ParseOK, "SELECT * from table1 offsetkey 4");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select * from [table] where [offset] = 3");
            ProcessQuery(ParseOK, "select * from tbl where col = $1");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select * from table");
            ProcessQuery(ParseOK, "select * from \"table\"");
            //ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "SELECT E'\\xDEADBEEF'"); // BUG here, #1237
            ProcessQuery(ParseOK, "select * from tbl option index (tbl indx1)");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select * from tbl option index tbl indx1");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select * from tbl option index tbl");
            ProcessQuery(ParseOK, "select * from tbl1 union select * from tbl option index (tbl indx1)");// MUST FAIL
            ProcessQuery(ParseOK, "select * from tbl1 union (select * from tbl option index (tbl indx1))");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select * from tbl option join order tbl");
            ProcessQuery(ParseOK, "select * from tbl option join order (tbl)");
            ProcessQuery(ParseOK, "select * from tbl option index (tbl indx1), join order (tbl)");
            ProcessQuery(ParseOK, "SELECT d.Name, e.LastName FROM Department d JOIN Employee e ON e.Department = d WHERE e.FirstName = 'Bob' OPTION JOIN ORDER (e,d)");
            ProcessQuery(ParseOK, "select (f.d()).d.f");
            ProcessQuery(ParseOK, "select f.d().f.d.s()");
            ProcessQuery(ParseOK, "select (f.d()).d.f()");
            //ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select cast(col as typ<typ2>) from tble"); // BUG here
            //ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select nm.typ<typ2>.fn()"); // BUG here
            ProcessQuery(ParseOK, "select $1.f()");
            ProcessQuery(ParseOK, "select cast(col as type).ref1.ref2 from tbl1");
            ProcessQuery(ParseOK, "select I(A)");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select (I(A).F()");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select (I(A).J(A,B).F()");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select (I(A,B,C).J(A,B).F(A)()");
            ProcessQuery(ParseOK, "select I.J(A,B).F()");
            ProcessQuery(ParseOK, "select I.J(A,B).F<A>()");
            ProcessQuery(ParseOK, "select I.J.F()");
            ProcessQuery(ParseOK, "select I.J(A,B).F<A<C>,T>()");
            ProcessQuery(ParseOK, "select I.J(A)");
            ProcessQuery(ParseOK, "select I<A>()");
            ProcessQuery(ParseOK, "select I.J<A>.f<A>(g)");
            ProcessQuery(ParseOK, "select (I.J(a)).T<K>(v)");
            //ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select a < b > c"); // BUG here
            //ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select a < b , c >"); // BUG here
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select f()()");
            ProcessQuery(ParseOK, "select f<typ1>()");
            ProcessQuery(ParseOK, "select cast(a+b as c)");
            ProcessQuery(ParseOK, "select cast(a+b as int)");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select cast(a+b as c.d(i)");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select cast(a+b as c.d(i))");
            ProcessQuery(ParseOK, "select cast(a+b as c.d[])");
            ProcessQuery(ParseOK, "select cast(a+b as c.d<i,d>)");
            ProcessQuery(ParseOK, "select cast(a+b as c.d<i<g>,d>)");
            ProcessQuery(ParseOK, "select DATE '2012-02-02'");
            ProcessQuery(ParseOK, "select col1.fn().col2 = DATE '2012-02-02' from ns1.tbl1");
            ProcessQuery(ParseOK, "select ?.f()");
            ProcessQuery(ParseOK, "select * from tbl where col1 = ?");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select binary 'x'");
            ProcessQuery(ParseOK, "select BINARY 'AB14093FE'");
            ProcessQuery(ParseOK, "SELECT e FROM Employee e WHERE e.Commission = TRUE");
            ProcessQuery(ParseOK, "SELECT e FROM Employee e WHERE e.Salary = 5000");
            ProcessQuery(ParseOK, "SELECT e FROM Employee e WHERE e.Salary = 5000.00");
            ProcessQuery(ParseOK, "SELECT e FROM Employee e WHERE e.Salary = 5.0E3");
            ProcessQuery(ParseOK, "SELECT p FROM Photo p WHERE p.Description = 'Smith''s family'");
            ProcessQuery(ParseOK, "SELECT e FROM Employee e WHERE e.HireDate = DATE '2006-11-01'");
            ProcessQuery(ParseOK, "SELECT e FROM Employee e WHERE e.HireDate = TIMESTAMP '2006-11-01 00:00:00'");
            ProcessQuery(ParseOK, "SELECT l FROM Department d WHERE d.BinaryId = BINARY 'D91FA24E19FB065A'");
            ProcessQuery(ParseOK, "SELECT e FROM Employee e WHERE e = OBJECT 123");
            ProcessQuery(ParseOK, "SELECT * from tbl where col1 STARTS WITH 'Start with me'");
            ProcessQuery(ParseOK, "select t from tbl t where str starts with ?");
            ProcessQuery(ParseOK, "select getextension<myperson>() from example.person");
            ProcessQuery(ParseOK, "select e from example.employee e where e.getextension<example.myperson>().myname =?");
            ProcessQuery(ParseOK, "select u from User u; select d from Department d");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select u from User u where u.name = u&'\\asdf'");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "SELECT p FROM Photo p WHERE p.Description = 'Smith\\'s family'");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select E'\\''");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "ALTER DEFAULT PRIVILEGES IN SCHEMA myschema REVOKE GRANT OPTION FOR SELECT ON TABLES FROM internal");
            ProcessQuery(ParseOK, "select A.D.B<C>.F.G<A>(d,A<d>.D<s>())");
            ProcessQuery(ParseOK, "select 2<3 and 3>4");
            ProcessQuery(ParseOK, "select 2<A.B and A.B>(A<B>(F))");
            ProcessQuery(ParseOK, "select fr<A.B>(A<B>(F))");
            ProcessQuery(ParseOK, "select F(G<A,B>(7))");
            ProcessQuery(ParseOK, "select F(G<A,B>>(7))");
            ProcessQuery(ParseOK, "select F(G<A,B>7)");
            ProcessQuery(ParseOK, "select F(G<A,B>>7)");
            ProcessQuery(ParseOK, "select A+ <B>F()");
            ProcessQuery(ParseOK, "select A+ <B>F(A,B)");
            ProcessQuery(ParseOK, "select A+ D<A,F>.<B>F()");
            ProcessQuery(ParseOK, "select A+ M.D.<B>F(A,B)");
            ProcessQuery(ParseOK, "select G<A,B>(7)");
            ProcessQuery(ParseOK, "select nullif(G<A,B>(7))");
            ProcessQuery(ParseOK, "DROP INDEX UserLN ON AccountTest.user");
            ProcessQuery(ParseOK, "create index SalaryEmployee_NSalaryInt64 on SqlTest.EmployeeDb.SalaryEmployee (NSalaryInt64)");
            ProcessQuery(ParseOK, "SELECT prop1, prop2 FROM table1");
            ProcessQuery(ParseOK, "SELECT ROW(prop1, prop2) FROM table1");
            ProcessQuery(ParseOK, "SELECT ROW(valueProp) FROM table1");
            ProcessQuery(ParseOK, "SELECT valueProp FROM table1");
            ProcessQuery(ParseOK, "select * from t [ 2]");
            ProcessQuery(ParseOK, "select * from t \"table\"");
            ProcessQuery(ParseOK, "select t.u as USER from t as ORDER");
            ProcessQuery(ParseOK, "select order() from order() as order order by order()");
            ProcessQuery(ParseOK, "select order from order as order order by order");
            ProcessQuery(ParseOK, "select a<b, a<c from t");
            ProcessQuery(ParseOK, "SELECT USER");
            ProcessQuery(ParseOK, "select user as user from user.user() order by user");
            ProcessQuery(ParseOK, "select order as order from order.order() order by order");
            ProcessQuery(ParseOK, "select u from user u group by u");
            ProcessQuery(ParseOK, "select group as group from group.group() group by group order by group ");
            ProcessQuery(ParseOK, "select window from window.window().window group by window window abc as (cdv) order by window");
            ProcessQuery(ParseOK, "select union from union.union().union group by union union select union from union order by union");
            ProcessQuery(ParseOK, "select option as option from option.option().option as option group by option OPTION JOIN ORDER (e, m), INDEX (e MyIndexOnLastName), INDEX (m MyIndexOnFirstName)");
            ProcessQuery(ParseOK, "select cast(al.col as cl)");
            ProcessQuery(ParseOK, "select cast(al.col as cl<tp>)");
            ProcessQuery(ParseOK, "select cast(al.col as ns.cl<tp>)");
            ProcessQuery(ParseOK, "drop index indx on ns.tbl1");
            ProcessQuery(ParseOK, "drop index indx on ns.tbl1, ind2 on tbl2, inx3 on ns1.ns2.tbl3<t>");
            ProcessQuery(ParseOK, "select typeof(nm.nm2.type<A,B>).FullName fullname");
            ProcessQuery(ParseOK, "select CURRENT_DATE");
            ProcessQuery(ParseOK, "select CURRENT_TIME, CURRENT_TIME(2)");
            ProcessQuery(ParseOK, "select CURRENT_TIMESTAMP, CURRENT_TIMESTAMP(2)");
            ProcessQuery(ParseOK, "select LOCALTIME, LOCALTIME(2)");
            ProcessQuery(ParseOK, "select LOCALTIMESTAMP, LOCALTIMESTAMP(2)");
            ProcessQuery(ParseOK, "select CURRENT_ROLE");
            ProcessQuery(ParseOK, "select CURRENT_USER");
            ProcessQuery(ParseOK, "select SESSION_USER");
            ProcessQuery(ParseOK, "select CURRENT_CATALOG");
            ProcessQuery(ParseOK, "select NULLIF(a+b,B+A)");
            ProcessQuery(ParseOK, "select COALESCE(a,b,c)");
            ProcessQuery(ParseOK, "select GREATEST(a+b,b+c,c+a)");
            ProcessQuery(ParseOK, "select LEAST(a+b,b+c,c+a)");
            ProcessQuery(ParseOK, "select foreign, primary, where, column from index, limit where use");
            ProcessQuery(ParseOK, "select analyze, analyse");
            ProcessQuery(ParseOK, "select absolute, aggregate, also, assertion, assignment, attribute, backward, called");
            ProcessQuery(ParseOK, "select\r\na\tfrom/* comment ** comment*/ \ft -- Comment\nwhere k>2");
            ProcessQuery(ParseOK, "select å from öl");
            ProcessQuery(ParseOK, "select \u0066");
            ProcessQuery(ParseOK, "SELECT -(-1.2E+02), -(-1.2), -(+1), +(-2), -(+.2E+02)");
            ProcessQuery(ParseOK, "select * from tbl where col like '\\n and \\u and \\\\'");
            ProcessQuery(ParseOK, "select * from tbl where a >= b and c != d and e <> k /* comment */ and tt -- kk */");
            ProcessQuery(ParseOK, "select 'ghf k kjn''jgk''''k''kjkjh'");
            ProcessQuery(ParseOK, "select B'010010', X'FD13A', N'Text'");
            //ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select B'010A010'"); // BUG here
            //ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select X'FID13A'"); // BUG here
            ProcessQuery(ParseOK, "select 'Константа' from \"Таблица\" таблица");
            ProcessQuery(ParseOK, "select 9223372036854775807");
            ProcessQuery(ParseOK, "select 345, -234, 123 from mytable t where c = -2343 and t = 4520904907");
            ProcessQuery(ParseOK, "select 324.23423, 342, -234.234e+12");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "asdfqad");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "1342");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "()");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, " ");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "\n");
            ProcessQuery(ParseOK, "select a from account a where accountid > ? fetch ?");
            ProcessQuery(ParseOK, "select a from account a where accountid > ? fetch ? offset ?");
            ProcessQuery(ParseOK, "select a from account a where accountid > ? fetch ? offsetkey ?");
            ProcessQuery(ParseOK, "select a from account a join user u where a.client = u and u.prop = ?");
            ProcessQuery(ParseOK, "select asdfas from tbl1 join tbl2 join tbl3 on tbl2.f = tbl3.f join (tbl4 join tbl5 on tbl4.s = tbl5.s) on tbl2.k = tbl4.k where tbl1.m = tbl4.m");
            ProcessQuery(ParseOK, "select asdfas from tbl1 join (tbl2 join tbl3) on tbl2.f = tbl3.f join (tbl4 join tbl5 on tbl4.s = tbl5.s) on tbl2.k = tbl4.k where tbl1.m = tbl4.m");
            //ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select u1 from user u1 join user u2 on u1 != u2 and u1.useridnr = u2.useridn2 + ?"); // BUG here
            ProcessQuery(ParseOK, "select ?a where a=2");
            ProcessQuery(ParseOK, "select $1a");
            ProcessQuery(ParseOK, "select?a");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select$1a");
            ProcessQuery(ParseOK, "select 'System.Reflection.TargetInvocationException: Obiekt docelowy wywoÅ‚ania zgÅ‚osiÅ‚ wyjÄ…tek'");
            ProcessQuery(ParseOK, "select NULL, 231");
            ProcessQuery(ParseOK, "SELECT d.Name, e.LastName FROM Department d JOIN Employee e ON e.Department = d WHERE e.FirstName = 'Bob' OPTION JOIN ORDER (e,d)");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "SELECT d.Name, e.LastName FROM Department d JOIN Employee e ON e.Department = d WHERE e.FirstName = 'Bob' OPTION JOIN ORDERs (e,d)");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "SELECT d.Name, e.LastName FROM Department d JOIN Employee e ON e.Department = d WHERE e.FirstName = 'Bob' OPTION JOIN dORDER (e,d)");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "SELECT d.Name, e.LastName FROM Department d JOIN Employee e ON e.Department = d WHERE e.FirstName = 'Bob' OPTION dJOIN ORDER (e,d)");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "SELECT d.Name, e.LastName FROM Department d JOIN Employee e ON e.Department = d WHERE e.FirstName = 'Bob' OPTION JOINs ORDER (e,d)");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "SELECT d.Name, e.LastName FROM Department d JOIN Employee e ON e.Department = d WHERE e.FirstName = 'Bob' OPTION JOINORDER (e,d)");
            ProcessQuery(ParseOK, "select t from myt t where t.\"when\" = ?");
            ProcessQuery(ParseOK, "select t from myt t where t.\"when\" = $1");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select t from myt t where t.when = :1");
            ProcessQuery(ParseOK, "select t from myt t where t.when = ?");
            ProcessQuery(ParseOK, "select 1 * 2");
            ProcessQuery(ParseOK, "select c from dsfa c where c.sadf * ? and 1 *c.sdfa");
            ProcessQuery(ParseOK, "select t from myt t where t.column = $4");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select t from myt t where when = ?");
            ProcessQuery(ParseOK, "SELECT e, m FROM Employee e JOIN Employee m ON e.Manager = m WHERE e.FirstName = 'John' AND e.LastName = 'Smith' AND m.FirstName = 'David' AND m.LastName = 'King' OPTION INDEX (e MyIndexOnLastName), INDEX(m MyIndexOnFirstName)");
            ProcessQuery(ParseOK, "SELECT x.Name FROM Person a, Person x, Parent p WHERE a = p.property AND p.object = x AND a.Name = 'Alice'");
            ProcessQuery(ParseOK, "SELECT a[Parent][Name] FROM Person a WHERE a[Name] = 'Alice'");
            ProcessQuery(ParseOK, "SELECT a[Parent].Name FROM Person a WHERE a[Name] = 'Alice'");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "SELECT a[Parent]Name FROM Person a WHERE a[Name] = 'Alice'");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "SELECT a[Parent]Name] FROM Person a WHERE a[Name] = 'Alice'");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "SELECT a[Parent][[Name] FROM Person a WHERE aName] = 'Alice'");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "SELECT a[Parent].[Name] FROM Person a WHERE a[Name] = 'Alice'");
            ProcessQuery(ParseOK, "SELECT a[MyNamespace.Father].Name FROM Person a WHERE a.Name = 'Alice';SELECT a[MyNamespace.Father].Name FROM Person a WHERE a.Name = 'Alice'");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "SELECT x FROM Person b, Person x WHERE { b Father+ x } AND b.Name = 'Bob'");
            ProcessQuery(ParseOK, "SELECT b.(Father+).Name FROM Person b WHERE b.Name = 'Bob'");
            ProcessQuery(ParseOK, "SELECT b.Father+Name FROM Person b WHERE b.Name = 'Bob'");
            ProcessQuery(ParseOK, "SELECT b[Father+].Name FROM Person b WHERE b[Name] = 'Bob'");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "SELECT b.[Father+].Name FROM Person b WHERE b[Name] = 'Bob'");
            ProcessQuery(ParseOK, "SELECT b.([Father]+)[Name] FROM Person b WHERE b[Name] = 'Bob'");
            ProcessQuery(ParseOK, "SELECT b.([Father]+).Name FROM Person b WHERE b[Name] = 'Bob'");
            ProcessQuery(ParseOK, "SELECT b[Father+][Name] FROM Person b WHERE b[Name] = 'Bob'");
            ProcessQuery(ParseOK, "SELECT b[Father+].Name FROM Person b WHERE b[Name] = 'Bob'");
            ProcessQuery(ParseOK, "SELECT b[Father]+Name FROM Person b WHERE b[Name] = 'Bob'");
            ProcessQuery(ParseOK, "SELECT b[Father]+Name FROM Person b WHERE b[Name] = 'Bob'+?");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "SELECT b[Father]+Name FROM Person b WHERE b+[Name] = 'Bob'");
            ProcessQuery(ParseOK, "SELECT x FROM Person x WHERE x.Employer.^Employer.Name = 'Alice'");
            ProcessQuery(ParseOK, "SELECT x FROM Person x WHERE x.Employer^Employer.Name = 'Alice'");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "SELECT ^x FROM Person x WHERE x.Employer.^Employer.Name = 'Alice'");
            ProcessQuery(ParseOK, "select 2^2=4");
            ProcessQuery(ParseOK, "SELECT x FROM Person x WHERE x[Employer]^[Employer][Name] = 'Alice'");
            ProcessQuery(ParseOK, "SELECT x FROM Person x WHERE x[Employer]^Employer[Name] = 'Alice'");
            ProcessQuery(ParseOK, "SELECT x FROM Person x WHERE x[Employer^Employer][Name] = 'Alice'");
            ProcessQuery(ParseOK, "SELECT x FROM Person x WHERE x[Employer^Employer][Name] = 'Alice'");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "SELECT x FROM Person x WHERE x[Employer]^Employer][Name] = 'Alice'");
            ProcessQuery(ParseOK, "SELECT x FROM Person x WHERE x.(Friend|Employer.^Employer*).Name = 'Alice'");
            ProcessQuery(ParseOK, "SELECT x FROM Person x WHERE x.((Friend|Employer.^Employer)*).Name = 'Alice'");
            ProcessQuery(ParseOK, "SELECT x FROM Person x WHERE x.(([Friend]|[Employer]^[Employer])*)[Name] = 'Alice'");
            ProcessQuery(ParseOK, "SELECT x FROM Person x WHERE x.(([Friend]|t[Employer]^[Employer])*)[Name] = 'Alice'");
            ProcessQuery(ParseOK, "SELECT x FROM Person x WHERE x.([Friend]|[Employer]^[Employer]*)[Name] = 'Alice'");
            ProcessQuery(ParseOK, "SELECT x FROM Person x WHERE x.([Friend]|t[Employer]^[Employer]*)[Name] = 'Alice'");
            ProcessQuery(ParseOK, "SELECT x FROM Person x WHERE (t[Friend]+t[Employer]^d[Employer])*v[Name] = 'Alice'");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "SELECT x FROM Person x WHERE x.(Friend|Employer.^Employer)*.Name = 'Alice'");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "SELECT x FROM Person x WHERE x.([Friend]|[Employer]^[Employer])*[Name] = 'Alice'");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "SELECT x FROM Person x WHERE x.([Friend]|t[Employer]^[Employer])*[Name] = 'Alice'");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "SELECT x FROM Person x WHERE (t[Friend]|t[Employer]^d[Employer])*v[Name] = 'Alice'");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "SELECT x FROM Person x WHERE (t[Friend]|t[Employer]^[Employer])*v[Name] = 'Alice'");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "SELECT x FROM Person x WHERE x(t[Friend]|t[Employer]^[Employer])*v[Name] = 'Alice'");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "SELECT x FROM Person x WHERE x(t[Friend]|t[Employer]^[Employer])*[Name] = 'Alice'");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "SELECT x FROM Person x WHERE x([Friend]|t[Employer]^[Employer])*t[Name] = 'Alice'");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select 1|2");
            ProcessQuery(ParseOK, "select * from \"table\" where name = 'Person'");
            ProcessQuery(ParseOK, "select * from \"table\" t, materializedtable m where t.name = 'Person' and t.materializedtable = m");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select * from table where name = 'Person'");
            ProcessQuery(Error.SCERRSQLINCORRECTSYNTAX, "select * from table t, materializedtable m where t.name = 'Person' and t.materializedtable = m");
            ProcessQuery(ParseOK, "select firstname || lastname");
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
