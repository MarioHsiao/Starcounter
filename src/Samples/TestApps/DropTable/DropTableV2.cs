using Starcounter;
using System;
using Starcounter.Metadata;

class Program {
    static void Main() {
		Exception ex = null;
		try {
			Db.SQL("DROP TABLE \"User\"");
		} catch (Exception e) {
			ex = e;
		}
		ScAssertion.Assert(ex != null);
		ScAssertion.Assert(ex.Message.Substring(0, 34).Equals("ScErrDropTypeNotEmpty (SCERR15006)"));
		ScAssertion.Assert(ex.Message.Contains("User"));
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
