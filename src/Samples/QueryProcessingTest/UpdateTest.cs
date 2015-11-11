using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter;
using System.Diagnostics;



namespace QueryProcessingTest
{
  [Database]
  public class UpdateTestItem
  {
    public int field;
  }

  class UpdateTest
  {
    class TransactionAbort : Exception
    {
    }

    public static void Run()
    {
      HelpMethods.LogEvent("Test Db.Update");

      //no arrange 

      try
      {
        Db.Transact(() =>
        {
          //act
          Db.Update("INSERT INTO UpdateTestItem (field) VALUES (1)");


          //check
          IEnumerable<UpdateTestItem> r = Db.SQL<UpdateTestItem>("SELECT i FROM UpdateTestItem i");
          Trace.Assert(r.Count() == 1);
          Trace.Assert(r.First().field == 1);


          HelpMethods.LogEvent("Db.Update test finished");

          //abort transaction to avoid database cleanup problems
          throw new TransactionAbort();
        });
      }
      catch (TransactionAbort)
      { }
    }
  }
}
