using Starcounter;
using System;

class Program {
	static void Main() {
		var persons = Db.SQL<Person>("select p from Person p").GetEnumerator();
		ScAssertion.Assert(persons.MoveNext());
		ScAssertion.Assert(persons.Current.Name == "JustPerson");
		DbException mismatch = null;
		try {
			ScAssertion.Assert(persons.MoveNext());
		} catch (DbException e) {
			mismatch = e;
		}
		ScAssertion.Assert(mismatch != null);
		ScAssertion.Assert((uint)mismatch.Data[ErrorCode.EC_TRANSPORT_KEY] == Error.SCERRSCHEMACODEMISMATCH);
	}
}

[Database]
public class Person {
	public String Name;
}
