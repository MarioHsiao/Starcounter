using System;
using Starcounter;
using Starcounter.Internal;

namespace tpcc
{
  [Database]
  public class Order
  {
    public static void CreateIndex()
    {
      DbWrap.CreateIndex("Order_primary_key", "CREATE UNIQUE INDEX Order_primary_key ON \"Order\" (O_W_ID, O_D_ID, O_ID)");
      DbWrap.CreateIndex("Orders_by_customer_ordered_by_order_id", "CREATE INDEX Orders_by_customer_ordered_by_order_id ON \"Order\" (O_W_ID, O_D_ID, O_C_ID, O_ID)");
    }

    public ulong O_ID;

    public long O_D_ID;

    public long O_W_ID;

    public long O_C_ID;

    public DateTime O_ENTRY_D;

    public long O_CARRIER_ID;

    public long O_OL_CNT;

    public long O_ALL_LOCAL;
  }
}
