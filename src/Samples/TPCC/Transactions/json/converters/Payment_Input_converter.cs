using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tpcc.Transactions.json.converters
{
  static class Payment_Input_converter
  {
    public static PaymentTransaction.InputData ToCLR(this Payment_Input d)
    {
      return new PaymentTransaction.InputData()
      {
        C_D_ID = d.C_D_ID,
        C_ID = d.C_ID,
        C_LAST = d.C_LAST,
        C_W_ID = d.C_W_ID,
        D_ID = d.D_ID,
        H_AMOUNT = d.H_AMOUNT,
        H_DATE = DateTime.Parse(d.H_DATE),
        W_ID = d.W_ID
      };
    }
  }
}
