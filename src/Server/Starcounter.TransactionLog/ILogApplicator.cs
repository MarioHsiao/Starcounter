using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.TransactionLog
{
    interface ILogApplicator
    {
        //Returns what position is in progress now. So it's the posistion to pass to ILogReader.OpenLog in case of reconnect
        //unapplyied_count is a hint, how much data may be kept in order to optimize possible log retransmission in case of reconnect
        LogPosition Apply(byte[] buffer, int offset, int count, out int unapplyied_count );
    }
}
