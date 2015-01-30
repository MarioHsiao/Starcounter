using Starcounter;
using System;
using Starcounter.Metadata;

class Program {
    static void Main() {
        ScAssertion.Assert(Db.SQL<RawView>("SELECT v FROM RawView v WHERE Name = ?", "User").First != null);
        ScAssertion.Assert(Db.SQL<Column>("SELECT c FROM Column c WHERE c.Table.Name = ? AND c.Name = ?",
            "User", "LastName") != null);
        ScAssertion.Assert(Db.SQL<RawView>("SELECT v FROM RawView v WHERE Name = ?", "Account").First != null);
        ScAssertion.Assert(Db.SQL<Column>("SELECT c FROM Column c WHERE c.Table.Name = ? AND c.Name = ?",
            "Account", "Client") != null);
        Db.SQL("DROP TABLE Account");
        ScAssertion.Assert(Db.SQL<RawView>("SELECT v FROM RawView v WHERE Name = ?", "User").First != null);
        ScAssertion.Assert(Db.SQL<Column>("SELECT c FROM Column c WHERE c.Table.Name = ? AND c.Name = ?",
            "User", "LastName") != null);
        ScAssertion.Assert(Db.SQL<RawView>("SELECT v FROM RawView v WHERE Name = ?", "Account").First == null);
        ScAssertion.Assert(Db.SQL<Column>("SELECT c FROM Column c WHERE c.Table.Name = ? AND c.Name = ?",
            "Account", "Client").First == null);
    }
}

[Database]
public class User {
    public String FirstName;
    public String LastName;
}
