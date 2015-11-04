using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tpcc.Transactions.json.converters
{
  static class OrderStatus_Input_converter
  {
    public static OrderStatusTransaction.InputData ToCLR(this OrderStatus_Input d)
    {
      return new OrderStatusTransaction.InputData()
      {
        C_ID = d.C_ID,
        C_LAST = d.C_LAST,
        D_ID = d.D_ID,
        W_ID = d.W_ID
      };
    }
  }
}
