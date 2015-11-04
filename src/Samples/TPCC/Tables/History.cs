using System;
using Starcounter;
using Starcounter.Internal;

namespace tpcc
{

  [Database]
  public class History
  {
    public long H_C_ID;

    public long H_C_D_ID;

    public long H_C_W_ID;

    public long H_D_ID;

    public long H_W_ID;

    public DateTime H_DATE;

    public decimal H_AMOUNT;

    public string H_DATA;
  }
}
