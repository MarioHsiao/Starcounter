using System;
using Starcounter;

namespace Snippets {
	[Database]
	public class Concept {
		public string Name;
	}
	
	public class Organisation : Concept {
		public string Domain;
	}
	
	public class HomeType : Concept {
	}
	
	public class Home : Concept {
		public HomeType HomeType;
	}
	
	public class ClassifiedHomeAd : Concept {
		public Organisation PublicationOwner;
		public Home Home;
		public string UniqueIdentifier;
	}
	
	class SqlQuery {
		static void Main() {
#if false
			if (Db.SQL("select i from starcounter.metadata.\"index\" i where name = ?", 
			"UniqueIdentifierIndx").First == null)
				Db.SQL("create index UniqueIdentifierIndx on ClassifiedHomeAd (UniqueIdentifier)");
#endif
			if (Db.SQL("select a from ClassifiedHomeAd a").First == null)
				Db.Transact(delegate {
					Organisation orgNN = new Organisation { Domain = "myorg.com"};
					Organisation orgNull = new Organisation();
					Organisation orgLanfast = new Organisation { Domain = "www.lansfast.se" };
					HomeType homeTypeNN = new HomeType { Name = "myhome"};
					HomeType homeTypeNull = new HomeType();
					Home homeTNN = new Home { HomeType = homeTypeNN};
					Home homeNull = new Home();
					Home homeTNull = new Home { HomeType = homeTypeNull};
					new ClassifiedHomeAd();
					new ClassifiedHomeAd {
						UniqueIdentifier = null,
						PublicationOwner = orgNN,
						Home = homeTNN
					};
					new ClassifiedHomeAd {
						UniqueIdentifier = "1",
						PublicationOwner = orgLanfast,
						Home = homeTNN
					};
					new ClassifiedHomeAd {
						UniqueIdentifier = null,
						PublicationOwner = orgLanfast,
						Home = homeTNull
					};
					new ClassifiedHomeAd {
						UniqueIdentifier = "3",
						PublicationOwner = orgNN,
						Home = homeTNull
					};
					new ClassifiedHomeAd {
						UniqueIdentifier = "4",
						PublicationOwner = orgLanfast,
						Home = homeTNN
					};
					new ClassifiedHomeAd {
						UniqueIdentifier = "2",
						PublicationOwner = orgLanfast,
						Home = homeTNull
					};
				});
			var res = Db.SQL<long>(@"select count(a) from classifiedhomead a 
				where publicationowner.domain=? and 
					home.hometype.name is null and 
					uniqueidentifier is not null", "www.lansfast.se").First;
			ScAssertion.Assert(res == 1);
			res = Db.SlowSQL<long>(@"select count(a) from classifiedhomead a 
				where publicationowner.domain='www.lansfast.se' and 
					home.hometype.name is null and 
					uniqueidentifier is not null").First;
			ScAssertion.Assert(res == 1);
#if false
			string query = Db.SlowSQL<long>(@"select count(a) from classifiedhomead a 
				where publicationowner.domain='www.lansfast.se' and 
					home.hometype.name is null and 
					uniqueidentifier is not null").GetEnumerator().ToString();
			Console.WriteLine(query);
		}
#endif
	}
}
