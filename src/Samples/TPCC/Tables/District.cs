using System;
using Starcounter;
using Starcounter.Internal;

namespace tpcc
{
  [Database]
  public class District
  {
    public static void CreateIndex()
    {
      DbWrap.CreateIndex("District_primary_key", "CREATE UNIQUE INDEX District_primary_key ON District (D_W_ID, D_ID)");
    }

    public long D_ID;

    public long D_W_ID;

    public string D_NAME;

    public string D_STREET_1;

    public string D_STREET_2;

    public string D_CITY;

    public string D_STATE;

    public string D_ZIP;

    public decimal D_TAX;

    public decimal D_YTD;
  }
}
