using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.TransactionLog
{
    public class LogManager : ILogManager
    {
        public ILogReader OpenLog(string db_name, string log_dir, Func<string, bool> table_predicate = null)
        {
            return new LogReader(db_name, log_dir, table_predicate);
        }

        public ILogReader OpenLog(string db_name, string log_dir, LogPosition position, Func<string, bool> table_predicate = null)
        {
            return new LogReader(db_name, log_dir, position, table_predicate);
        }
    }
}
