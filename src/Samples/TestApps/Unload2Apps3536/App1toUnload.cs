using Starcounter;
using System;

class Program {
	static void Main() {
		Db.Transact(delegate {
			new Person {
				FirstName = "Leo",
				LastName = "Smith",
				NickName = "Lion" 
			};
			new Person {
				FirstName = "Olof",
				LastName = "Svensson"
			};
		});
	}
}

[Database]
public class Person {
	public string FirstName;
	public string LastName;
	public string NickName;
}