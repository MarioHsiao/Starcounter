using System;
using Starcounter;
using System.Linq;

namespace tpcc
{
  public static class DbWrap
  {
    static public void RetriableTransact(string name, Action fn)
    {
      System.Threading.Interlocked.Increment(ref Program.tr_count);
      while (true)
      {
        try
        {
          Db.Transact(fn,1);
          return;
        }
        catch (Exception e) when (e?.InnerException is ITransactionConflictException)
        {
          var v = Program.retry_count.GetOrAdd(name, new Program.V());
          System.Threading.Interlocked.Increment( ref v.i );
        }
      }
    }

    static public void CreateIndex(string index_name, string query)
    {
      bool index_exist = false;
      Db.Transact(() => index_exist = Db.SQL("SELECT i FROM Starcounter.Metadata.\"Index\" i WHERE Name = ?", index_name).Any());
      if (!index_exist)
        Db.SQL(query);
    }
  }
}
