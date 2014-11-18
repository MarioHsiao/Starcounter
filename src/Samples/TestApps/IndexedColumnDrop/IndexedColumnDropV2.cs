using Starcounter;

class Program {
	static void Main() {
		ScAssertion.Assert(Db.SQL("select u from user u option index (u NikcNameIndx)").First != null);
	}
}

[Database]
public class User {
	public string FirstName;
	public string LastName;
}