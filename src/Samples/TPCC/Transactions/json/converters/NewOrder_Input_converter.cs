using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tpcc.Transactions.json.converters
{
  static class NewOrder_Input_converter
  {
    public static NewOrderTransaction.InputData ToCLR(this NewOrder_Input d)
    {
      return new NewOrderTransaction.InputData()
      {
        C_ID = (int)d.C_ID,
        D_ID = (int)d.D_ID,
        items = d.items.Select(i => new NewOrderTransaction.InputData.ItemData { OL_I_ID = (int)i.OL_I_ID, OL_QUANTITY = (int)i.OL_QUANTITY, OL_SUPPLY_W_ID = (int)i.OL_SUPPLY_W_ID }).ToArray(),
        OL_CNT = (int)d.OL_CNT,
        O_ENTRY_D = DateTime.Parse(d.O_ENTRY_D),
        W_ID = (int)d.W_ID
      };
    }
  }
}
