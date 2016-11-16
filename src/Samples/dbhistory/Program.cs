using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter.TransactionLog;
using System.Threading;
using System.Runtime.Serialization.Json;

namespace dbhistory
{
    class Program
    {
        static void Main(string[] args)
        {
            if ( args.Length != 1 )
            {
                System.Console.Error.WriteLine("Usage: dbhistory <db.cfg>");
                return;
            }

            string full_cfg_path = System.IO.Path.GetFullPath(args[0]);

            string db_name = System.IO.Path.GetFileNameWithoutExtension(full_cfg_path);
            string log_dir = System.IO.Path.GetDirectoryName(full_cfg_path);


            var jser = new DataContractJsonSerializer(typeof(LogReadResult), new Type[] { typeof(TransactionData), typeof(Reference), typeof(LogPosition) });
            var lm = new LogManager();
            using (var log_reader = lm.OpenLog(db_name, log_dir))
            {
                var cts = new CancellationTokenSource();

                //rewind to the end of log
                LogReadResult lr;
                do
                {
                    lr = log_reader.ReadAsync(cts.Token, false).Result;

                    if (lr != null)
                    {
                        jser.WriteObject(System.Console.OpenStandardOutput(), lr);
                        System.Console.WriteLine();
                    }
                }
                while (lr != null);
            }
        }
    }
}
