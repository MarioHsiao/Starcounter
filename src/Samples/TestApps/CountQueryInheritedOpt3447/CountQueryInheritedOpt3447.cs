using Starcounter;
using System;

namespace AnApp {
class Program {
	static void Main() {
		if (Db.SQL("select i from starcounter.metadata.\"index\" i where i.name = ?", "CompanyIndex").First == null)
			Db.SQL("create index CompanyIndex on AnApp.Company (Name)");
		Db.Transact(delegate {
			PublicationOwner p = new PublicationOwner { Name = "publ" };
			new Seller { PublicationOwner = p, Name = "seller" };
		});
		long count = Db.SQL<long>("select count(s) from anapp.seller s where publicationowner.name like ?", "publ").First;
        ScAssertion.Assert(count > 0);
		count = Db.SQL<long>("select count(s) from anapp.seller s where s.publicationowner.name = ?", "publ").First;
        ScAssertion.Assert(count > 0);
	}
}

    [Database]
    public class Concept {
        public string Name;
        public string ObId { get { return DbHelper.GetObjectID(this); } }
    }

    public class Agent : Concept {
    }
	
    public class Company : Agent
    {
        public string Country;
    }

    public class Seller : Company
    {
        //Unique values are name and PublicationOwner.
        public PublicationOwner PublicationOwner;
        //public AdFenix.Database.Advertiser Advertiser;
        public bool EmailNotification;
        public bool SingleOfferActive;
        public bool MultiOfferActive;
        public decimal CostShare;
        public string Email;
        public string LeadAdFormId;
        public bool AutomaticallyPublish;
    }
	
    public class PublicationOwner : Agent
    {
        public bool UserProfile;
        public string BaseUrl;
        public string LeadAdFormId;
        public bool IsSellingPackages;
        public string DefaultEmail;
        public bool UsingEncryption;
        public string DefaultCustomAudienceId;
        public string Language;
        public string HashTag;
    }
}
