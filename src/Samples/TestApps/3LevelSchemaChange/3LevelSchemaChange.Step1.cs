using System;
using Starcounter;
using System.Diagnostics;
using Starcounter.Metadata;

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
		Employee q = Db.SQL<Employee>("select e from employee e").First;
		ScAssertion.Assert(q != null, "Query should return a result");
		ScAssertion.Assert(c != null, "Created instance of Company should not be empty");
		ScAssertion.Assert(e != null, "Created instance of Employee should not be empty");
		ScAssertion.Assert(q.UserName == e.UserName, "Unexpected result");
		ScAssertion.Assert(q.Company.OrganizationId == c.OrganizationId, "Unexpected result");
		ScAssertion.Assert(q.UserName == c.Head.UserName, "Unexpected result");
		
		AssertMetadata();
	}
	
	static void AssertMetadata() {
		// 5 types: Person with 1 column, User - +1, Employee +1, Company +1, Organization 2
		// ClrClass and RawView, Columns
		ClrClass cPerson = null;
		ClrClass cUser = null;
		ClrClass cEmployee = null;
		ClrClass cOrganization = null;
		ClrClass cCompany = null;
		int count = 0;
		foreach(ClrClass c in Db.SQL<ClrClass>("select c from clrclass c where fullname = ?", "Person")) {
			cPerson = c;
			count++;
		}
		ScAssertion.Assert(count == 1);
		ScAssertion.Assert(cPerson != null);
		ScAssertion.Assert(cPerson.Name == cPerson.FullName);
		Console.WriteLine(cPerson.UniqueIdentifier);
		ScAssertion.Assert(cPerson.UniqueIdentifier == "sccode.exe.Person");
		count = 0;
		foreach(ClrClass c in Db.SQL<ClrClass>("select c from clrclass c where fullname = ?", "User")) {
			cUser = c;
			count++;
		}
		ScAssertion.Assert(count == 1);
		ScAssertion.Assert(cUser != null);
		count = 0;
		foreach(ClrClass c in Db.SQL<ClrClass>("select c from clrclass c where fullname = ?", "Employee")) {
			cEmployee = c;
			count++;
		}
		ScAssertion.Assert(count == 1);
		ScAssertion.Assert(cEmployee != null);
		count = 0;
		foreach(ClrClass c in Db.SQL<ClrClass>("select c from clrclass c where fullname = ?", "Organization")) {
			cOrganization = c;
			count++;
		}
		ScAssertion.Assert(count == 1);
		ScAssertion.Assert(cOrganization != null);
		count = 0;
		foreach(ClrClass c in Db.SQL<ClrClass>("select c from clrclass c where fullname = ?", "Company")) {
			cCompany = c;
			count++;
		}
		ScAssertion.Assert(count == 1);
		ScAssertion.Assert(cCompany != null);
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