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
	
	public class OrganisationType : Concept {
		public string somethingAboutIt;
	}
	
	public class HomeType : OrganisationType {
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
			if (Db.SQL("select a from ClassifiedHomeAd a").First == null)
#endif
			ulong counter = Db.SQL<ulong>("select max(objectno) from classifiedHomead").First;
            ScAssertion.Assert(counter > 0);
            Db.Transact(delegate {
				//var h = Db.SQL<HomeType>("select h from hometype h where name = ?","myhome").First;
				//h.Delete();
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
						UniqueIdentifier = (++counter).ToString(),
						PublicationOwner = orgLanfast,
						Home = homeTNN
					};
					new ClassifiedHomeAd {
						UniqueIdentifier = null,
						PublicationOwner = orgLanfast,
						Home = homeTNull
					};
					new ClassifiedHomeAd {
						UniqueIdentifier = (++counter).ToString(),
                        PublicationOwner = orgNN,
						Home = homeTNull
					};
					new ClassifiedHomeAd {
						UniqueIdentifier = (++counter).ToString(),
                        PublicationOwner = orgLanfast,
						Home = homeTNN
					};
                new ClassifiedHomeAd {
                    UniqueIdentifier = (++counter).ToString(),
                    PublicationOwner = orgNull,
                    Home = homeTNull
                };
                new ClassifiedHomeAd {
                    UniqueIdentifier = (++counter).ToString(),
                    PublicationOwner = orgNull,
                    Home = homeNull
                };
                new ClassifiedHomeAd {
						UniqueIdentifier = (++counter).ToString(),
                    PublicationOwner = orgLanfast,
						Home = homeTNull
					};
				});
			var res = Db.SQL<long>(@"select count(a) from classifiedhomead a 
				where publicationowner.domain=? and 
					home.hometype.name is null and 
					uniqueidentifier is not null", "www.lansfast.se").First;
			ScAssertion.Assert(res > 0);
			res = Db.SlowSQL<long>(@"select count(a) from classifiedhomead a 
				where publicationowner.domain='www.lansfast.se' and 
					home.hometype.name is null and 
					uniqueidentifier is not null").First;
			ScAssertion.Assert(res > 0);
#if false
			string query = Db.SlowSQL<long>(@"select count(a) from classifiedhomead a 
				where publicationowner.domain='www.lansfast.se' and 
					home.hometype.name is null and 
					uniqueidentifier is not null").GetEnumerator().ToString();
			Console.WriteLine(query);
#endif
        }
    }
}
