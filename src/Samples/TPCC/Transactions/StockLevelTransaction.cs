using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Starcounter;
using Starcounter.Internal;

namespace tpcc.Transactions
{
  public static class StockLevelTransaction
  {
    public class InputData
    {
      public static InputData Generate(TpccValuesGenerator gen, int w_id, int d_id)
      {
        return new InputData()
        {
          W_ID = w_id,
          D_ID = d_id,
          threshold = gen.random(10, 20)
        };

      }

      public int W_ID;
      public int D_ID;
      public int threshold;
    }

    public class OutputData
    {
      public int low_stock;
    }

    public static async Task<OutputData> Execute(InputData input)
    {
      var ret = new OutputData();

      await DbWrap.RetriableTransact( "StockLevel", () =>
      {
        District d = Db.SQL<District>("SELECT d FROM District d WHERE D_W_ID=? AND D_ID=?", input.W_ID, input.D_ID).Single();

        ret.low_stock = Db.SQL<OrderLine>("SELECT ol FROM OrderLine ol WHERE OL_W_ID=? AND OL_D_ID=? ORDER BY OL_W_ID DESC,OL_D_ID DESC,OL_O_ID DESC,OL_NUMBER DESC", input.W_ID, input.D_ID)
                          .Take(20)
                          .Select(ol => ol.OL_I_ID)
                          .Distinct()
                          .SelectMany(ol_i_id => Db.SQL<Stock>("SELECT c FROM Stock c WHERE S_W_ID=? AND S_I_ID=?", input.W_ID, ol_i_id)
                                                   .Where(s => s.S_QUANTITY < input.threshold))
                          .Count();
      });

      return ret;
    }
  }
}
