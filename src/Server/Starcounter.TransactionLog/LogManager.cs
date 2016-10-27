using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.TransactionLog
{
    public class LogManager : ILogManager
    {
        public ILogReader OpenLog(string dbName, string logDir, Func<string, bool> tablePredicate = null)
        {
            return new LogReader(dbName, logDir, tablePredicate);
        }

        public ILogReader OpenLog(string dbName, string logDir, LogPosition position, Func<string, bool> tablePredicate = null)
        {
            return new LogReader(dbName, logDir, position, tablePredicate);
        }
    }
}
