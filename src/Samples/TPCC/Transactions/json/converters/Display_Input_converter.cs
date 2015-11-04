using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tpcc.Transactions.json.converters
{
  static class Display_Input_converter
  {
    public static DeliveryTransaction.InputData ToCLR(this Delivery_Input d)
    {
      return new DeliveryTransaction.InputData()
      {
        D_ID = (int)d.D_ID,
        O_CARRIER_ID = (int)d.O_CARRIER_ID,
        W_ID = (int)d.W_ID
      };
    }
  }
}
