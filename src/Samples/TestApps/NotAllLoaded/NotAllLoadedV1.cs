using Starcounter;
using System;

class Program {
	static void Main() {
		Db.Transaction(delegate {
			new Person { Name = "JustPerson" };
			new User { Name = "AUser", UserId = 1 };
		});
		var persons = Db.SQL<Person>("select p from Person p").GetEnumerator();
		ScAssertion.Assert(persons.MoveNext());
		ScAssertion.Assert(persons.Current.Name == "JustPerson");
		ScAssertion.Assert(persons.MoveNext());
		ScAssertion.Assert(persons.Current.Name == "AUser");
		ScAssertion.Assert(!persons.MoveNext());
	}
}

[Database]
public class Person {
	public String Name;
}

[Database]
public class User : Person {
	public int UserId;
}