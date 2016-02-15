using Starcounter;

class Program {
	static void Main() {
		Person p = null;
		Account a = null;
		DailyAccount d = null;
		Db.Transact(delegate {
			p = new Person { FirstName = "Fname" };
			a = new Account { AccountId = 1 };
			d = new DailyAccount { AccountId = 2, Amount = 100.0m };
		});
		Person q = Db.SQL<Person>("select p from Person p where LastName IS NULL").First;
		ScAssertion.Assert(q != null, "At least one person with LastName IS NULL should be found.");
		q = Db.SQL<Person>("select p from Person p where LastName = ?", null).First;
		ScAssertion.Assert(q == null, "Should be no hits on person with LastName = null.");
		q = Db.SQL<Person>("select p from Person p where NickName = ?", null).First;
		ScAssertion.Assert(q == null, "Should be no hits on person with NickName = null.");
		Account f = Db.SQL<Account>("select a from Account a where Client IS NULL").First;
		ScAssertion.Assert(f != null, "At least one account with Client IS NULL should be found.");
		f = Db.SQL<Account>("select a from Account a where Client = ?", null).First;
		ScAssertion.Assert(f == null, "Should be no hits on account with Client = null.");
		DailyAccount r = Db.SQL<DailyAccount>("select d from dailyaccount d where Client = ?", null).First;
		ScAssertion.Assert(r == null);
		int count = 0;
		foreach(Account account in Db.SQL<Account>("select a from Account a where a.Client IS NULL"))
			count++;
		ScAssertion.Assert(count == 2, "Two account with Client = null should be found.");
		count = 0;
		foreach(Account account in Db.SQL<Account>("select a from Account a where a.Client=?", null))
			count++;
		ScAssertion.Assert(count == 0, "No hits on account with Client = null should be found.");
		Db.Transact(delegate {
			d.Delete();
			a.Delete();
			p.Delete();
		});
	}
}

[Database]
public class Person {
	public string FirstName;
	public string LastName;
	public string NickName { get; set; }
}

[Database]
public class Account {
	public int AccountId;
	public Person Client;
}

public class DailyAccount : Account {
	public decimal Amount { get; set; }
}