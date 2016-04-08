using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.OptimizedLog
{
    public class OptimizedLogManager : IOptimizedLogManager
    {
        public IOptimizedLogReader OpenLog(string db_name, string log_dir)
        {
            return new OptimizedLogReader(db_name, log_dir);
        }

        public IOptimizedLogReader OpenLog(string db_name, string log_dir, OptimizedLogPosition position)
        {
            return new OptimizedLogReader(db_name, log_dir, position);
        }
    }
}
