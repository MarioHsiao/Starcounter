
sql_example(pos,'MyDB',"select Department, count(FirstName) from Employee group by department").
sql_example(pos,'MyDB',"select Department, count(FirstName) from Employee group by Department").


sql_example(pos,'MyDB',"select e from Employee e where e.FirstName = 'Peter\"").
sql_example(pos,'MyDB',"select e from Employee e where e.FirstName = 'Pet\"er'").
sql_example(pos,'MyDB',"select e from Employee e where e.FirstName = \"Pet'er\"").
sql_example(pos,'MyDB',"select e from Employee e where e.FirstName = 'Peter'").
sql_example(pos,'MyDB',"select e from Employee e where e.FirstName = \"Peter\"").
sql_example(pos,'MyDB',"select e from Employee e where e.FirstName = 'Pet''er'").
sql_example(pos,'MyDB',"select e from Employee e where e.FirstName = \"Pet\"\"er\"").

sql_example(pos,'MyDB',"select E from Employee e where E.Manager = ?").
sql_example(pos,'MyDB',"select e from Employee e where e.Manager = ?").
sql_example(pos,'MyDB',"select p from Example.Person p where p is Employee").
sql_example(pos,'MyDB',"select p from Example.Person p where p is ?").
sql_example(pos,'MyDB',"select p from Example.Person p where p is not ?").
sql_example(pos,'MyDB',"select p from Example.Person p where p is Employee").
sql_example(pos,'MyDB',"select p from Example.Person p where p is not Employee").

sql_example(neg,'MyDB',"select p from Example.Person p where ? is Employee").
sql_example(neg,'MyDB',"select p from Example.Person p where p is String").

sql_example(pos,'MyDB',"select * from location fetch 10 offsetkey binary 'fff'").
sql_example(pos,'MyDB',"select * from location fetch ? offsetkey ?").
sql_example(pos,'MyDB',"select * from location fetch 10 offset 20").
sql_example(pos,'MyDB',"select * from location fetch ? offset ?").

sql_example(pos,'MyDB',"select e.lastname from example.employee e where e.firstname = ? and e.commission = ? and e.hiredate = ? and e.salaryint16 = ? and e.manager = ?").


/* SQL examples for testing, Peter Idestam-Almquist, Starcounter, 2011-12-01. */


sql_example(pos,'MyDB',"select ((3 - 2) - 8) from Employee").
sql_example(pos,'MyDB',"select (3 * ((6 - 8) / (4 - 8)) * 0) from Employee").
sql_example(pos,'MyDB',"select (3 * 6 - 8 / 4 - 8 * 0) from Employee").
sql_example(pos,'MyDB',"select (3 - 2 - 8) from Employee").
sql_example(pos,'MyDB',"select (3 - (2 - 8)) from Employee").


/* Test of fetch specification. */

sql_example(pos,'MyDB',"select * from location fetch 10 offsetkey binary 'fff'").
sql_example(pos,'MyDB',"select * from location fetch ? offsetkey ?").
sql_example(pos,'MyDB',"select * from location fetch 10 offset 20").
sql_example(pos,'MyDB',"select * from location fetch ? offset ?").

sql_example(neg,'MyDB',"select * from location fetch '10' offset 'fff'").
sql_example(neg,'MyDB',"select * from location fetch 10.1 offsetkey 'fff'").

/* Not sufficient error messages. */

sql_example(neg,'MyDB',"select cast(p.father as string) from example.person p").
sql_example(neg,'MyDB',"select nonexistingmethod<myperson>() from example.person").
sql_example(neg,'MyDB',"select firstname from employee where nonexistingmethod(manager) = true").

/* Test of variables. */

sql_example(pos,'MyDB',"select e.lastname from example.employee e where e.firstname = ? and e.commission = ? and e.hiredate = ? and e.salaryint16 = ? and e.manager = ?").
sql_example(pos,'MyDB',"select e.commission from example.employee e where e.firstname starts with ?").
sql_example(pos,'MyDB',"select e.firstname from example.employee e where ? is null").
sql_example(pos,'MyDB',"select d.name deptname, d.idbinary id from example.department d where d.idbinary = ?").

/* Test of old version of from-clause. */

sql_example(pos,'MyDB',"select e, d from example.employee e, example.department d where e.department = d").
sql_example(pos,'MyDB',"select e0, e1, e2, e3 from example.employee e0, example.employee e1, example.employee e2, example.employee e3 where e0 = e1 and e1 = e2 and e2 = e3").
sql_example(pos,'MyDB',"select e from example.employee e, example.employee f where e.manager.manager = f").

/* Test of replacing path expressions with joins. */

sql_example(pos,'MyDB',"select e from example.employee e where e.department.location = e.manager.department.location").
sql_example(pos,'MyDB',"select e from example.employee e where e.manager.department.location is null").
sql_example(pos,'MyDB',"select e from example.employee e where ? = e.manager.department.location").
sql_example(pos,'MyDB',"select e from example.employee e where e.manager.department.location = ?").
sql_example(pos,'MyDB',"select e from example.employee e where ? = e.department.location").
sql_example(pos,'MyDB',"select e from example.employee e where e.department.location = ?").
sql_example(pos,'MyDB',"select p, d from example.person p, example.department d where cast(p.father as employee).department = d").
sql_example(pos,'MyDB',"select sum(e.salaryint32) from employee e where e.manager.firstname = ?").

/* Test of asterisks '*' in paths in select-list. */

sql_example(pos,'MyDB',"select l.* from example.location l").
sql_example(pos,'MyDB',"select l.*, d.* from example.location l join example.department d").
sql_example(pos,'MyDB',"select * from location").
sql_example(pos,'MyDB',"select * from location join department").
sql_example(pos,'MyDB',"select p.father.father.* from example.person p").
sql_example(pos,'MyDB',"select cast(p.father.father as example.location).* from example.person p").

sql_example(neg,'MyDB',"select e.* as name from employee e").
sql_example(neg,'MyDB',"select cast(p.father.father.* as example.location) from example.person p").

/* Test of unqualified fields. */

sql_example(pos,'MyDB',"select firstname, name from employee join department").
sql_example(pos,'MyDB',"select cast(father as employee).manager from example.person").
sql_example(pos,'MyDB',"select getextension<myperson>() from example.person").
sql_example(pos,'MyDB',"select cast(getextension<myperson>() as employee) from example.person").

sql_example(neg,'MyDB',"select e.firstname from employee").
sql_example(neg,'MyDB',"select name from location join department").
sql_example(neg,'MyDB',"select getextension<myperson>() from example.person join employee").

/* Test of short class names. */

sql_example(pos,'MyDB',"select e.firstname from employee e").
sql_example(pos,'MyDB',"select e.getextension<myperson>() from employee e").

sql_example(neg,'MyDB',"select p from person p").

/* Test of "starts with". */

sql_example(pos,'MyDB',"select e.firstname from example.employee e where e.firstname starts with 'st'").
sql_example(pos,'MyDB',"select e.firstname from example.employee e where e.firstname starts with ?").

/* Test of object reference casting. */

sql_example(pos,'MyDB',"select cast(p as example.employee) from example.person p").
sql_example(pos,'MyDB',"select cast(p as example.employee).manager from example.person p").
sql_example(pos,'MyDB',"select cast(p.father as example.employee).manager from example.person p").
sql_example(pos,'MyDB',"select cast(cast(p as example.employee).manager as example.person).father from example.person p").
sql_example(pos,'MyDB',"select cast(p.getextension<example.myperson>() as example.employee).manager from example.person p").

sql_example(neg,'MyDB',"select cast(q as example.employee) from example.person p").
sql_example(neg,'MyDB',"select cast(p as example.nonexistingclass) from example.person p").
sql_example(neg,'MyDB',"select cast(p.father as string) from example.person p").
sql_example(neg,'MyDB',"select cast(p.equalsorisderivedfrom(p) as example.employee).manager from example.person p").

/* Test of object literals. */

sql_example(pos,'MyDB',"select e.firstname from example.employee e where e = object 123").
sql_example(pos,'MyDB',"select e.firstname, d.name from example.employee e join example.department d on e.department = d where e = object 123").

/* Test of binary literals. */

sql_example(pos,'MyDB',"select d from department d where d.idbinary = binary 'ffff'").

/* Test of hints. */

sql_example(pos,'MyDB',"select e.firstname, d.name from example.department d join example.employee e where e.firstname <= 'peter' and e.department.name = d.name and e.firstname >'b' option join order (e,d)").
sql_example(pos,'MyDB',"select e.firstname, d.name from example.department d join example.employee e join example.location l where e.firstname <= 'peter' and l.name = 'stockholm' and e.department.name = d.name and d.location = l and e.firstname >'b' option join order (e,d,l)").
sql_example(pos,'MyDB',"select e.firstname from example.employee e where e.firstname <= 'peter' and e.lastname >'b' option index (e myindex)").
sql_example(pos,'MyDB',"select e.firstname from example.employee e where e.firstname = 'peter' order by e.lastname desc option index(e myindex)").
sql_example(pos,'MyDB',"select e.firstname, d.name from example.employee e join example.department d on e.department = d where e.firstname = 'peter' order by d.name desc option index(e myindex)").

sql_example(neg,'MyDB',"select e.firstname, d.name from example.employee e right join example.department d on e.department = d option join order fixed").
sql_example(neg,'MyDB',"select e, m, d, l from example.department d join example.employee e left join (example.employee m join example.location l) option join order fixed").

/* Test of syntax. */

sql_example(pos,'MyDB',"select p._temp from example.person p").
sql_example(pos,'MyDB',"select e.manager.manager.department.location.name from example.employee e").

sql_example(neg,'MyDB',"").
sql_example(neg,'MyDB',"select example.employee.department.name from example.employee where example.employee.firstname = 'peter'").
sql_example(neg,'MyDB',"select e.firstname name, e.lastname name from employee e").

/* Test of methods. */

sql_example(pos,'MyDB',"select e.firstname, f.firstname from example.employee e join example.employee f where e.equalsorisderivedfrom(f) = true").
sql_example(pos,'MyDB',"select e.firstname, f.firstname from example.employee e join example.employee f where e.manager.equalsorisderivedfrom(f.manager) = true").
sql_example(pos,'MyDB',"select e.getextension<example.myperson>() from example.employee e").
sql_example(pos,'MyDB',"select e.getextension<example.myperson>().myid from example.employee e").

/* Test of comparisons. */

sql_example(pos,'MyDB',"select e.firstname, e.hiredate from example.employee e where e.hiredate = date '2006-10-30'").
sql_example(pos,'MyDB',"select e.firstname from example.employee e where e.manager = e.department").
sql_example(pos,'MyDB',"select e from example.employee e where e.commission = true").
sql_example(pos,'MyDB',"select e from example.employee e where e.firstname is null and e.manager = null and e.hiredate > null").
sql_example(pos,'MyDB',"select e.firstname from example.employee e where null is null order by e.manager desc").
sql_example(pos,'MyDB',"select e.firstname from example.employee e where e.firstname like 'p%et_er'").
sql_example(pos,'MyDB',"select e.firstname from example.employee e where e.firstname like 'p7_te7%' escape '7'").

sql_example(neg,'MyDB',"select e.firstname from example.employee e where e.firstname in ('peter','erik')").

/* Test of operations. */

sql_example(pos,'MyDB',"select e.department.name || ' ' || e.firstname, e.salarysingle * 2 / -1 from example.employee e where e.salarydecimal * + +1.25 = - +2e+3 - e.salaryuint16 and e.manager = e").
sql_example(pos,'MyDB',"select -(-(-e.salaryint64)) * 3 from example.employee e").

sql_example(neg,'MyDB',"select sum(e.salaryint32) * (2 + 'b') from example.employee e").

/* Test of sorting. */

sql_example(pos,'MyDB',"select e.firstname fname from example.employee e order by random").
sql_example(pos,'MyDB',"select e.firstname from example.employee e order by e.department.name, e.firstname, e.lastname").
sql_example(pos,'MyDB',"select p from example.person p order by p").
sql_example(pos,'MyDB',"select p.firstname fname from example.person p order by p.firstname").

/* Test of joins. */

sql_example(pos,'MyDB',"select e.firstname, d.name from example.employee e join example.department d on e.department = d").
sql_example(pos,'MyDB',"select e.firstname, d.name from example.employee e join example.department d where e.department = d").
sql_example(pos,'MyDB',"select e.firstname, d.name from example.employee e join example.department d on e = d").
sql_example(pos,'MyDB',"select e.firstname, d.name from example.employee e cross join example.department d").
sql_example(pos,'MyDB',"select e.firstname, f.firstname from example.employee e left outer join example.employee f where e.manager = f").
sql_example(pos,'MyDB',"select e0.firstname, e1.firstname, e2.firstname, e3.firstname from example.employee e0 join example.employee e1 join (example.employee e2 join example.employee e3) where e0 = e1 and e1 = e2 and e2 = e3").
sql_example(pos,'MyDB',"select e.firstname, f.firstname from example.employee f right outer join example.employee e where e.manager = f").
sql_example(pos,'MyDB',"select e from example.employee e join example.employee f where e.manager.manager = f").

/* Test of aggregations. */

sql_example(pos,'MyDB',"select count(*), sum(e.salaryint32) from example.employee e").
sql_example(pos,'MyDB',"select e.department.name, sum(e.salaryint16 * 2) * 3 from example.employee e group by e.department having sum(e.salaryint32) > 0 order by e.department").
sql_example(pos,'MyDB',"select d.name, sum(e.salaryint16) from example.employee e join example.department d where e.department = d group by d having sum(e.salaryint32) > 0 order by d.name").
sql_example(pos,'MyDB',"select d.name, sum(e.salaryint32) from example.department d join example.employee e on d = e.department group by d.name").
sql_example(pos,'MyDB',"select e.department, e.department.name, sum(e.salarydouble), avg(e.salarydecimal), min(e.salaryuint64), max(e.salarysingle) from example.employee e group by e.department, e.commission").
sql_example(pos,'MyDB',"select sum(e.salaryint64), avg(e.salarysingle) from example.employee e group by e.commission, e.hiredate, e.manager, e.salaryint32, e.salaryuint32, e.salarydouble, e.salarydecimal").

sql_example(neg,'MyDB',"select max(null) from example.employee").
sql_example(neg,'MyDB',"select f.loc.name, f.sum1 + h.sum2 from (select e.department.location loc, sum(all e.salaryint32) sum1 from example.employee e where e.firstname <> 'peter' group by e.department.location) f join (select sum(g.salaryint32) sum2 from example.employee g group by g.department having sum(g.salaryint32) > 1) h").
sql_example(neg,'MyDB',"select sum(e.salaryint64) * 3, max(e.manager), min(e) from example.employee e").
sql_example(neg,'MyDB',"select e.department.name dname, min(e.manager) from example.employee e group by e.department").
sql_example(neg,'MyDB',"select e.department.name, e.firstname, sum(e.salaryint16) from example.employee e group by e.department having sum(e.salaryint32) > 0 order by e.lastname").

/* Test of indexes. */

sql_example(pos,'MyDB',"select e.firstname from example.employee e where e.commission = true").
sql_example(pos,'MyDB',"select e.department.name from example.employee e where e.lastname = 'idestam' and e.firstname > 'bertil' order by e.firstname").
sql_example(pos,'MyDB',"select e.lastname from example.employee e where e.lastname = 'idestam' and e.lastname <= 'samuelsson'").
sql_example(pos,'MyDB',"select e1.firstname, e2.firstname, e3.firstname, e4.firstname from example.employee e1 join example.employee e2 on e1.firstname = e2.firstname join example.employee e3 on e2.firstname = e3.firstname join example.employee e4 on e3.firstname = e4.firstname where e1.firstname = 'peter' and e2.firstname = 'per' and e3.firstname = 'erik' and e4.firstname = 'christian'").
sql_example(pos,'MyDB',"select e.firstname from example.employee e where e.firstname >= e.lastname and e.manager = e and e.firstname > 'erik' and (e.salaryint16 = e.salaryint64 or e.salaryint16 = e.salaryuint64 or e.salaryint16 = e.salarysingle)").
sql_example(pos,'MyDB',"select e.firstname from example.employee e where e.lastname is not null and e.firstname is not null").
sql_example(pos,'MyDB',"select e from example.employee e where e.getextension<example.myperson>().myname = 'peter2'").
sql_example(pos,'MyDB',"select e from example.employee e where e.getextension<example.myperson>().myname = 'peter2' option index(e myindex)").

/*END*/
