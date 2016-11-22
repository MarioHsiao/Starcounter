using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Starcounter;
using Starcounter.Internal;

namespace tpcc.Transactions
{
  public static class PaymentTransaction
  {
    public class InputData
    {
      public const int probability_of_choosing_customer_by_name = 60; //decrease to reduce collisions

      public static InputData Generate(TpccValuesGenerator gen, int w_id)
      {
        int d_id = gen.random(1, DataLoader.D_ID_limit);
        var c_ids = gen.generate_C_W_ID_and_C_D_ID(w_id, d_id);
        int y = gen.random(1, 100);

        return new InputData
        {
          W_ID = w_id,
          D_ID = d_id,
          C_W_ID = c_ids.Key,
          C_D_ID = c_ids.Value,
          C_LAST = (y <= probability_of_choosing_customer_by_name) ? gen.generate_C_LAST(gen.NURand(255, 0, 999)) : null,
          C_ID = (y > probability_of_choosing_customer_by_name) ? gen.NURand(1023, 1, DataLoader.C_ID_limit) : 0,
          H_AMOUNT = gen.random(1m, 5000m, 2),
          H_DATE = DateTime.Now
        };
      }

      public long W_ID;
      public long D_ID;
      public long C_W_ID;
      public long C_D_ID;
      public string C_LAST;
      public long C_ID;
      public decimal H_AMOUNT;
      public DateTime H_DATE;
    }

    public class OutputData
    {
      public string W_STREET_1;
      public string W_STREET_2;
      public string W_CITY;
      public string W_STATE;
      public string W_ZIP;
      public string D_STREET_1;
      public string D_STREET_2;
      public string D_CITY;
      public string D_STATE;
      public string D_ZIP;
      public string C_FIRST;
      public string C_MIDDLE;
      public string C_LAST;
      public string C_STREET_1;
      public string C_STREET_2;
      public string C_CITY;
      public string C_STATE;
      public string C_ZIP;
      public string C_PHONE;
      public DateTime C_SINCE;
      public string C_CREDIT;
      public decimal C_CREDIT_LIM;
      public decimal C_DISCOUNT;
      public decimal C_BALANCE;
      public string C_DATA;
    }

    public static async Task<OutputData> Execute(InputData input)
    {
      var ret = new OutputData();

      await DbWrap.RetriableTransact("Payment", () =>
      {
        Warehouse w = Db.SQL<Warehouse>("SELECT w FROM Warehouse w WHERE W_ID=?", input.W_ID).Single();

        var w_name = w.W_NAME;
        ret.W_STREET_1 = w.W_STREET_1;
        ret.W_STREET_2 = w.W_STREET_2;
        ret.W_CITY = w.W_CITY;
        ret.W_STATE = w.W_STATE;
        ret.W_ZIP = w.W_ZIP;

        District d = Db.SQL<District>("SELECT d FROM District d WHERE D_W_ID=? AND D_ID=?", input.W_ID, input.D_ID).Single();

        var d_name = d.D_NAME;
        ret.D_STREET_1 = d.D_STREET_1;
        ret.D_STREET_2 = d.D_STREET_2;
        ret.D_CITY = d.D_CITY;
        ret.D_STATE = d.D_STATE;
        ret.D_ZIP = d.D_ZIP;

        Customer c;
        var c_id = input.C_ID;
        if (string.IsNullOrEmpty(input.C_LAST)) //case 1 of 2.5.2.2
        {
          c = Db.SQL<Customer>("SELECT c FROM Customer c WHERE C_W_ID=? AND C_D_ID=? AND C_ID=?", input.C_W_ID, input.C_D_ID, c_id).Single();
          ret.C_LAST = c.C_LAST;
        }
        else //case 2 of 2.5.2.2
        {
          var customers_with_the_same_last_name = Db.SQL<Customer>("SELECT c FROM Customer c WHERE C_W_ID=? AND C_D_ID=? AND C_LAST=?",
                                                                   input.C_W_ID, input.C_D_ID, input.C_LAST)
                                                    .OrderBy(cust => cust.C_FIRST)
                                                    .ToArray();
          c = customers_with_the_same_last_name[(customers_with_the_same_last_name.Count() - 1) / 2];
          c_id = c.C_ID;
          ret.C_LAST = input.C_LAST;
        }

        ret.C_FIRST = c.C_FIRST;
        ret.C_MIDDLE = c.C_MIDDLE;
        ret.C_STREET_1 = c.C_STREET_1;
        ret.C_STREET_2 = c.C_STREET_2;
        ret.C_CITY = c.C_CITY;
        ret.C_STATE = c.C_STATE;
        ret.C_ZIP = c.C_ZIP;
        ret.C_PHONE = c.C_PHONE;
        ret.C_SINCE = c.C_SINCE;
        ret.C_CREDIT = c.C_CREDIT;
        ret.C_CREDIT_LIM = c.C_CREDIT_LIM;
        ret.C_DISCOUNT = c.C_DISCOUNT;
        c.C_BALANCE -= input.H_AMOUNT;
        ret.C_BALANCE = c.C_BALANCE;
        c.C_YTD_PAYMENT += input.H_AMOUNT;
        ++c.C_PAYMENT_CNT;

        if (ret.C_CREDIT == "BC")
        {
          var c_data = String.Format("{0} {1} {2} {3} {4} {5} {6}", c_id, input.C_D_ID, input.C_W_ID, input.D_ID, input.W_ID, input.H_AMOUNT, c.C_DATA);
          c.C_DATA = c_data.Length > 500 ? c_data.Substring(0, 500) : c_data;
          ret.C_DATA = c.C_DATA;
          ret.C_DATA = ret.C_DATA.Length > 200 ? ret.C_DATA.Substring(0, 200) : ret.C_DATA;
        }

        new History()
        {
          H_C_ID = c_id,
          H_C_D_ID = input.C_D_ID,
          H_C_W_ID = input.C_W_ID,
          H_D_ID = input.D_ID,
          H_W_ID = input.W_ID,
          H_DATA = w_name + "    " + d_name,
          H_AMOUNT = input.H_AMOUNT,
          H_DATE = input.H_DATE
        };
      });

      return ret;
    }
  }
}
