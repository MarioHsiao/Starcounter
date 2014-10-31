using System;
using Starcounter;
using System.Diagnostics;

class Program {
	static void Main() {
		Employee e = null;
		Company c = null;
		Db.Transaction(delegate {
			c = new Company { OrganizationId = 0 };
			e = new Employee { FirstName = "The", LastName = "First",
				UserName = "TheF", Company = c };
			c.Head = e;
		});
		ScAssertion.Assert(c != null, "Created instance should not be empty");
		ScAssertion.Assert(e != null, "Created instance should not be empty");
		Employee q = Db.SQL<Employee>("select e from employee e").First;
		ScAssertion.Assert(q != null, "Query should return a result");
		ScAssertion.Assert(q.UserName == e.UserName, "Unexpected result");
		ScAssertion.Assert(q.Company.OrganizationId == c.OrganizationId, "Unexpected result");
		ScAssertion.Assert(q.UserName == c.Head.UserName, "Unexpected result");
	}
}

[Database]
public class Employee : User {
	public Company Company;
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
}