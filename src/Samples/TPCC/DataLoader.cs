using System;
using Starcounter;
using Starcounter.Internal;
using System.Collections.Generic;
using System.Linq;

namespace tpcc
{
  public static class DataLoader
  {
    //constants according to TPCC 4.3.3.1
    public const int I_ID_limit = 1000000;    //items count. increase to reduce collisions of NewOrder transactions
    public const int I_IM_ID_limit = 10000;    //items images count
    public const int W_ID_limit = 2;           //warehouses count. increase to reduce collisions of NewOrder transactions
    public const int D_ID_limit = 10;          //discticts count
    public const int C_ID_limit = 3000;        //customers count

    public static void PopulateAllTables(TpccValuesGenerator gen)
    {
      PopulateItems(gen);
      PopulateWarehouses(gen);
      PopulateStocks(gen);
      PopulateDisctricts(gen);
      PopulateCustomers(gen);
      PopulateHistory(gen);
      PopulateOrdersAndOrderLines(gen);
      PopulateNewOrders(gen);

      //create write transaction to ensure all logs are on disk
      Starcounter.Db.Transact(() =>
      {
          var item = Db.SQL<Item>("select i from item i").First();
          var p = item.I_PRICE;
          item.I_PRICE = p;
      });
    }

        private static void LoadAll<T>(this IEnumerable<T> c, Action<T> a)
    {
      //c.AsParallel().ForAll(a);
      c.ToList().ForEach(a);
    }

    private static void PopulateItems(TpccValuesGenerator gen)
    {
      (from i_id in Enumerable.Range(1, I_ID_limit)
       select new { i_id = i_id }).LoadAll(
            x =>
            {
              Starcounter.Db.TransactAsync(() =>
              {
                new Item()
                {
                  I_ID = x.i_id,
                  I_IM_ID = gen.random(1, I_IM_ID_limit),
                  I_NAME = gen.a_string(14, 24),
                  I_PRICE = gen.random(1m, 100m, 2),
                  I_DATA = gen.generate_I_DATA_or_S_DATA()
                };
              });
            }
       );
    }

    private static void PopulateWarehouses(TpccValuesGenerator gen)
    {
      Starcounter.Db.TransactAsync(() =>
      {
        for (int i = 1; i <= W_ID_limit; ++i)
        {
          new Warehouse()
          {
            W_ID = i,
            W_NAME = gen.a_string(6, 10),
            W_STREET_1 = gen.a_string(10, 20),
            W_STREET_2 = gen.a_string(10, 20),
            W_CITY = gen.a_string(10, 20),
            W_STATE = gen.a_string(2, 2),
            W_ZIP = gen.generate_zip(),
            W_TAX = gen.random(0m, 0.2m, 4)
          };
        }
      });
    }

    private static void PopulateStocks(TpccValuesGenerator gen)
    {
      (from w_id in Enumerable.Range(1, W_ID_limit)
       from i_id in Enumerable.Range(1, I_ID_limit)
       select new { w_id = w_id, i_id = i_id }).LoadAll(
            x =>
            {
              Starcounter.Db.TransactAsync(() =>
              {
                new Stock()
                {
                  S_I_ID = x.i_id,
                  S_W_ID = x.w_id,
                  S_QUANTITY = gen.random(10, 100),
                  S_DIST_01 = gen.a_string(24, 24),
                  S_DIST_02 = gen.a_string(24, 24),
                  S_DIST_03 = gen.a_string(24, 24),
                  S_DIST_04 = gen.a_string(24, 24),
                  S_DIST_05 = gen.a_string(24, 24),
                  S_DIST_06 = gen.a_string(24, 24),
                  S_DIST_07 = gen.a_string(24, 24),
                  S_DIST_08 = gen.a_string(24, 24),
                  S_DIST_09 = gen.a_string(24, 24),
                  S_DIST_10 = gen.a_string(24, 24),
                  S_YTD = 0,
                  S_ORDER_CNT = 0,
                  S_REMOTE_CNT = 0,
                  S_DATA = gen.generate_I_DATA_or_S_DATA()
                };
              });
            }
          );
    }

    private static void PopulateDisctricts(TpccValuesGenerator gen)
    {
      Starcounter.Db.TransactAsync(() =>
      {
        for (int w_id = 1; w_id <= W_ID_limit; ++w_id)
        {
          for (int d_id = 1; d_id <= D_ID_limit; ++d_id)
          {
            new District()
            {
              D_ID = d_id,
              D_W_ID = w_id,
              D_NAME = gen.a_string(6, 10),
              D_STREET_1 = gen.a_string(10, 20),
              D_STREET_2 = gen.a_string(10, 20),
              D_CITY = gen.a_string(10, 20),
              D_STATE = gen.a_string(2, 2),
              D_ZIP = gen.generate_zip(),
              D_TAX = gen.random(0m, 0.2m, 4)
            };
          }
        }
      });
    }

    private static void PopulateCustomers(TpccValuesGenerator gen)
    {
      (from w_id in Enumerable.Range(1, W_ID_limit)
       from d_id in Enumerable.Range(1, D_ID_limit)
       from c_id in Enumerable.Range(1, C_ID_limit)
       select new { w_id = w_id, d_id = d_id, c_id = c_id }).LoadAll(
            x =>
            {
              Starcounter.Db.TransactAsync(() =>
              {
                new Customer()
                {
                  C_ID = x.c_id,
                  C_D_ID = x.d_id,
                  C_W_ID = x.w_id,
                  C_LAST = gen.generate_C_LAST((x.c_id <= 1000) ? x.c_id - 1 : gen.NURand(255, 0, 999)),
                  C_MIDDLE = "OE",
                  C_FIRST = gen.a_string(8, 16),
                  C_STREET_1 = gen.a_string(10, 20),
                  C_STREET_2 = gen.a_string(10, 20),
                  C_CITY = gen.a_string(10, 20),
                  C_STATE = gen.a_string(2, 2),
                  C_ZIP = gen.generate_zip(),
                  C_PHONE = gen.n_string(16, 16),
                  C_SINCE = DateTime.UtcNow,
                  C_CREDIT = gen.true_with_probability(10) ? "BC" : "GC",
                  C_CREDIT_LIM = 50000m,
                  C_DISCOUNT = gen.random(0m, 0.5m, 4),
                  C_BALANCE = -10m,
                  C_YTD_PAYMENT = 10m,
                  C_PAYMENT_CNT = 1,
                  C_DELIVERY_CNT = 0,
                  C_DATA = gen.a_string(300, 500)
                };
              });
            }
          );
    }

    private static void PopulateHistory(TpccValuesGenerator gen)
    {
      (from w_id in Enumerable.Range(1, W_ID_limit)
       from d_id in Enumerable.Range(1, D_ID_limit)
       from c_id in Enumerable.Range(1, C_ID_limit)
       select new { w_id = w_id, d_id = d_id, c_id = c_id }).LoadAll(
            x =>
            {
              Starcounter.Db.TransactAsync(() =>
              {
                new History()
                {
                  H_C_ID = x.c_id,
                  H_C_D_ID = x.d_id,
                  H_D_ID = x.d_id,
                  H_C_W_ID = x.w_id,
                  H_W_ID = x.w_id,
                  H_DATE = DateTime.UtcNow,
                  H_AMOUNT = 10m,
                  H_DATA = gen.a_string(12, 24)
                };
              });
            }
          );
    }

    private static void PopulateOrdersAndOrderLines(TpccValuesGenerator gen)
    {
      (from w_id in Enumerable.Range(1, W_ID_limit)
       from d_id in Enumerable.Range(1, D_ID_limit)
       select new { w_id = w_id, d_id = d_id }).LoadAll(
            y =>
            {
              Starcounter.Db.TransactAsync(() =>
              {
                foreach (var x in gen.permutate(1, C_ID_limit).Select((c_id, id) => new { c_id = c_id, o_id = (ulong)id + 1 }))
                {
                  int o_ol_cnt = gen.random(5, 15);
                  new Order()
                  {
                    O_ID = x.o_id,
                    O_C_ID = x.c_id,
                    O_D_ID = y.d_id,
                    O_W_ID = y.w_id,
                    O_ENTRY_D = DateTime.UtcNow,
                    O_CARRIER_ID = x.o_id < 2101 ? gen.random(1, 10) : 0,
                    O_OL_CNT = o_ol_cnt,
                    O_ALL_LOCAL = 1
                  };

                  for (int ol_number = 1; ol_number < o_ol_cnt; ++ol_number)
                  {
                    new OrderLine()
                    {
                      OL_O_ID = x.o_id,
                      OL_D_ID = y.d_id,
                      OL_W_ID = y.w_id,
                      OL_NUMBER = ol_number,
                      OL_I_ID = gen.random(1, 100000),
                      OL_SUPPLY_W_ID = y.w_id,
                      OL_DELIVERY_D = x.o_id < 2101 ? DateTime.UtcNow : DateTime.MinValue,
                      OL_QUANTITY = 5,
                      OL_AMOUNT = x.o_id < 2101 ? 0m : gen.random(0.01m, 9999.99m, 2),
                      OL_DIST_INFO = gen.a_string(24, 24)
                    };
                  }
                }
              });
            }
          );

    }

    private static void PopulateNewOrders(TpccValuesGenerator gen)
    {
      (from w_id in Enumerable.Range(1, W_ID_limit)
       from d_id in Enumerable.Range(1, D_ID_limit)
       select new { w_id = w_id, d_id = d_id }).LoadAll(
            x =>
            {
              Starcounter.Db.TransactAsync(() =>
              {
                for (ulong o_id = 2101; o_id <= C_ID_limit; ++o_id)
                {
                  new NewOrder()
                  {
                    NO_O_ID = o_id,
                    NO_D_ID = x.d_id,
                    NO_W_ID = x.w_id
                  };
                }
              });
            }
          );
    }

  }
}
