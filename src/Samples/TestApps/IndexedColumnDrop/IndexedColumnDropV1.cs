using Starcounter;

class Program {
	static void Main() {
		Db.SQL("create index NikcNameIndx on \"User\" (NickName)");
		Db.SQL("drop index auto on \"User\"");
		Db.Transaction(delegate {
			new User { FirstName = "Alex", LastName = "Ivanov", NickName = "Lion" };
		});
		ScAssertion.Assert(Db.SQL("select u from user u option index (u NikcNameIndx)").First != null);
	}
}

[Database]
public class User {
	public string FirstName;
	public string LastName;
	public string NickName;
}