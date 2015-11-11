using System;
using Starcounter;
using Starcounter.Internal;

namespace tpcc
{
  [Database]
  public class Stock
  {
    public static void CreateIndex()
    {
      DbWrap.CreateIndex("Stock_primary_key", "CREATE UNIQUE INDEX Stock_primary_key ON Stock (S_W_ID, S_I_ID)");
    }

    public long S_I_ID;

    public long S_W_ID;

    public long S_QUANTITY;

    public string S_DIST_01;

    public string S_DIST_02;

    public string S_DIST_03;

    public string S_DIST_04;

    public string S_DIST_05;

    public string S_DIST_06;

    public string S_DIST_07;

    public string S_DIST_08;

    public string S_DIST_09;

    public string S_DIST_10;

    public long S_YTD;

    public long S_ORDER_CNT;

    public long S_REMOTE_CNT;

    public string S_DATA;
  }
}
