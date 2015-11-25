using System;
using Starcounter;
using Starcounter.Internal;

namespace tpcc
{
  [Database]
  public class Customer
  {
    public static void CreateIndex()
    {
      DbWrap.CreateIndex("Customer_primary_key", "CREATE UNIQUE INDEX Customer_primary_key ON Customer (C_W_ID, C_D_ID, C_ID)");
      DbWrap.CreateIndex("Customer_index_for_payment_transaction", "CREATE INDEX Customer_index_for_payment_transaction ON Customer (C_W_ID, C_D_ID, C_LAST)");
    }

    public long C_ID;

    public long C_D_ID;

    public long C_W_ID;

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

    public decimal C_YTD_PAYMENT;

    public long C_PAYMENT_CNT;

    public long C_DELIVERY_CNT;

    public string C_DATA;
  }
}
