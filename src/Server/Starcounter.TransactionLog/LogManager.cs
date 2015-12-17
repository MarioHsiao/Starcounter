using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.TransactionLog
{
    public class LogManager : ILogManager
    {
        public ILogReader OpenLog(string db_name, string log_dir)
        {
            return new LogReader(db_name, log_dir);
        }

        public ILogReader OpenLog(string db_name, string log_dir, LogPosition position)
        {
            return new LogReader(db_name, log_dir, position);
        }
    }
}
