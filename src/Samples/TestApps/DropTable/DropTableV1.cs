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
