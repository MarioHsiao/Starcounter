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

      //arrange 

      //act
      try
      {
        Db.Transact(() =>
        {
          Db.Update("INSERT INTO UpdateTestItem (field) VALUES (1)");

          IEnumerable<UpdateTestItem> r = Db.SQL<UpdateTestItem>("SELECT i FROM UpdateTestItem i");
          Trace.Assert(r.Count() == 1);
          Trace.Assert(r.First().field == 1);


          HelpMethods.LogEvent("Db.Update test finished");

          //Doesn't make sense to test. Just to supress CS0649 comilation error
          r.First().field = 0;

          throw new TransactionAbort();
        });
      }
      catch (TransactionAbort)
      { }
    }
  }
}
