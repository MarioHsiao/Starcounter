using System;
using Starcounter;
using Starcounter.Internal;

namespace tpcc
{
  [Database]
  public class Item
  {
    public static void CreateIndex()
    {
      DbWrap.CreateIndex("Item_primary_key", "CREATE UNIQUE INDEX Item_primary_key ON Item (I_ID)");
    }

    public long I_ID;

    public long I_IM_ID;

    public string I_NAME;

    public decimal I_PRICE;

    public string I_DATA;
  }
}
