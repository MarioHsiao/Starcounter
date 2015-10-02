using System;
using Starcounter;
using System.Diagnostics;

namespace Snippets {
	[Database]
	public class Account {
		public int AccountId;
	}
	
  class OffsetKey {
	  static void Main() {
		  OffsetKeyTest();
	  }
    public static void OffsetKeyTest() {
      Db.Scope(() => {
        Db.SlowSQL("DELETE FROM Snippets.Account");

        for (var i = 1; i < 20; i++) {
          new Account() {
            AccountId = i
          };
        }

        byte[] k = null;
		int j = 0;
        using (IRowEnumerator<Account> e = Db.SQL<Account>("SELECT a FROM Snippets.Account a WHERE a.AccountId < ? FETCH ?", 100, 10).GetEnumerator()) {
          while (e.MoveNext()) {
            Account a = e.Current;
            Console.Write(a.AccountId + " ");
			j++;
          }
          k = e.GetOffsetKey();
		  Debug.Assert(j <= 10);
        }
        Debug.Assert(k != null);
        Console.WriteLine();
        using (IRowEnumerator<Account> e = Db.SQL<Account>("SELECT a FROM Snippets.Account a WHERE a.AccountId < ? FETCH ? OFFSETKEY ?", 100, 5, k).GetEnumerator()) {
          while (e.MoveNext()) {
            Account a = e.Current;
            Console.Write(a.AccountId + " ");
          }
          k = e.GetOffsetKey();
        }
        Console.WriteLine();
      });
    }
  }
}
