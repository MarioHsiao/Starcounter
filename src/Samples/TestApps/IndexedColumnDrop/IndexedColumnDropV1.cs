using Starcounter;

class Program {
	static void Main() {
		Db.SQL("create index PersonNikcNameIndx on \"Person\" (NickName)");
		Db.SQL("create index UserNikcNameIndx on \"User\" (NickName)");
		Db.SQL("drop index auto on \"User\"");
		Db.Transact(delegate {
			new User { FirstName = "Alex", LastName = "Ivanov", NickName = "Lion" };
		});
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
	public string NickName;
}