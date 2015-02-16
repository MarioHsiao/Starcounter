using Starcounter;
using System;

class Program {
	static void Main() {
        SqlException ex = null;
        try {
            Db.SQL("create index PersonAgeIndx on Person (NickName, Age)");
        } catch (SqlException e) {
            ex = e;
        }
        ScAssertion.Assert(ex != null);
        ScAssertion.Assert((uint)ex.Data[ErrorCode.EC_TRANSPORT_KEY] == Error.SCERRSQLUNKNOWNNAME);
        ScAssertion.Assert(ex.Message.Contains("Property Age in table Person is not a stored property."));
	}
}

[Database]
public class Person {
	public string FirstName;
	public string LastName;
	public string NickName;
    public int Age { get { int age = 3; return age; } }
}