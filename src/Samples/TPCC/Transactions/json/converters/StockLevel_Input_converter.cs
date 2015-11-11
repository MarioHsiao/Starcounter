using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tpcc.Transactions.json.converters
{
  static class StockLevel_Input_converter
  {
    public static StockLevelTransaction.InputData ToCLR(this StockLevel_Input d)
    {
      return new StockLevelTransaction.InputData()
      {
        D_ID = (int)d.D_ID,
        threshold = (int)d.threshold,
        W_ID = (int)d.W_ID
      };
    }
  }
}
