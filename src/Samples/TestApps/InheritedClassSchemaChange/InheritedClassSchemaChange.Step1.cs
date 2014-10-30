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
		ScAssertion.Assert(q.Equals(e), "Unexpected result");
		ScAssertion.Assert(q.Company.Equals(c), "Unexpected result");
		ScAssertion.Assert(q.Equals(c.Head), "Unexpected result");
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