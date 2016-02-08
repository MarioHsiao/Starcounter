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
		// Trying to drop a table for a loaded class.
		Exception ex = null;
		try {
			Db.SQL("DROP TABLE Account");
		} catch (Exception e) {
			ex = e;
		}
		ScAssertion.Assert(ex != null);
		ScAssertion.Assert(ex.Message.Substring(0, 34).Equals("ScErrDropTypeNotEmpty (SCERR15006)"));
		ScAssertion.Assert(ex.Message.Contains("Account"));
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
