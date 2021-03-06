﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.OptimizedLog
{
    public interface IOptimizedLogManager
    {
        IOptimizedLogReader OpenLog(string db_name, string log_dir, Func<string, bool> table_predicate = null);

        IOptimizedLogReader OpenLog(string db_name, string log_dir, OptimizedLogPosition position, Func<string, bool> table_predicate = null);
    }
}
