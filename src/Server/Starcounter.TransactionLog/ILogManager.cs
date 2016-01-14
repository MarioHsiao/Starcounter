using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.TransactionLog
{
    public interface ILogManager
    {
        ILogReader OpenLog(string db_name, string log_dir);

        ILogReader OpenLog(string db_name, string log_dir, LogPosition position);
    }
}
