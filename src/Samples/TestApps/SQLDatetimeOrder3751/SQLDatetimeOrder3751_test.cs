using System;
using Starcounter;
using System.Diagnostics;

namespace StarcounterApplicationTask
{
    [Database]
    public class Invoice
    {
        public DateTime Date;

        public Invoice(DateTime date)
        {
            this.Date = date;
        }
    }

    class Program
    {
        static void Main()
        {
            // create acsending order index if needed
            if (Db.SQL("SELECT i FROM Starcounter.Metadata.\"Index\" i WHERE Name = ?", "Asc_Index_on_Date").First == null)
            {
                Db.SQL("CREATE INDEX Asc_Index_on_Date ON StarcounterApplicationTask.Invoice (\"Date\" ASC)");
            }

            // populate data if needed
            int count = (int)Db.SQL<long>("SELECT COUNT(i) FROM StarcounterApplicationTask.Invoice i").First;
            if (count < 2)
            {
                for (int i = 0; i < 5; i++)
                {
                    DateTime cur = DateTime.Now;

                    Db.Transact(
                    delegate ()
                    {
                        addRow(cur);
                    }
                );
                }
            }

            // check that we have the bug
            DateTime date = Db.SQL<DateTime>("SELECT i.\"Date\" FROM StarcounterApplicationTask.Invoice i").First;
            QueryResultRows<Invoice> resultSuccess = Db.SQL<Invoice>("SELECT i FROM StarcounterApplicationTask.Invoice i WHERE i.\"Date\"> ? ORDER BY i.\"Date\" ASC", date);
            foreach(Invoice i in resultSuccess)
            {
                ScAssertion.Assert(i.Date != date, "We have problem with ORDER BY usage with backwards oredered index");
            }
            QueryResultRows<Invoice> resultFail = Db.SQL<Invoice>("SELECT i FROM StarcounterApplicationTask.Invoice i WHERE i.\"Date\"> ? ORDER BY i.\"Date\" DESC", date);
            foreach(Invoice i in resultFail)
            {
                ScAssertion.Assert(i.Date != date, "We have problem with ORDER BY usage with backwards oredered index");
            }
        }

        private static void addRow(DateTime date)
        {
            new Invoice(date);
        }
    }
}