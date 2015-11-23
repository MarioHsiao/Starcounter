using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.TransactionLog
{
    public interface IInputLogStream : IDisposable
    {
        Task<int> ReadAsync(byte[] buffer, int offset, int count);
    }
}
