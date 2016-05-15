using System;
using Starcounter;

namespace Snippets {
	[Database]
	public class Account {
		public int AccountId;
		public int TypeId;
		public string Name;
		public DateTime Created;
	}
	
	class OffsetKey {
		static void Main() {
			if (Db.SQL("select i from starcounter.metadata.\"index\" i where name = ?", 
			"AccountIndx").First == null)
				Db.SQL("create index AccountIndx on Account (TypeId, Created DESC)");
			if (Db.SQL("select a from account a").First == null)
				Db.Transact(delegate {
					for (int i = 0; i < 10; i++)
						new Account { 
							AccountId = 100+i, 
							TypeId = 1,
							Name = "Acc"+i+" name", 
							Created = DateTime.Now.AddTicks(i*10) 
						};
				});
			Byte[] offsetkey = null;
			using (IRowEnumerator<Account> a = 
				Db.SQL<Account>("select a from account a where typeid = ? order by created desc offsetkey ?",
					1, offsetkey).GetEnumerator()) {
						int count = 0;
						bool calledBreak = false;
						while (a.MoveNext()) {
							count++;
							if (count > 5) {
								calledBreak = true;
								break;
							}
						}
						ScAssertion.Assert(calledBreak);
						offsetkey = a.GetOffsetKey();
						ScAssertion.Assert(offsetkey != null);
					}
		}
	}
}
