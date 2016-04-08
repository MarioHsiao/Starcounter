using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Starcounter.OptimizedLog
{
    public class OptimizedLogReadResult
    {
        public OptimizedLogPosition continuation_position;
        public Starcounter.TransactionLog.create_record_entry record;
    }

    public interface IOptimizedLogReader : IDisposable
    {
        /// <returns>Returns null on enf of log.</returns>
        IEnumerable<OptimizedLogReadResult> Records { get; }
        Starcounter.TransactionLog.LogPosition TransactionLogContinuationPosition { get; }
    }
}
