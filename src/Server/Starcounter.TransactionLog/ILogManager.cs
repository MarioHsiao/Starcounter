using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.TransactionLog
{
    public interface ILogManager
    {
        ILogReader OpenLog(string dbName, string logDir, Func<string, bool> tablePredicate=null);

        ILogReader OpenLog(string dbName, string logDir, LogPosition position, Func<string, bool> tablePredicate = null);
    }
}
