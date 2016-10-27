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
        public LogPosition ContinuationPosition;
        public TransactionData TransactionData;
    }

    public interface ILogReader : IDisposable
    {
        Task<LogReadResult> ReadAsync(CancellationToken ct, bool waitForLiveUpdates = true);
    }
}
