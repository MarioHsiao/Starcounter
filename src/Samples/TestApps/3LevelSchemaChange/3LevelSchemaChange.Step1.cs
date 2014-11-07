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
		ScAssertion.Assert(cPerson.UniqueIdentifier == "Person");
		count = 0;
		foreach(ClrClass c in Db.SQL<ClrClass>("select c from clrclass c where fullname = ?", "User")) {
			cUser = c;
			count++;
		}
		ScAssertion.Assert(count == 1);
		ScAssertion.Assert(cUser != null);
		ScAssertion.Assert(cUser.UniqueIdentifier == "User");
		ScAssertion.Assert(cPerson.Equals(cUser.Inherits));
		count = 0;
		foreach(ClrClass c in Db.SQL<ClrClass>("select c from clrclass c where fullname = ?", "Employee")) {
			cEmployee = c;
			count++;
		}
		ScAssertion.Assert(count == 1);
		ScAssertion.Assert(cEmployee != null);
		ScAssertion.Assert(cEmployee.UniqueIdentifier == "Employee");
		ScAssertion.Assert(cUser.Equals(cEmployee.Inherits));
		count = 0;
		foreach(ClrClass c in Db.SQL<ClrClass>("select c from clrclass c where fullname = ?", "Organization")) {
			cOrganization = c;
			count++;
		}
		ScAssertion.Assert(count == 1);
		ScAssertion.Assert(cOrganization != null);
		ScAssertion.Assert(cOrganization.UniqueIdentifier == "Organization");
		count = 0;
		foreach(ClrClass c in Db.SQL<ClrClass>("select c from clrclass c where fullname = ?", "Company")) {
			cCompany = c;
			count++;
		}
		ScAssertion.Assert(count == 1);
		ScAssertion.Assert(cCompany != null);
		ScAssertion.Assert(cCompany.UniqueIdentifier == "Company");
		ScAssertion.Assert(cOrganization.Equals(cCompany.Inherits));
		RawView vPerson = null;
		RawView vUser = null;
		RawView vEmployee = null;
		RawView vOrganization = null;
		RawView vCompany = null;
		count = 0;
		foreach(RawView v in Db.SQL<RawView>("select v from rawview v where materializedtable.name = ? and fullname = ?", cPerson.FullName, cPerson.FullName)) {
			count++;
			vPerson = v;
		}
		ScAssertion.Assert(count == 1);
		ScAssertion.Assert(vPerson != null);
		ScAssertion.Assert(vPerson.UniqueIdentifier == "Starcounter.Raw.Person");
		ScAssertion.Assert(vPerson.MaterializedTable.Equals(cPerson.MaterializedTable));
		count = 0;
		foreach(RawView v in Db.SQL<RawView>("select v from rawview v where materializedtable.name = ? and fullname = ?", cUser.FullName, cUser.FullName)) {
			count++;
			vUser = v;
		}
		ScAssertion.Assert(count == 1);
		ScAssertion.Assert(vUser != null);
		ScAssertion.Assert(vUser.UniqueIdentifier == "Starcounter.Raw.User");
		ScAssertion.Assert(vUser.MaterializedTable.Equals(cUser.MaterializedTable));
		ScAssertion.Assert(vPerson.Equals(vUser.Inherits));
		count = 0;
		foreach(RawView v in Db.SQL<RawView>("select v from rawview v where materializedtable.name = ? and fullname = ?", cEmployee.FullName, cEmployee.FullName)) {
			count++;
			vEmployee = v;
		}
		ScAssertion.Assert(count == 1);
		ScAssertion.Assert(vEmployee != null);
		ScAssertion.Assert(vEmployee.UniqueIdentifier == "Starcounter.Raw.Employee");
		ScAssertion.Assert(vEmployee.MaterializedTable.Equals(cEmployee.MaterializedTable));
		ScAssertion.Assert(vUser.Equals(vEmployee.Inherits));
		count = 0;
		foreach(RawView v in Db.SQL<RawView>("select v from rawview v where materializedtable.name = ? and fullname = ?", cOrganization.FullName, cOrganization.FullName)) {
			count++;
			vOrganization = v;
		}
		ScAssertion.Assert(count == 1);
		ScAssertion.Assert(vOrganization != null);
		ScAssertion.Assert(vOrganization.UniqueIdentifier == "Starcounter.Raw.Organization");
		ScAssertion.Assert(vOrganization.MaterializedTable.Equals(cOrganization.MaterializedTable));
		count = 0;
		foreach(RawView v in Db.SQL<RawView>("select v from rawview v where materializedtable.name = ? and fullname = ?", cCompany.FullName, cCompany.FullName)) {
			count++;
			vCompany = v;
		}
		ScAssertion.Assert(count == 1);
		ScAssertion.Assert(vCompany != null);
		ScAssertion.Assert(vCompany.UniqueIdentifier == "Starcounter.Raw.Company");
		ScAssertion.Assert(vCompany.MaterializedTable.Equals(cCompany.MaterializedTable));
		ScAssertion.Assert(vOrganization.Equals(vCompany.Inherits));
		count = 0;
		foreach(Column c in Db.SQL<Column>("select c from column c where c.Table = ?", cPerson))
			count++;
		ScAssertion.Assert(count == 2);
		string[] names = { "Person", "User", "Employee", "Organization", "Company" };
		int[] nrCols = { 2, 1, 1, 1, 1 };
		ScAssertion.Assert(names.Length == nrCols.Length);
		ClrClass[] clrClassses = new ClrClass[names.Length];
		RawView[] views = new RawView[names.Length];
		for(int i = 0; i < names.Length; i++) {
			count = 0;
			foreach(ClrClass c in Db.SQL<ClrClass>("select c from clrclass c where fullname = ?", names[i])) {
				clrClassses[i] = c;
				count++;
			}
			ScAssertion.Assert(count == 1);
			ScAssertion.Assert(clrClassses[i] != null);
			ScAssertion.Assert(clrClassses[i].Name == clrClassses[i].FullName);
			ScAssertion.Assert(clrClassses[i].UniqueIdentifier == names[i]);
		}
		ScAssertion.Assert(clrClassses[0].Equals(clrClassses[1].Inherits));
		ScAssertion.Assert(clrClassses[1].Equals(clrClassses[2].Inherits));
		ScAssertion.Assert(clrClassses[3].Equals(clrClassses[4].Inherits));
		for(int i = 0; i < names.Length; i++) {
			count = 0;
			foreach(RawView v in Db.SQL<RawView>("select v from rawview v where materializedtable.name = ? and fullname = ?", 
					clrClassses[i].FullName, clrClassses[i].FullName)) {
				count++;
				views[i] = v;
			}
			ScAssertion.Assert(count == 1);
			ScAssertion.Assert(views[i] != null);
			ScAssertion.Assert(views[i].UniqueIdentifier == "Starcounter.Raw." + names[i]);
			ScAssertion.Assert(views[i].MaterializedTable.Equals(clrClassses[i].MaterializedTable));
		}
		ScAssertion.Assert(views[0].Equals(views[1].Inherits));
		ScAssertion.Assert(views[1].Equals(views[2].Inherits));
		ScAssertion.Assert(views[3].Equals(views[4].Inherits));
		for(int i = 0; i < names.Length; i++) {
			count = 0;
			foreach(Column c in Db.SQL<Column>("select c from column c where c.Table = ?", clrClassses[i]))
				count++;
			ScAssertion.Assert(count == nrCols[i]);
		}
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