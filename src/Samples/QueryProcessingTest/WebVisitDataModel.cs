using Starcounter;
using System;

namespace QueryProcessingTest {
    [Database]
    public class WebPage {
        public string Title;
        public string URL;
        public int PageValue;
        public int PersonalPageValue;
        public string TrackingCode;
        public bool Located;
        public bool Deleted;
    }

    [Database]
    public class Impression {
        public Visit Visit;
        public DateTime Start;
        public WebPage WebPage;
        public String QueryString;
    }

    // A Visit consists of Webpage Impressions
    [Database]
    public class Visit {
        public ulong Id;
        public Company Company;
        public DateTime Start;
        public DateTime End;
        public int Spent;
        public int PageViewCount;
        public string Ip; //note: Henrik said they never show Ip in the interface, it is not even stored in PF3 database
        public string Referer;
        public string UserAgent;
        public string TrackingCookie;
        public string Protocol;
        public string DomainName;
    }

    [Database]
    public class Company {
        public string Name;
        public Country Country;
    }

    [Database]
    public class Country {
        public string Name;
    }
}
