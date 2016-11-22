using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Starcounter;
using Starcounter.Internal;

namespace tpcc.Transactions
{
  public static class OrderStatusTransaction
  {
    public class InputData
    {
      public static InputData Generate(TpccValuesGenerator gen, int w_id)
      {
        int y = gen.random(1, 100);

        return new InputData()
        {
          W_ID = w_id,
          D_ID = gen.random(1, DataLoader.D_ID_limit),
          C_LAST = (y <= 60) ? gen.generate_C_LAST(gen.NURand(255, 0, 999)) : null,
          C_ID = (y > 60) ? gen.NURand(1023, 1, DataLoader.C_ID_limit) : 0,

        };
      }
      public long W_ID;
      public long D_ID;
      public string C_LAST;
      public long C_ID;
    }

    public class OutputData
    {
      public class ItemData
      {
        public long OL_SUPPLY_W_ID;
        public long OL_I_ID;
        public long OL_QUANTITY;
        public decimal OL_AMOUNT;
        public DateTime OL_DELIVERY_D;
      }

      public string C_FIRST;
      public decimal C_BALANCE;
      public ulong O_ID;
      public DateTime O_ENTRY_D;
      public long O_CARRIER_ID;
      public List<ItemData> items = new List<ItemData>();
    }

    public static async Task<OutputData> Execute(InputData input)
    {
      var ret = new OutputData();

      await DbWrap.RetriableTransact("OrderStatus", () =>
      {
        Warehouse w = Db.SQL<Warehouse>("SELECT w FROM Warehouse w WHERE W_ID=?", input.W_ID).Single();

        Customer c;
        var c_id = input.C_ID;
        if (string.IsNullOrEmpty(input.C_LAST)) //case 1 of 2.6.2.2
        {
          c = Db.SQL<Customer>("SELECT c FROM Customer c WHERE C_W_ID=? AND C_D_ID=? AND C_ID=?", input.W_ID, input.D_ID, c_id).Single();
          var c_last = c.C_LAST;
        }
        else //case 2 of 2.6.2.2
        {
          var customers_with_the_same_last_name = Db.SQL<Customer>("SELECT c FROM Customer c WHERE C_W_ID=? AND C_D_ID=? AND C_LAST=?",
                                                                   input.W_ID, input.D_ID, input.C_LAST)
                                                    .OrderBy(cust => cust.C_FIRST)
                                                    .ToArray();
          c = customers_with_the_same_last_name[(customers_with_the_same_last_name.Count() - 1) / 2];
          c_id = c.C_ID;
        }

        ret.C_BALANCE = c.C_BALANCE;
        ret.C_FIRST = c.C_FIRST;
        var c_middle = c.C_MIDDLE;

        Order o = Db.SQL<Order>("SELECT o FROM \"Order\" o WHERE O_W_ID=? AND O_D_ID=? AND O_C_ID=? ORDER BY O_ID DESC", input.W_ID, input.D_ID, c_id).First();

        ret.O_ID = o.O_ID;
        ret.O_ENTRY_D = o.O_ENTRY_D;
        ret.O_CARRIER_ID = o.O_CARRIER_ID;

        foreach (var ol in Db.SQL<OrderLine>("SELECT ol FROM OrderLine ol WHERE OL_W_ID=? AND OL_D_ID=? AND OL_O_ID=?", input.W_ID, input.D_ID, ret.O_ID))
        {
          var item = new OutputData.ItemData();

          item.OL_I_ID = ol.OL_I_ID;
          item.OL_SUPPLY_W_ID = ol.OL_SUPPLY_W_ID;
          item.OL_QUANTITY = ol.OL_QUANTITY;
          item.OL_AMOUNT = ol.OL_AMOUNT;
          item.OL_DELIVERY_D = ol.OL_DELIVERY_D;

          ret.items.Add(item);
        }
      });

      return ret;
    }
  }
}
