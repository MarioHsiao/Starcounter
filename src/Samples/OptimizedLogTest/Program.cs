using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Starcounter;
using System.Diagnostics;
using Starcounter.TransactionLog;
using Starcounter.OptimizedLog;


namespace TransactionLogTest
{
    [Database]
    public class TestClassBase
    {
        public string base_string;
    }

    [Database]
    public class TestClass : TestClassBase
    {
        public string str_field;
        public Binary bin_field;
        public long long_field;
        public ulong ulong_field;
        public decimal dec_field;
        public float float_field;
        public double double_field;
        public TestClass ref_field;
        public string null_str_field;
        public long? null_long_field;
    };


    class Program
    {
        static ulong create_entry_in_transaction_log()
        {
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

                ulong t_record_key = 0;

                Db.Transact(() =>
                {
                    var t = new TestClass
                    {
                        base_string = "Str0",
                        bin_field = new Binary(new byte[1] { 42 }),
                        dec_field = 42.24m,
                        double_field = -42.42,
                        float_field = 42.42f,
                        long_field = -42,
                        str_field = "Str",
                        ulong_field = ulong.MaxValue,
                        null_str_field = null
                    };
                    t.ref_field = t;
                    t_record_key = t.GetObjectNo();
                });

                log_reader.ReadAsync(cts.Token).Wait();

                return t_record_key;
            }
        }

        static string find_latest_optimized_log()
        {
            return System.IO.Directory.EnumerateFiles(Starcounter.Db.Environment.DatabaseLogDir, string.Format("{0}.????????????.optlog", Starcounter.Db.Environment.DatabaseName))
                                      .Select(path => System.IO.Path.GetFileName(path))
                                      .Max();
        }

        static void run_logopt()
        {
            Process p = Process.GetCurrentProcess();
            string bin_path = System.IO.Path.GetDirectoryName(p.MainModule.FileName);

            string logopt_path = System.IO.Path.Combine(bin_path, "logopt.exe");
            string logopt_args = string.Format("-make \"{0}\" \"{1}\" \"{0}\" \"{1}\"", Starcounter.Db.Environment.DatabaseName, Starcounter.Db.Environment.DatabaseLogDir);

            Process.Start(logopt_path, logopt_args).WaitForExit();
        }

        static void setup()
        {
            oid_in_optimized_log = create_entry_in_transaction_log();

            run_logopt();

            string latest_log = find_latest_optimized_log();

            oid_in_transaction_log = create_entry_in_transaction_log();

            string latest_log2 = find_latest_optimized_log();

            //in case latest entry caused new optimized log to be produced - remove it since test is about to test first logopt
            if (latest_log != latest_log2)
                System.IO.File.Delete(System.IO.Path.Combine(Starcounter.Db.Environment.DatabaseLogDir, latest_log2));

        }

        static ulong oid_in_optimized_log;
        static ulong oid_in_transaction_log;

        static void check_record_in_optimized_log()
        {
            // ARRANGE
            IOptimizedLogManager optlog_manager = new OptimizedLogManager();

            using (IOptimizedLogReader optlog_reader = optlog_manager.OpenLog(Starcounter.Db.Environment.DatabaseName, Starcounter.Db.Environment.DatabaseLogDir))
            {
                // ACT. find the record

                CreateRecordEntry create_entry = optlog_reader.Records.Where(r => r.record.Key.ObjectID == oid_in_optimized_log).Single().record;

                //CHECK
                Trace.Assert(create_entry.Table == typeof(TestClass).FullName);

                Trace.Assert((string)(create_entry.Columns.Where(c => c.Name == "base_string").Single().Value) == "Str0");
                Trace.Assert((create_entry.Columns.Where(c => c.Name == "bin_field").Single().Value as byte[]).SequenceEqual(new byte[1] { 42 }));
                Trace.Assert((decimal)(create_entry.Columns.Where(c => c.Name == "dec_field").Single().Value) == 42.24m);
                Trace.Assert((double)(create_entry.Columns.Where(c => c.Name == "double_field").Single().Value) == -42.42);
                Trace.Assert((float)(create_entry.Columns.Where(c => c.Name == "float_field").Single().Value) == 42.42f);
                Trace.Assert((long)(create_entry.Columns.Where(c => c.Name == "long_field").Single().Value) == -42);
                Trace.Assert((string)(create_entry.Columns.Where(c => c.Name == "str_field").Single().Value) == "Str");
                Trace.Assert((ulong)(create_entry.Columns.Where(c => c.Name == "ulong_field").Single().Value) == ulong.MaxValue);
                Trace.Assert(((Reference)(create_entry.Columns.Where(c => c.Name == "ref_field").Single().Value)).ObjectID == oid_in_optimized_log);
            }
        }

        static void check_continuity()
        {
            // ARRANGE
            IOptimizedLogManager optlog_manager = new OptimizedLogManager();

            using (IOptimizedLogReader optlog_reader = optlog_manager.OpenLog(Starcounter.Db.Environment.DatabaseName, Starcounter.Db.Environment.DatabaseLogDir))
            {
                // ACT
                var continuation_position = optlog_reader.TransactionLogContinuationPosition;

                using (ILogReader log_reader = new LogManager().OpenLog(Starcounter.Db.Environment.DatabaseName, Starcounter.Db.Environment.DatabaseLogDir, continuation_position))
                {

                    CreateRecordEntry create_entry = log_reader.ReadAsync(new CancellationTokenSource().Token).Result.TransactionData.Creates.Single();

                    //CHECK
                    Trace.Assert(create_entry.Table == typeof(TestClass).FullName);
                    Trace.Assert(create_entry.Key.ObjectID == oid_in_transaction_log);

                    Trace.Assert((string)(create_entry.Columns.Where(c => c.Name == "base_string").Single().Value) == "Str0");
                    Trace.Assert((create_entry.Columns.Where(c => c.Name == "bin_field").Single().Value as byte[]).SequenceEqual(new byte[1] { 42 }));
                    Trace.Assert((decimal)(create_entry.Columns.Where(c => c.Name == "dec_field").Single().Value) == 42.24m);
                    Trace.Assert((double)(create_entry.Columns.Where(c => c.Name == "double_field").Single().Value) == -42.42);
                    Trace.Assert((float)(create_entry.Columns.Where(c => c.Name == "float_field").Single().Value) == 42.42f);
                    Trace.Assert((long)(create_entry.Columns.Where(c => c.Name == "long_field").Single().Value) == -42);
                    Trace.Assert((string)(create_entry.Columns.Where(c => c.Name == "str_field").Single().Value) == "Str");
                    Trace.Assert((ulong)(create_entry.Columns.Where(c => c.Name == "ulong_field").Single().Value) == ulong.MaxValue);
                    Trace.Assert(((Reference)(create_entry.Columns.Where(c => c.Name == "ref_field").Single().Value)).ObjectID == oid_in_transaction_log);
                }
            }
        }

        static void check_filtering()
        {
            //ACT
            IOptimizedLogManager optlog_manager = new OptimizedLogManager();

            using (IOptimizedLogReader optlog_reader = optlog_manager.OpenLog(Starcounter.Db.Environment.DatabaseName, Starcounter.Db.Environment.DatabaseLogDir, t=>t== typeof(TestClass).FullName))
            {
                using (IOptimizedLogReader optlog_reader2 = optlog_manager.OpenLog(Starcounter.Db.Environment.DatabaseName, Starcounter.Db.Environment.DatabaseLogDir, t=>false))
                {
                    //CHECK
                    Trace.Assert(optlog_reader.Records.Where(r => r.record.Key.ObjectID == oid_in_optimized_log).Count() == 1);
                    Trace.Assert(optlog_reader2.Records.Where(r => r.record.Key.ObjectID == oid_in_optimized_log).Count() == 0);
                }
            }
        }



        static void Main(string[] args)
        {
            setup();

            check_record_in_optimized_log();
            check_continuity();
            check_filtering();

            Environment.Exit(0);
        }
    }
}

