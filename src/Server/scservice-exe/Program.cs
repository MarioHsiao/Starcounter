
using System;
using System.Runtime.InteropServices;

namespace scservice {

    class Program {
        [DllImport("scservice.dll", CallingConvention = CallingConvention.StdCall)]
        static extern unsafe int wmain(int argumentCount, string[] args, string[] ignored);

        static void Main(string[] args) {
            Console.WriteLine("Booh!");
            Environment.ExitCode = wmain(args.Length, args, null);
        }
    }
}