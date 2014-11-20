using Starcounter;

class Program {
	static void Main() {
		ScAssertion.Assert(Db.SQL("select u from user u option index (u NikcNameIndx)").First != null);
	}
}


[Database]
public class User : Person {
	public string UserName;
}
[Database]
public class Person {
	public string FirstName;
	public string LastName;
}