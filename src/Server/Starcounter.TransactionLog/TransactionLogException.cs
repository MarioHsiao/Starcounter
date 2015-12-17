using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.TransactionLog
{
    public class TransactionLogException : System.Exception
    {
        public TransactionLogException(int code)
        {
            Code = code;
        }

        public int Code { get; private set; }

        public static void Test(int code)
        {
            if (code != 0)
                throw new TransactionLogException(code);
        }
    }
}
