using System;
using Starcounter;
using System.Diagnostics;

class Program {
	static void Main() {
		Employee e;
		Company c;
		Db.Transaction(delegate {
			c = new Company { OrganizationId = 0 };
			e = new Employee { FirstName = "The", LastName = "First",
				UserName = "TheF", Company = c };
			c.Head = e;
		});
		Employee q = Db.SQL<Employee>("select e from employee e").First;
		Trace.Assert(q.Equals(e));
		Trace.Assert(q.Company.Equals(c));
		Trace.Assert(q.Equals(c.Head));
	}
}

[Database]
public class Employee : User {
	public Company Company;
	public Int32 OfficeNr;
}

[Database]
public class User : Person {
	public String UserName;
}

[Database]
public class Company : Organization {
	public Employee Head;
}

[Database]
public class Organization {
	public Int32 OrganizationId;
}

[Database]
public class Person {
	public String FirstName;
	public String LastName;
	public String NickName;
}
