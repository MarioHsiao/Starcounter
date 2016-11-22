using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Starcounter;
using Starcounter.Internal;

namespace tpcc.Transactions
{
  public static class DeliveryTransaction
  {
    public class InputData
    {
      public static InputData Generate(TpccValuesGenerator gen, int w_id, int d_id)
      {
        return new InputData()
        {
          W_ID = w_id,
          D_ID = d_id,
          O_CARRIER_ID = gen.random(1, 10)
        };

      }

      public int W_ID;
      public int D_ID;
      public int O_CARRIER_ID;
    }

    public class OutputData
    {
    }

    public static async Task<OutputData> Execute(InputData input)
    {
      var ret = new OutputData();

      await DbWrap.RetriableTransact("Delivery", () =>
      {
        int d_id = input.D_ID;

        NewOrder no = Db.SQL<NewOrder>("SELECT n FROM NewOrder n WHERE NO_W_ID=? AND NO_D_ID=?", input.W_ID, d_id)
                        .FirstOrDefault();

        if (no != null)
        {
          var no_o_id = no.NO_O_ID;
          no.Delete();

          Order o = Db.SQL<Order>("SELECT o FROM \"Order\" o WHERE O_W_ID=? AND O_D_ID=? AND O_ID=?", input.W_ID, d_id, no_o_id)
                      .Single();
          var o_c_id = o.O_C_ID;
          o.O_CARRIER_ID = input.O_CARRIER_ID;

          decimal total_ol_amount = 0;

          foreach (var ol in Db.SQL<OrderLine>("SELECT ol FROM OrderLine ol WHERE OL_W_ID=? AND OL_D_ID=? AND OL_O_ID=?", input.W_ID, d_id, no_o_id))
          {
            ol.OL_DELIVERY_D = DateTime.Now;
            total_ol_amount += ol.OL_AMOUNT;
          }

          Customer c = Db.SQL<Customer>("SELECT c FROM Customer c WHERE C_W_ID=? AND C_D_ID=? AND C_ID=?", input.W_ID, d_id, o_c_id).Single();

          c.C_BALANCE += total_ol_amount;
          ++c.C_DELIVERY_CNT;
        }
      });

      return ret;
    }
  }
}
