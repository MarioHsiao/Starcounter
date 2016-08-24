using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.OptimizedLog
{
    public class OptimizedLogManager : IOptimizedLogManager
    {
        public IOptimizedLogReader OpenLog(string db_name, string log_dir, Func<string, bool> table_predicate = null)
        {
            return new OptimizedLogReader(db_name, log_dir, table_predicate);
        }

        public IOptimizedLogReader OpenLog(string db_name, string log_dir, OptimizedLogPosition position, Func<string, bool> table_predicate = null)
        {
            return new OptimizedLogReader(db_name, log_dir, position, table_predicate);
        }
    }
}
