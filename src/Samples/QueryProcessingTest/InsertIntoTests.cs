using System;
using Starcounter;
using System.Diagnostics;

namespace QueryProcessingTest {
    public static class InsertIntoTests {
        public static void TestValuesInsertIntoWebVisits() {
            HelpMethods.LogEvent("Test insert into statements with values on web visit data model");
            UInt64 vId = (UInt64)Int64.MaxValue + 1;
            Db.Transaction(delegate {
                if (Db.SQL("select c from company c").First != null) {
                    WebPage w1 = Db.SQL<WebPage>("select w from webpage w where title = ?", "MyCompany, AboutUs").First;
                    w1.Delete();
                    foreach (Company c1 in Db.SQL<Company>("select c from company c")) {
                        foreach (Visit v1 in Db.SQL<Visit>("select v from visit v where company = ?", c1)) {
                            if (v1.GetObjectNo() == vId)
                                vId++;
                            foreach (Impression imp in Db.SQL<Impression>("select i from impression i where visit = ?", v1))
                                imp.Delete();
                            v1.Delete();
                        }
                        c1.Delete();
                    }
                    foreach (Country c1 in Db.SQL<Country>("select c from country c"))
                        c1.Delete();
                }
            });
            Db.Transaction(delegate {
                String query = "INSERT INTO WebPage (Title, uRL, PageValue, PersonalPageValue, TrackingCode, Located, deleted)" +
                    "Values ('MyCompany, AboutUs', '168.12.147.2/AboutUs', 100, -90, '', false, false)";
                Db.SQL(query);
            });
            WebPage w = Db.SQL<WebPage>("select w from Webpage w where title = ?", "MyCompany, AboutUs").First;
            Trace.Assert(w != null);
            Trace.Assert(w.Title == "MyCompany, AboutUs");
            Trace.Assert(w.URL == "168.12.147.2/AboutUs");
            Trace.Assert(w.PageValue == 100);
            Trace.Assert(w.PersonalPageValue == -90);
            Trace.Assert(w.TrackingCode == "");
            Trace.Assert(w.Located == false);
            Trace.Assert(w.Deleted == false);
            Db.Transaction(delegate { Db.SQL("insert into country(name) values ('Sweden'), ('Germany'), ('France')"); });
            
            var counEnum = Db.SQL<Country>("select c from country c").GetEnumerator();
            Trace.Assert(counEnum.MoveNext());
            Country c = counEnum.Current;
            Trace.Assert(c.Name == "Sweden");
            Trace.Assert(counEnum.MoveNext());
            c = counEnum.Current;
            Trace.Assert(c.Name == "Germany");
            Trace.Assert(counEnum.MoveNext());
            c = counEnum.Current;
            Trace.Assert(c.Name == "France");
            Trace.Assert(!counEnum.MoveNext());
            counEnum.Dispose();
            Db.Transaction(delegate { Db.SQL("insert into company (name,country) values ('Canal+',object " + c.GetObjectNo()+")"); });
            var compEnum = Db.SQL<Company>("select c from company c").GetEnumerator();
            Trace.Assert(compEnum.MoveNext());
            Company co = compEnum.Current;
            Trace.Assert(co.Name == "Canal+");
            Trace.Assert(co.Country.Equals(c));
            Trace.Assert(!compEnum.MoveNext());
            compEnum.Dispose();
            DateTime startV = Convert.ToDateTime("2006-11-01 00:08:40");
            DateTime endV = Convert.ToDateTime("2006-11-01 00:08:59");
            Db.Transaction(delegate {
                Db.SQL("insert into starcounter.raw.QueryProcessingTest.visit (id, company, start, end, UserAgent, ipbytes) values (" +
                    UInt64.MaxValue + ", object " + co.GetObjectNo() + "," + startV.Ticks + "," + endV.Ticks + ",'Opera',binary '01010101')");
            });
            Db.Transaction(delegate {
                Db.SQL("insert into starcounter.raw.QueryProcessingTest.visit (id, company, UserAgent, ipbytes) values (2, object " +
                    co.GetObjectNo() + ",'Opera',BINARY 'D91FA24E19FB065Ad')");
            });
            var visits = Db.SQL<Visit>("select v from visit v where company = ?", co).GetEnumerator();
            Trace.Assert(visits.MoveNext());
            Visit v = visits.Current;
            Trace.Assert(v.Id == UInt64.MaxValue);
            Trace.Assert(v.Company.Equals(co));
            Trace.Assert(v.UserAgent == "Opera");
            Trace.Assert(v.Start == startV);
            Trace.Assert(v.End == endV);
            Trace.Assert(v.IpBytes.Equals(new Binary(new byte[] { 1, 1, 1, 1 })));
            Trace.Assert(Db.BinaryToHex(v.IpBytes) == "01010101");
            Trace.Assert(visits.MoveNext());
            v = visits.Current;
            Trace.Assert(v.Id == 2);
            Trace.Assert(v.Company.Equals(co));
            Trace.Assert(v.UserAgent == "Opera");
            Trace.Assert(Db.BinaryToHex(v.IpBytes) == "0D91FA24E19FB065AD");
            Trace.Assert(!visits.MoveNext());
            visits.Dispose();
            visits = Db.SQL<Visit>("select v from visit v where Ipbytes = ? and id = ?",
                new Binary(new byte[] { 1, 1, 1, 1 }), UInt64.MaxValue).GetEnumerator();
            Trace.Assert(visits.MoveNext());
            v = visits.Current;
            Trace.Assert(v.Id == UInt64.MaxValue);
            Trace.Assert(v.Company.Equals(co));
            Trace.Assert(v.UserAgent == "Opera");
            Trace.Assert(v.Start == startV);
            Trace.Assert(v.End == endV);
            Trace.Assert(v.IpBytes.Equals(new Binary(new byte[] { 1, 1, 1, 1 })));
            Trace.Assert(Db.BinaryToHex(v.IpBytes) == "01010101");
            Trace.Assert(!visits.MoveNext());
            visits.Dispose();
            // Test insert __id value
            Db.Transaction(delegate { v.Delete(); });
            Db.SystemTransaction(delegate {
                Db.SQL("insert into starcounter.raw.QueryProcessingTest.visit (__id, id, company, UserAgent) values (object " +
                    vId + ",1, object " +
                    co.GetObjectNo() + ",'Opera')");
            });
            visits = Db.SQL<Visit>("select v from visit v where company = ?", co).GetEnumerator();
            Trace.Assert(visits.MoveNext());
            Trace.Assert(visits.MoveNext());
            v = visits.Current;
            Trace.Assert(v.Id == 1);
            Trace.Assert(v.Company.Equals(co));
            Trace.Assert(v.UserAgent == "Opera");
            Trace.Assert(v.GetObjectNo() == vId);
            Trace.Assert(!visits.MoveNext());
            visits.Dispose();
            Db.Transaction(delegate { Db.SQL("insert into impression(visit) values (object " + vId + ")"); });
            var impressions = Db.SQL<Impression>("select i from impression i where visit = ?", v).GetEnumerator();
            Trace.Assert(impressions.MoveNext());
            Impression impr = impressions.Current;
            Trace.Assert(impr != null);
            Trace.Assert(impr.Visit.Equals(v));
            Trace.Assert(!impressions.MoveNext());
            impressions.Dispose();
            Db.Transaction(delegate {
                Db.SQL("insert into QueryProcessingTest.user(userid,useridnr,birthday,firstname,lastname,nickname)" +
                    "values('SpecUser',1000000," + startV.Ticks + ",'Carl','Olofsson','')");
                var users = Db.SQL<User>("select u from user u where userid = ?", "SpecUser").GetEnumerator();
                Trace.Assert(users.MoveNext());
                User u = users.Current;
                Trace.Assert(u != null);
                Trace.Assert(u.UserId == "SpecUser");
                Trace.Assert(u.UserIdNr == 1000000);
                Trace.Assert(u.BirthDay == startV);
                Trace.Assert(u.FirstName == "Carl");
                Trace.Assert(u.LastName == "Olofsson");
                Trace.Assert(u.AnotherNickName == "");
                Trace.Assert(u.PatronymicName == null);
                Trace.Assert(!users.MoveNext());
                users.Dispose();
                Db.SQL("insert into account(accountid,client,amount,accounttype,notactive,amountdouble)"+
                    "values(1000000,object "+u.GetObjectNo()+",-10.2,'savings',false,10.2)");
                var accounts = Db.SQL<Account>("select a from account a where client = ?", u).GetEnumerator();
                Trace.Assert(accounts.MoveNext());
                Account a = accounts.Current;
                Trace.Assert(a != null);
                Trace.Assert(a.AccountId == 1000000);
                Trace.Assert(a.Client.Equals(u));
                Trace.Assert(a.Amount == -10.2m);
                Trace.Assert(a.AccountType == "savings");
                Trace.Assert(a.NotActive == false);
                Trace.Assert(a.AmountDouble == 10.2);
                Trace.Assert(!accounts.MoveNext());
                accounts.Dispose();
                a.Delete();
                Db.SQL("insert into account(accountid,client,amount,accounttype,notactive,amountdouble)" +
                    "values(1000000,object " + u.GetObjectNo() + ",10.243000,'savings',false,10)");
                accounts = Db.SQL<Account>("select a from account a where client = ?", u).GetEnumerator();
                Trace.Assert(accounts.MoveNext());
                a = accounts.Current;
                Trace.Assert(a != null);
                Trace.Assert(a.AccountId == 1000000);
                Trace.Assert(a.Client.Equals(u));
                Trace.Assert(a.Amount == 10.243000m);
                Trace.Assert(a.AccountType == "savings");
                Trace.Assert(a.NotActive == false);
                Trace.Assert(a.AmountDouble == 10);
                Trace.Assert(!accounts.MoveNext());
                accounts.Dispose();
                a.Delete();
                u.Delete();
            });
            Trace.Assert(Db.SQL<User>("select u from user u where userid = ?", "SpecUser").First == null);
            Trace.Assert(Db.SQL<Account>("select a from account a where accountid = ?", 1000000).First == null);
            int nrUnknCOmp = 0;
            foreach (Company cc in Db.SQL<Company>("select c from company c where c.Country.name = ?", "Unknown"))
                nrUnknCOmp++;
            Db.Transaction(delegate {
                Company comp = new Company { Country = new Country { Name = "Unknown" }, Name = "Single\'quoted\'name" };
                comp = new Company { Country = new Country { Name = "Unknown" }, Name = "Double\"quoted\"\nname" };
            });
            int nrUnknCOmpA = 0;
            foreach (Company cc in Db.SQL<Company>("select c from company c where c.Country.name = ?", "Unknown"))
                nrUnknCOmpA++;
            Trace.Assert(nrUnknCOmp + 2 == nrUnknCOmpA);
            HelpMethods.LogEvent("Finished testing insert into statements with values on web visit data model");
        }
    }
}
