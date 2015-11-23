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

    interface ILogReader
    {
        IInputLogStream OpenLog(string path);

        IInputLogStream OpenLog(string path, LogPosition position, LogPositionOptions position_options);
    }
}
