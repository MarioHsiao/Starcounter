using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Starcounter.TransactionLog
{
    public class LogReadResult
    {
        public LogPosition continuation_position;
        public TransactionData transaction_data;
    }

    public interface ILogReader : IDisposable
    {
        Task<LogReadResult> ReadAsync(CancellationToken ct, bool wait_for_live_updates = true);
    }
}
