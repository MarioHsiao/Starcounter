using System;
using Starcounter;
using Starcounter.Internal;

namespace tpcc
{
  [Database]
  public class Warehouse
  {
    public static void CreateIndex()
    {
      DbWrap.CreateIndex("Warehouse_primary_key", "CREATE UNIQUE INDEX Warehouse_primary_key ON Warehouse (W_ID)");
    }

    public long W_ID;

    public string W_NAME;

    public string W_STREET_1;

    public string W_STREET_2;

    public string W_CITY;

    public string W_STATE;

    public string W_ZIP;

    public decimal W_TAX;

    public decimal W_YTD;
  }
}
