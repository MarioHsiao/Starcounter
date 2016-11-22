using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Starcounter;
using Starcounter.Internal;

namespace tpcc.Transactions
{
  public static class NewOrderTransaction
  {
    public class InputData
    {
      public static InputData Generate(TpccValuesGenerator gen, int w_id)
      {
        int ol_cnt = gen.random(5, 15);
        int rbk = gen.random(1, 100);

        return new InputData()
        {
          W_ID = w_id,
          D_ID = gen.random(1, DataLoader.D_ID_limit),
          C_ID = gen.NURand(1023, 1, DataLoader.C_ID_limit),
          OL_CNT = ol_cnt,
          items = Enumerable.Range(1, ol_cnt).Select(
                i =>
                new ItemData
                {
                  OL_I_ID = i == ol_cnt && rbk == 1 ?
                                DataLoader.I_ID_limit + 1 :
                                gen.NURand(8191, 1, DataLoader.I_ID_limit),
                  OL_SUPPLY_W_ID = gen.generate_OL_SUPPLY_W_ID(w_id),
                  OL_QUANTITY = gen.random(1, 10)
                }).
                ToArray(),
          O_ENTRY_D = DateTime.UtcNow
        };
      }

      public struct ItemData
      {
        public int OL_I_ID;
        public int OL_SUPPLY_W_ID;
        public int OL_QUANTITY;
      }


      public int W_ID;
      public int D_ID;
      public int C_ID;
      public int OL_CNT;
      public ItemData[] items;
      public DateTime O_ENTRY_D;
    }

    public class OutputData
    {
      public class ItemData
      {
        public string I_NAME;
        public decimal I_PRICE;
      }

      public ulong O_ID;
      public string C_LAST;
      public string C_CREDIT;
      public decimal C_DISCOUNT;
      public decimal W_TAX;
      public decimal D_TAX;

      public ItemData[] items;
    }

    private class TransactionRollback : System.Exception
    {
    }

    public static async Task<OutputData> Execute(InputData input)
    {
      var ret = new OutputData()
      {
        items = new OutputData.ItemData[input.OL_CNT]
      };

      try
      {
        await DbWrap.RetriableTransact("NewOrder", () =>
        {
          Warehouse w = Db.SQL<Warehouse>("SELECT w FROM Warehouse w WHERE W_ID=?", input.W_ID).Single();

          ret.W_TAX = w.W_TAX;

          District d = Db.SQL<District>("SELECT d FROM District d WHERE D_W_ID=? AND D_ID=?", input.W_ID, input.D_ID).Single();

          ret.D_TAX = d.D_TAX;

          Customer c = Db.SQL<Customer>("SELECT c FROM Customer c WHERE C_W_ID=? AND C_D_ID=? AND C_ID=?", input.W_ID, input.D_ID, input.C_ID).Single();

          ret.C_DISCOUNT = c.C_DISCOUNT;
          ret.C_LAST = c.C_LAST;
          ret.C_CREDIT = c.C_CREDIT;

          var no = new NewOrder()
          {
            NO_W_ID = input.W_ID,
            NO_D_ID = input.D_ID,
          };

          ret.O_ID = no.GetObjectNo();
          no.NO_O_ID = ret.O_ID;

          new Order()
          {
            O_W_ID = input.W_ID,
            O_D_ID = input.D_ID,
            O_C_ID = input.C_ID,
            O_ID = ret.O_ID,
            O_OL_CNT = input.OL_CNT,
            O_ENTRY_D = DateTime.Now,
            O_ALL_LOCAL = input.items.All(i => i.OL_SUPPLY_W_ID == input.W_ID) ? 1 : 0
          };


          for (int ol_number = 1; ol_number <= input.OL_CNT; ++ol_number)
          {
            var item = input.items[ol_number - 1];
            var r = new OutputData.ItemData();
            ret.items[ol_number - 1] = r;

            Item i;
            try
            {
              i = Db.SQL<Item>("SELECT i FROM Item i WHERE I_ID=?", item.OL_I_ID).Single();
            }
            catch (System.InvalidOperationException)
            {
              throw new TransactionRollback();
            }

            r.I_PRICE = i.I_PRICE;
            r.I_NAME = i.I_NAME;
            var i_data = i.I_DATA;

            Stock s = Db.SQL<Stock>("SELECT s FROM Stock s WHERE S_W_ID=? AND S_I_ID=?", item.OL_SUPPLY_W_ID, item.OL_I_ID).Single();

            var s_quantity = s.S_QUANTITY;
            var s_dist_xx = new string[10] { s.S_DIST_01, s.S_DIST_02, s.S_DIST_03, s.S_DIST_04, s.S_DIST_05,
                                                         s.S_DIST_06, s.S_DIST_07, s.S_DIST_08, s.S_DIST_09, s.S_DIST_10 };
            var s_data = s.S_DATA;

            if (s_quantity >= item.OL_QUANTITY + 10)
              s.S_QUANTITY = s_quantity - item.OL_QUANTITY;
            else
              s.S_QUANTITY = s_quantity - item.OL_QUANTITY + 91;

            s.S_YTD += item.OL_QUANTITY;
            ++s.S_ORDER_CNT;
            if (item.OL_SUPPLY_W_ID != input.W_ID)
              ++s.S_REMOTE_CNT;

            new OrderLine()
            {
              OL_O_ID = ret.O_ID,
              OL_D_ID = input.D_ID,
              OL_W_ID = input.W_ID,
              OL_NUMBER = ol_number,
              OL_I_ID = item.OL_I_ID,
              OL_SUPPLY_W_ID = item.OL_SUPPLY_W_ID,
              OL_QUANTITY = item.OL_QUANTITY,
              OL_AMOUNT = r.I_PRICE * item.OL_QUANTITY,
              OL_DIST_INFO = s_dist_xx[input.D_ID - 1]
            };
          }
        });
      }
      catch (TransactionRollback)
      {
      }

      return ret;
    }
  }
}
