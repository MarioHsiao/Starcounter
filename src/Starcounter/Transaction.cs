
using System;

namespace Starcounter
{
    
    public static class Transaction
    {

        public static void OnTransactionSwitch()
        {
            ThreadData.Current.Scheduler.SqlEnumCache.InvalidateCache();
        }
    }
}
