using System;
using Starcounter;
using System.Diagnostics;
using Starcounter.Metadata;

class Program {
	static void Main() {
		Employee e = null;
		Company c = null;
		Db.Transact(delegate {
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
		// Setting values to check
		string[] tblNames = { "Person", "User", "Employee", "Organization", "Company" };
		int[] nrCols = { 2, 1, 1, 1, 1 };
		int[] nrColsInh = { 2, 3, 4, 1, 2 };
		ClrClass[] clrClassses = new ClrClass[tblNames.Length];
		RawView[] views = new RawView[tblNames.Length];
		string[] colNames = { "FirstName", "LastName", "UserName", "Company", 
							"OrganizationId", "Head" };
		int[] colRepeat = { 3, 3, 2, 1, 2, 1 };
		int[] colTab = { 0, 0, 1, 2, 3, 4 };

		// Asserting integrity between values
		ScAssertion.Assert(tblNames.Length == nrColsInh.Length);
		ScAssertion.Assert(tblNames.Length == nrCols.Length);
		int totalNrCols = 0;
		for (int i = 0; i < tblNames.Length; i++) {
			totalNrCols += nrCols[i];
			ScAssertion.Assert(nrCols[i] <= nrColsInh[i]);
		}
		ScAssertion.Assert(totalNrCols == colNames.Length);
		ScAssertion.Assert(totalNrCols == colRepeat.Length);
		ScAssertion.Assert(totalNrCols == colTab.Length);
		
		int count = 0;
		for(int i = 0; i < tblNames.Length; i++) {
			count = 0;
			foreach(ClrClass c in Db.SQL<ClrClass>("select c from clrclass c where fullname = ?", tblNames[i])) {
				clrClassses[i] = c;
				count++;
			}
			ScAssertion.Assert(count == 1);
			ScAssertion.Assert(clrClassses[i] != null);
			ScAssertion.Assert(clrClassses[i].Name == clrClassses[i].FullName);
			ScAssertion.Assert(clrClassses[i].UniqueIdentifier == tblNames[i]);
		}
		ScAssertion.Assert(clrClassses[0].Equals(clrClassses[1].Inherits));
		ScAssertion.Assert(clrClassses[1].Equals(clrClassses[2].Inherits));
		ScAssertion.Assert(clrClassses[3].Equals(clrClassses[4].Inherits));
		for(int i = 0; i < tblNames.Length; i++) {
			count = 0;
			foreach(RawView v in Db.SQL<RawView>("select v from rawview v where fullname = ?", clrClassses[i].FullName)) {
				count++;
				views[i] = v;
			}
			ScAssertion.Assert(count == 1);
			ScAssertion.Assert(views[i] != null);
			ScAssertion.Assert(views[i].UniqueIdentifier == "Starcounter.Raw." + tblNames[i]);
		}
		ScAssertion.Assert(views[0].Equals(views[1].Inherits));
		ScAssertion.Assert(views[1].Equals(views[2].Inherits));
		ScAssertion.Assert(views[3].Equals(views[4].Inherits));

        // TODO: 
        // Disabled until issue 3204 (https://github.com/Starcounter/Starcounter/issues/3204) is solved for mapped properties.
        //for(int i = 0; i < tblNames.Length; i++) {
        //	count = 0;
        //	foreach(MappedProperty c in Db.SQL<MappedProperty>("select c from mappedProperty c where c.Table = ? order by c desc", 
        //			clrClassses[i])) {
        //		if (i < 3)
        //			ScAssertion.Assert(c.Name == colNames[0 + count]);
        //		else
        //			ScAssertion.Assert(c.Name == colNames[4 + count]);
        //		ScAssertion.Assert(count < nrColsInh[i]);
        //		ScAssertion.Assert(c.Table is ClrClass);
        //		count++;
        //	}
        //	ScAssertion.Assert(count == nrColsInh[i]);
        //}
        //for(int i = 0; i < totalNrCols; i++) {
        //	count = 0;
        //	foreach(MappedProperty c in Db.SQL<MappedProperty>("select c from MappedProperty c where name = ? and c.\"table\" is ? order by \"table\" desc", 
        //			colNames[i], typeof(ClrClass))) {
        //		ScAssertion.Assert(c.Table.Equals(clrClassses[colTab[i] + count]));
        //		count++;
        //	}
        //	ScAssertion.Assert(count == colRepeat[i]);
        //}
  //      for (int i = 0; i < tblNames.Length; i++) {
		//	count = 0;
		//	foreach(Column c in Db.SQL<Column>("select c from column c where c.Table = ? and name <> ? and name <> ? order by c desc", 
		//			views[i], "__id", "__setspecifier")) {
		//		if (i < 3)
		//			ScAssertion.Assert(c.Name == colNames[0 + count]);
		//		else
		//			ScAssertion.Assert(c.Name == colNames[4 + count]);
		//		ScAssertion.Assert(count < nrColsInh[i]);
		//		ScAssertion.Assert(c.Table is RawView);
		//		count++;
		//	}
		//	ScAssertion.Assert(count == nrColsInh[i]);
		//}
		//for(int i = 0; i < totalNrCols; i++) {
		//	count = 0;
		//	foreach(Column c in Db.SQL<Column>("select c from column c where name = ? and c.\"table\" is ? and name <> ?  and name <> ? order by \"table\" desc", 
		//			colNames[i], typeof(RawView), "__id", "__setspecifier")) {
		//		ScAssertion.Assert(c.Table.Equals(views[colTab[i] + count]));
		//		count++;
		//	}
		//	ScAssertion.Assert(count == colRepeat[i]);
		//}
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