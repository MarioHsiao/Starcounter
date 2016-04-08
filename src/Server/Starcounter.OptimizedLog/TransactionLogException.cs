using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.OptimizedLog
{
    public class OptimizedLogException : System.Exception
    {
        public OptimizedLogException(int code)
        {
            Code = code;
        }

        public int Code { get; private set; }

        public static void Test(int code)
        {
            if (code != 0)
                throw new OptimizedLogException(code);
        }
    }
}
