using System;
using Starcounter;
using Starcounter.Internal;

namespace tpcc
{
  [Database]
  public class NewOrder
  {
    public static void CreateIndex()
    {
      DbWrap.CreateIndex("NewOrder_primary_key", "CREATE UNIQUE INDEX NewOrder_primary_key ON NewOrder (NO_W_ID, NO_D_ID, NO_O_ID)");
    }

    public ulong NO_O_ID;

    public long NO_D_ID;

    public long NO_W_ID;
  }
}
