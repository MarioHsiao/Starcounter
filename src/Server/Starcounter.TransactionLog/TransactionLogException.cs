using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.TransactionLog
{
    public class TransactionLogException : System.Exception
    {
        internal TransactionLogException(int code)
        {
            Code = code;
        }

        public int Code { get; private set; }

        internal static void Test(int code)
        {
            if (code != 0)
                throw new TransactionLogException(code);
        }
    }
}
