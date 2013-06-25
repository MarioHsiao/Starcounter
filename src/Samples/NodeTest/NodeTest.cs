#define FILL_RANDOMLY

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

namespace NodeTest
{
    class Settings
    {
        public enum AsyncModes
        {
            ModeSync,
            ModeAsync,
            ModeRandom
        }

        public Int32 NumWorkers = 2;

        public Int32 MinEchoBytes = 1;

        public Int32 MaxEchoBytes = 1000000;

        public Int32 NumEchoesPerWorker = 10000;

        public Int32 NumSecondsToWait = 150;

        public AsyncModes AsyncMode = AsyncModes.ModeRandom;

        public Boolean ConsoleDiag = false;

        public void Init(string[] args)
        {
            foreach (String arg in args)
            {
                if (arg.StartsWith("-NumThreads="))
                {
                    NumWorkers = Int32.Parse(arg.Substring(12));
                }
                else if (arg.StartsWith("-MinEchoBytes="))
                {
                    MinEchoBytes = Int32.Parse(arg.Substring(14));
                }
                else if (arg.StartsWith("-MaxEchoBytes="))
                {
                    MaxEchoBytes = Int32.Parse(arg.Substring(14));
                }
                else if (arg.StartsWith("-NumEchoesPerWorker="))
                {
                    NumEchoesPerWorker = Int32.Parse(arg.Substring(20));
                }
                else if (arg.StartsWith("-NumSecondsToWait="))
                {
                    NumSecondsToWait = Int32.Parse(arg.Substring(18));
                }
                else if (arg.StartsWith("-AsyncMode="))
                {
                    String asyncParam = arg.Substring(11);

                    if (asyncParam == "ModeSync")
                        AsyncMode = AsyncModes.ModeSync;
                    else if (asyncParam == "ModeAsync")
                        AsyncMode = AsyncModes.ModeAsync;
                    else if (asyncParam == "ModeRandom")
                        AsyncMode = AsyncModes.ModeRandom;
                }
                else if (arg.StartsWith("-ConsoleDiag="))
                {
                    ConsoleDiag = Boolean.Parse(arg.Substring(13));
                }
            }
        }
    }

    class NodeTestInstance
    {
        UInt64 unique_id_;

        Int32 num_echo_bytes_;

        Boolean async_;

        Byte[] correct_hash_;

        Random random_ = new Random();

        Byte[] body_bytes_;

        Settings settings_;

        Worker worker_;

        // Initializes new test instance.
        public void Init(
            Settings settings,
            Worker worker,
            UInt64 unique_id,
            Boolean async,
            Int32 num_echo_bytes)
        {
            settings_ = settings;
            worker_ = worker;
            unique_id_ = unique_id;
            async_ = async;

            num_echo_bytes_ = num_echo_bytes;
            body_bytes_ = new Byte[num_echo_bytes_];

#if FILL_RANDOMLY

            // Generating random bytes.
            random_.NextBytes(body_bytes_);

#else

            // Filling bytes continuously between 0 and 9
            Byte k = 0;
            for (Int32 i = 0; i < num_echo_bytes_; i++)
            {
                body_bytes_[i] = (Byte)('0' + k);
                k++;
                if (k >= 10) k = 0;
            }

#endif

            // Calculating SHA1 hash.
            SHA1 sha1 = new SHA1CryptoServiceProvider();
            correct_hash_ = sha1.ComputeHash(body_bytes_);
        }

        // Sends data, gets the response, and checks its correctness.
        public Boolean PerformTest(Node node)
        {
            if (!async_)
            {
                Response resp = node.POST("/nodetest", body_bytes_, null, null);
                return CheckResponse(resp);
            }
            else
            {
                node.POST("/nodetest", body_bytes_, null, null, (Response resp) =>
                {
                    CheckResponse(resp);
                    return null;
                });

                return true;
            }
        }

        // Checks response correctness.
        Boolean CheckResponse(Response resp)
        {
            Byte[] resp_body = resp.BodyBytes;
            if (resp_body.Length != num_echo_bytes_)
            {
                NodeTest.WorkersMonitor.IndicateTestFailed();
                return false;
            }

            if (settings_.ConsoleDiag)
                Console.WriteLine(worker_.Id + ": echoed: " + num_echo_bytes_ + " bytes");

            // Calculating SHA1 hash.
            SHA1 sha1 = new SHA1CryptoServiceProvider();
            Byte[] received_hash_ = sha1.ComputeHash(resp_body);

            // Checking that hash is the same.
            for (Int32 i = 0; i < received_hash_.Length; i++)
            {
                if (received_hash_[i] != correct_hash_[i])
                {
                    /*for (Int32 i = 0; i < resp_body.Length; i++)
                    {
                        if (resp_body[i] != body_bytes_[i])
                            Console.WriteLine("Different bytes!");
                    }*/

                    NodeTest.WorkersMonitor.IndicateTestFailed();
                    return false;
                }
            }

            // Incrementing number of finished tests.
            NodeTest.WorkersMonitor.IncrementNumFinishedTests();

            return true;
        }
    }

    class Worker
    {
        public Int32 Id;

        Random rand_;

        Settings settings_;

        static Node GlobalNode = new Node("127.0.0.1", 8080);

        public void Init(Settings settings, Int32 id)
        {
            Id = id;
            rand_ = new Random(0);
            settings_ = settings;
        }

        /// <summary>
        /// Initializes specific new test for worker.
        /// </summary>
        /// <returns></returns>
        NodeTestInstance CreateNewTest()
        {
            UInt64 id = ((UInt64)rand_.Next() << 32);
            Int32 num_echo_bytes = rand_.Next(settings_.MinEchoBytes, settings_.MaxEchoBytes);

            NodeTestInstance test = new NodeTestInstance();

            Boolean async = true;
            switch (settings_.AsyncMode)
            {
                case Settings.AsyncModes.ModeSync:
                {
                    async = false;
                    break;
                }

                case Settings.AsyncModes.ModeAsync:
                {
                    async = true;
                    break;
                }

                case Settings.AsyncModes.ModeRandom:
                {
                    if (rand_.Next(0, 2) == 0)
                        async = true;
                    else
                        async = false;

                    break;
                }
            }

            test.Init(settings_, this, id, async, num_echo_bytes);

            return test;
        }

        /// <summary>
        /// Main worker test loop.
        /// </summary>
        public void WorkerLoop()
        {
            Console.WriteLine(Id + ": test started..");

            try
            {
                for (Int32 j = 0; j < settings_.NumEchoesPerWorker; j++)
                {
                    NodeTestInstance test = CreateNewTest();

                    if (!test.PerformTest(GlobalNode))
                        return;
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine(Id + ": test crashed: " + exc.ToString());

                NodeTest.WorkersMonitor.IndicateTestFailed();
            }
        }
    }

    class GlobalObserver
    {
        Settings settings_;

        Int64 num_finished_tests_;

        public void Init(Settings settings)
        {
            settings_ = settings;
        }

        /// <summary>
        /// Increment global number of finished tests.
        /// </summary>
        public void IncrementNumFinishedTests()
        {
            Interlocked.Increment(ref num_finished_tests_);
        }

        volatile Boolean all_tests_succeeded_ = true;

        /// <summary>
        /// Indicate that some test failed.
        /// </summary>
        public void IndicateTestFailed()
        {
            all_tests_succeeded_ = false;
        }

        /// <summary>
        /// Wait until all tests succeed, fail or timeout.
        /// </summary>
        public Boolean MonitorState()
        {
            Int64 num_ms_passed = 0, num_ms_max = settings_.NumSecondsToWait * 1000;

            Int64 num_tests_all_workers = settings_.NumEchoesPerWorker * settings_.NumWorkers;

            // Looping until either tests succeed, fail or timeout.
            while (
                (num_finished_tests_ < num_tests_all_workers) &&
                (num_ms_passed < num_ms_max) &&
                (true == all_tests_succeeded_))
            {
                Thread.Sleep(10);
                num_ms_passed += 10;
            }

            if (!all_tests_succeeded_)
            {
                Console.WriteLine("Test failed: incorrect echo received.");
                return false;
            }

            if (num_ms_passed >= num_ms_max)
            {
                Console.WriteLine("Test failed: took too long time.");
                return false;
            }

            return true;
        }
    }

    class NodeTest
    {
        public static GlobalObserver WorkersMonitor = new GlobalObserver();

        static Int32 Main(string[] args)
        {
            Settings settings = new Settings();
            settings.Init(args);

            Console.WriteLine("Node Test!");
            Console.WriteLine("Workers: " + settings.NumWorkers);
            Console.WriteLine("Echoes size bytes: [" + settings.MinEchoBytes + ", " + settings.MaxEchoBytes + "]");
            Console.WriteLine("Echoes number per worker: " + settings.NumEchoesPerWorker);
            Console.WriteLine("Max time seconds: " + settings.NumSecondsToWait);

            WorkersMonitor.Init(settings);

            // Starting all workers.
            Worker[] workers = new Worker[settings.NumWorkers];
            for (Int32 w = 0; w < settings.NumWorkers; w++)
            {
                Int32 id = w;
                workers[w] = new Worker();
                workers[w].Init(settings, w);

                new Thread(() => { workers[id].WorkerLoop(); }).Start();
            }

            Stopwatch timer = new Stopwatch();
            timer.Start();

            // Waiting for all workers to succeed or fail.
            if (!WorkersMonitor.MonitorState())
                return 1;

            timer.Stop();

            Console.WriteLine("Test succeeded, took ms: " + timer.ElapsedMilliseconds);
            Console.WriteLine("Echoes/second: " + ((settings.NumWorkers * settings.NumEchoesPerWorker) * 1000.0) / timer.ElapsedMilliseconds);

            return 0;
        }
    }
}
