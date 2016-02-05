using Starcounter;
using System;
using Starcounter.Metadata;

class Program {
    static void Main() {
        Db.Transact(delegate {
            new Account {
                Client = new User { FirstName = "Peter", LastName = "Svensson" },
                Amount = 100m
            };
        });
        ScAssertion.Assert(Db.SQL<RawView>("SELECT v FROM RawView v WHERE Name = ?", "User").First != null);
        ScAssertion.Assert(Db.SQL<Column>("SELECT c FROM Column c WHERE c.Table.Name = ? AND c.Name = ?",
            "User", "LastName") != null);
        ScAssertion.Assert(Db.SQL<RawView>("SELECT v FROM RawView v WHERE Name = ?", "Account").First != null);
        ScAssertion.Assert(Db.SQL<Column>("SELECT c FROM Column c WHERE c.Table.Name = ? AND c.Name = ?",
            "Account", "Client") != null);
		try {
			Db.SQL("DROP TABLE Account");
		} catch (DbException e) {
			Console.WriteLine(e.Message);
		}
		int count = 0;
		Db.Transact(delegate {
			foreach(Account a in Db.SQL<Account>("SELECT a FROM Account a")) {
				a.Delete();
				count++;
			}
		});
		System.Diagnostics.Trace.Assert(count == 1);
    }
}

[Database]
public class User {
    public String FirstName;
    public String LastName;
}

[Database]
public class Account {
    public User Client;
    public Decimal Amount;
}
