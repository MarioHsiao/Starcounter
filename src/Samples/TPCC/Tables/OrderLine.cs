using System;
using Starcounter;
using Starcounter.Internal;

namespace tpcc
{
  [Database]
  public class OrderLine
  {
    public static void CreateIndex()
    {
      DbWrap.CreateIndex("OrderLine_primary_key", "CREATE UNIQUE INDEX OrderLine_primary_key ON OrderLine (OL_W_ID, OL_D_ID, OL_O_ID, OL_NUMBER)");
    }

    public ulong OL_O_ID;

    public long OL_D_ID;

    public long OL_W_ID;

    public long OL_NUMBER;

    public long OL_I_ID;

    public long OL_SUPPLY_W_ID;

    public DateTime OL_DELIVERY_D;

    public long OL_QUANTITY;

    public decimal OL_AMOUNT;

    public string OL_DIST_INFO;
  }
}
