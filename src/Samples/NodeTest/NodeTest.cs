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

        public Int32 NumWorkers = 3;

        public Int32 MinEchoBytes = 1;

        public Int32 MaxEchoBytes = 100000;

        public Int32 NumEchoesPerWorker = 10000;

        public Int32 NumSecondsToWait = 5000;

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

        Boolean useNodeX_;

#if !FASTEST_POSSIBLE
        Byte[] correct_hash_;
#endif

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
            useNodeX_ = ((num_echo_bytes_ % 2) == 0) ? true : false;

            num_echo_bytes_ = num_echo_bytes;
            body_bytes_ = new Byte[num_echo_bytes_];

#if FILL_RANDOMLY
            // Generating random bytes.
            worker_.Rand.NextBytes(body_bytes_);
#else

            // Filling bytes continuously between 0 and 9
            Byte k = 0;
            for (Int32 i = 0; i < num_echo_bytes_; i++)
            {
                body_bytes_[i] = (Byte)('1' + k);
                //k++;
                //if (k >= 10) k = 0;
            }

#endif

#if !FASTEST_POSSIBLE
            // Calculating SHA1 hash.
            SHA1 sha1 = new SHA1CryptoServiceProvider();
            correct_hash_ = sha1.ComputeHash(body_bytes_);
#endif
        }

        // Sends data, gets the response, and checks its correctness.
        public Boolean PerformTest(Node node)
        {
            if (!async_)
            {
                if (useNodeX_)
                {
                    Response resp = NodeX.POST("/nodetest", body_bytes_, null, null);
                    return CheckResponse(resp);
                }
                else
                {
                    Response resp = node.POST("/nodetest", body_bytes_, null, null);
                    return CheckResponse(resp);
                }
            }
            else
            {
                if (useNodeX_)
                {
                    NodeX.POST("/nodetest", body_bytes_, null, null, null, (Response resp, Object userObject) =>
                    {
                        CheckResponse(resp);
                        return null;
                    });
                }
                else
                {
                    node.POST("/nodetest", body_bytes_, null, null, null, (Response resp, Object userObject) =>
                    {
                        CheckResponse(resp);
                        return null;
                    });
                }

                return true;
            }
        }

        // Checks response correctness.
        Boolean CheckResponse(Response resp)
        {
#if FASTEST_POSSIBLE
            NodeTest.WorkersMonitor.IncrementNumFinishedTests();
            return true;
#else

            if (null == resp)
                throw new Exception("Response is null by some reason!");

            try
            {
                Byte[] resp_body = resp.BodyBytes;
                if (resp_body.Length != num_echo_bytes_)
                {
                    Console.WriteLine("Wrong echo size! Correct size: " + num_echo_bytes_ + ", wrong: " + resp_body.Length + " [Async=" + async_ + "]");
                    NodeTest.WorkersMonitor.FailTest();
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
                        for (Int32 k = 0; k < resp_body.Length; k++)
                        {
                            if (resp_body[k] != body_bytes_[k])
                            {
                                //Debugger.Launch();
                            }
                        }

                        Console.WriteLine("Wrong echo content! Echo size: " + num_echo_bytes_ + " [Async=" + async_ + "]");
                        NodeTest.WorkersMonitor.FailTest();
                        return false;
                    }
                }

                // Incrementing number of finished tests.
                NodeTest.WorkersMonitor.IncrementNumFinishedTests();
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.ToString());
                throw exc;
            }

            return true;
#endif
        }
    }

    class Worker
    {
        public Int32 Id;

        public Random Rand;

        Settings settings_;

        Node worker_node_ = new Node("127.0.0.1", 8080);

        public void Init(Settings settings, Int32 id)
        {
            Id = id;
            Rand = new Random(id);
            settings_ = settings;
        }

        /// <summary>
        /// Initializes specific new test for worker.
        /// </summary>
        /// <returns></returns>
        NodeTestInstance CreateNewTest()
        {
            UInt64 id = ((UInt64)Rand.Next() << 32);
            Int32 num_echo_bytes = Rand.Next(settings_.MinEchoBytes, settings_.MaxEchoBytes);

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
                    if (Rand.Next(0, 2) == 0)
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

                    if (!test.PerformTest(worker_node_))
                        return;

                    // Checking if tests has already failed.
                    if (NodeTest.WorkersMonitor.HasTestFailed)
                        return;
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine(Id + ": test crashed: " + exc.ToString());

                NodeTest.WorkersMonitor.FailTest();
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
        /// Returns True if tests failed.
        /// </summary>
        public Boolean HasTestFailed
        {
            get { return !all_tests_succeeded_; }
        }

        /// <summary>
        /// Indicate that some test failed.
        /// </summary>
        public void FailTest()
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
                Console.Error.WriteLine("Test failed: incorrect echo received.");
                return false;
            }

            if (num_ms_passed >= num_ms_max)
            {
                Console.Error.WriteLine("Test failed: took too long time.");
                FailTest();
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
                Environment.Exit(1);

            timer.Stop();

            Console.WriteLine("Test succeeded, took ms: " + timer.ElapsedMilliseconds);

            Double echoesPerSecond = ((settings.NumWorkers * settings.NumEchoesPerWorker) * 1000.0) / timer.ElapsedMilliseconds;
            TestLogger.ReportStatistics(
                String.Format("nodetest_workers_{0}_echo_minbytes_{1}_maxbytes_{2}__echoes_per_second", settings.NumWorkers, settings.MinEchoBytes, settings.MaxEchoBytes),
                echoesPerSecond);

            Console.WriteLine("Echoes/second: " + echoesPerSecond);

            return 0;
        }
    }
}
