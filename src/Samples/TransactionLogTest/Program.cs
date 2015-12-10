using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Starcounter;
using System.Diagnostics;
using Starcounter.TransactionLog;


namespace TransactionLogTest
{
    [Database]
    public class TestClass
    {
        public int int_field;
    };


    class Program
    {
        static void Main(string[] args)
        {
            Debugger.Launch();

            // ARRANGE
            ILogManager log_manager = new LogManager();

            using (ILogReader log_reader = log_manager.OpenLog(Starcounter.Db.Environment.DatabaseName, Starcounter.Db.Environment.DatabaseLogDir))
            {
                var cts = new CancellationTokenSource();

                //rewind to the end of log
                LogReadResult lr;
                do
                {
                    lr = log_reader.ReadAsync(cts.Token, false).Result;
                }
                while (lr != null);

                Db.Transact(() => new TestClass { int_field = 1 });

                // ACT
                lr = log_reader.ReadAsync(cts.Token).Result;

                //CHECK
                Trace.Assert(lr.transaction_data.creates.Count() == 1);
            }

            Environment.Exit(0);
        }
    }
}
