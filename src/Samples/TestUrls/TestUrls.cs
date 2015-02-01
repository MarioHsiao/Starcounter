//#define FASTEST_POSSIBLE
#define FILL_RANDOMLY
#if FASTEST_POSSIBLE
#undef FILL_RANDOMLY
#endif

using Starcounter;
using Starcounter.Advanced;
using Starcounter.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Starcounter.TestFramework;
using System.IO;

namespace NodeTest
{
    class NodeTest
    {
        static Int32 Main(string[] args)
        {
            //Debugger.Launch();

            try {

                return 0;

            } catch (Exception exc) {
                Console.Error.WriteLine(exc.ToString());
                Environment.Exit(1);
                return 1;
            }
        }
    }
}
