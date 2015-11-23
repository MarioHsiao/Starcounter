using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.TransactionLog
{
    public enum LogPositionOptions
    {
        ReadFromPosition,
        ReadAfterPosition
    }

    public class TransactionAndPosition
    {
        public ITransaction transaction;
        public LogPosition position;
    }

    public interface ILogReader
    {
        IObservable<TransactionAndPosition> OpenLog(string path);

        IObservable<TransactionAndPosition> OpenLog(string path, LogPosition position, LogPositionOptions position_options);
    }
}
