using Starcounter;
using System;
using Starcounter.Metadata;
using Starcounter.Internal;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;

[Database]
public class SomeClass {
    public String SomeProperty;
}

class SchedulingPerfTest {

    public static String SimplestSyncTaskOnAnySchedulerFromDotNetThread(Int32 numTasks) {

        Console.WriteLine("Starting SimplestSyncTaskOnAnySchedulerFromDotNetThread");

        Int32 numFinishedTasks = 0;

        Stopwatch sw = Stopwatch.StartNew();

        for (Int32 i = 0; i < numTasks; i++) {

            Scheduling.ScheduleTask(() => {

                Interlocked.Increment(ref numFinishedTasks);
            }, true);
        }

        while (numFinishedTasks != numTasks) {
            Thread.Sleep(1);
        }

        sw.Stop();

        return String.Format("##teamcity[buildStatisticValue key='{0}' value='{1}']",
            "SimplestSyncTaskOnAnySchedulerFromDotNetThread_PerSec", (Int32)(numTasks / (sw.ElapsedMilliseconds / 1000.0)));
    }

    public static Int32 SimplestSyncTaskOnAnyScheduler(Int32 numTasks) {

        Console.WriteLine("Starting SimplestSyncTaskOnAnyScheduler");

        Int32 numFinishedTasks = 0;

        Stopwatch sw = Stopwatch.StartNew();

        for (Int32 i = 0; i < numTasks; i++) {

            Scheduling.ScheduleTask(() => {

                Interlocked.Increment(ref numFinishedTasks);
            }, true);
        }

        while (numFinishedTasks != numTasks) {
            StarcounterEnvironment.RunDetached(() => {
                Thread.Sleep(1);
            });
        }

        sw.Stop();

        Console.WriteLine("##teamcity[buildStatisticValue key='{0}' value='{1}']",
            "SimplestSyncTaskOnAnyScheduler_PerSec", (Int32)(numTasks / (sw.ElapsedMilliseconds / 1000.0)));

        return 0;
    }

    public static Int32 LargeSyncTaskOnAnyScheduler(Int32 numTasks) {

        Console.WriteLine("Starting LargeSyncTaskOnAnyScheduler");

        Int32 numFinishedTasks = 0;

        Stopwatch sw = Stopwatch.StartNew();

        for (Int32 i = 0; i < numTasks; i++) {

            Scheduling.ScheduleTask(() => {

                for (Int32 c = 0; c < 100; c++) {

                    var x = Db.SQL<SomeClass>("select t from SomeClass t").First;
                    if (x.SomeProperty != "Blabla") {
                        throw new ArgumentOutOfRangeException("Required class object is not found!");
                    }
                }

                Interlocked.Increment(ref numFinishedTasks);
            }, true);
        }

        while (numFinishedTasks != numTasks) {
            Thread.Sleep(1);
        }

        sw.Stop();

        Console.WriteLine("##teamcity[buildStatisticValue key='{0}' value='{1}']",
            "LargeSyncTaskOnAnyScheduler_PerSec", (Int32)(numTasks / (sw.ElapsedMilliseconds / 1000.0)));

        return 0;
    }

    public static Int32 SimplestSyncTaskOnSpecificScheduler(Int32 numTasks, Byte schedId) {

        Console.WriteLine("Starting SimplestSyncTaskOnSpecificScheduler");

        Int32 numFinishedTasks = 0;

        Stopwatch sw = Stopwatch.StartNew();

        for (Int32 i = 0; i < numTasks; i++) {

            Scheduling.ScheduleTask(() => {

                if (StarcounterEnvironment.CurrentSchedulerId == schedId) {
                    Interlocked.Increment(ref numFinishedTasks);
                } else {
                    throw new ArgumentOutOfRangeException("Wrong scheduler id: " + schedId);
                }

            }, true, schedId);
        }

        while (numFinishedTasks != numTasks) {
            Thread.Sleep(1);
        }

        sw.Stop();

        Console.WriteLine("##teamcity[buildStatisticValue key='{0}' value='{1}']",
            "SimplestSyncTaskOnSpecificScheduler" + schedId + "_PerSec", (Int32)(numTasks / (sw.ElapsedMilliseconds / 1000.0)));

        return 0;
    }

    public static Int32 SmallAsyncTaskOnAnyScheduler(Int32 numTasks) {

        Console.WriteLine("Starting SmallAsyncTaskOnAnyScheduler");

        Int32 numFinishedTasks = 0;

        Stopwatch sw = Stopwatch.StartNew();

        for (Int32 i = 0; i < numTasks; i++) {

            Scheduling.ScheduleTask(() => {

                var x = Db.SQL<SomeClass>("select t from SomeClass t").First;
                if (x.SomeProperty != "Blabla") {
                    throw new ArgumentOutOfRangeException("Required class object is not found!");
                }

                Interlocked.Increment(ref numFinishedTasks);
            });
        }

        while (numFinishedTasks != numTasks) {
            Thread.Sleep(1);
        }

        sw.Stop();

        Console.WriteLine("##teamcity[buildStatisticValue key='{0}' value='{1}']",
            "SmallAsyncTaskOnAnyScheduler_PerSec", (Int32)(numTasks / (sw.ElapsedMilliseconds / 1000.0)));

        return 0;
    }

    public static Int32 TestRunDetachedPerformance(Int32 numDetaches) {

        Console.WriteLine("Starting TestRunDetachedPerformance");

        Int32 numFinishedTasks = 0;

        Stopwatch sw = Stopwatch.StartNew();

        for (Int32 i = 0; i < numDetaches; i++) {

            StarcounterEnvironment.RunDetached(() => {
                Interlocked.Increment(ref numFinishedTasks);
            });
        }

        sw.Stop();

        Console.WriteLine("##teamcity[buildStatisticValue key='{0}' value='{1}']",
            "NumRunDetached_PerSec", (Int32)(numFinishedTasks / (sw.ElapsedMilliseconds / 1000.0)));

        Console.WriteLine("Finished run-detached performance tests");

        return 0;
    }

    public static Int32 EasyhookPerformanceTest(Int32 numRuns) {

        Console.WriteLine("Starting EasyhookPerformanceTest");

        Int32 numFinishedTasks = 0;
        Mutex m = new Mutex(false);

        Stopwatch sw = Stopwatch.StartNew();

        for (Int32 i = 0; i < numRuns; i++) {
            m.WaitOne();
            Interlocked.Increment(ref numFinishedTasks);
            m.ReleaseMutex();
        }

        sw.Stop();

        Console.WriteLine("##teamcity[buildStatisticValue key='{0}' value='{1}']",
            "NumEasyHookCalls_PerSec", (Int32)(numFinishedTasks / (sw.ElapsedMilliseconds / 1000.0)));

        Console.WriteLine("Finished EasyHook performance tests");

        return 0;
    }

    public static Int32 CooperativeSchedulingTest(Int32 numLongRunningTasks) {

        Console.WriteLine("Starting CooperativeSchedulingTest");

        Int32 numFinishedTasks = 0;
        Mutex m = new Mutex(false);
        Int64 sharedCounter = 0;
        Int64 fakeCounter = 0;
        Int32 prevTaskId = 0;

        Stopwatch sw = Stopwatch.StartNew();

        for (Int32 i = 0; i < numLongRunningTasks; i++) {

            Int32 z = i;

            Scheduling.ScheduleTask(() => {

                Int32 taskId = z;
                prevTaskId = taskId;

                for (Int32 k = 0; k < 100; k++) {

                    // This causes scheduler to switch.
                    m.WaitOne();
                    for (Int32 v = 0; v < 100000 * (taskId + 1); v++) {
                        fakeCounter++;
                    }
                    m.ReleaseMutex();

                    Int64 orig = sharedCounter;
                    const Int32 numTimes = 1000000;
                    for (Int32 v = 0; v < numTimes; v++) {
                        sharedCounter++;
                    }

                    // Checking if two parallel tasks didn't run simultaneously.
                    if (orig + numTimes != sharedCounter) {
                        throw new Exception("Parallel execution detected in cooperative scheduling test!");
                    }
                }

                Interlocked.Increment(ref numFinishedTasks);

            }, false, 0);
        }

        while (numFinishedTasks != numLongRunningTasks) {
            Thread.Sleep(1000);
            Console.WriteLine("Finished: " + numFinishedTasks);
        }

        sw.Stop();

        Console.WriteLine("##teamcity[buildStatisticValue key='{0}' value='{1}']",
            "NumLongTasksWithCooperativeScheduling_PerSec", (Int32)(numLongRunningTasks / (sw.ElapsedMilliseconds / 1000.0)));

        Console.WriteLine("Finished cooperative scheduling performance tests");

        return 0;
    }

    public static Int32 LargeAsyncTaskOnAnyScheduler(Int32 numTasks) {

        Console.WriteLine("Starting LargeAsyncTaskOnAnyScheduler");

        Int32 numFinishedTasks = 0;

        Stopwatch sw = Stopwatch.StartNew();

        for (Int32 i = 0; i < numTasks; i++) {

            Scheduling.ScheduleTask(() => {

                for (Int32 c = 0; c < 100; c++) {

                    var x = Db.SQL<SomeClass>("select t from SomeClass t").First;
                    if (x.SomeProperty != "Blabla") {
                        throw new ArgumentOutOfRangeException("Required class object is not found!");
                    }
                }

                Interlocked.Increment(ref numFinishedTasks);
            });
        }

        while (numFinishedTasks != numTasks) {
            Thread.Sleep(1);
        }

        sw.Stop();

        Console.WriteLine("##teamcity[buildStatisticValue key='{0}' value='{1}']",
            "LargeAsyncTaskOnAnyScheduler_PerSec", (Int32)(numTasks / (sw.ElapsedMilliseconds / 1000.0)));

        return 0;
    }

    public static Int32 SmallAsyncTaskOnSpecificScheduler(Int32 numTasks, Byte schedId) {

        Console.WriteLine("Starting SmallAsyncTaskOnSpecificScheduler");

        Int32 numFinishedTasks = 0;

        Stopwatch sw = Stopwatch.StartNew();

        for (Int32 i = 0; i < numTasks; i++) {

            Scheduling.ScheduleTask(() => {

                if (StarcounterEnvironment.CurrentSchedulerId == schedId) {

                    var x = Db.SQL<SomeClass>("select t from SomeClass t").First;
                    if (x.SomeProperty != "Blabla") {
                        throw new ArgumentOutOfRangeException("Required class object is not found!");
                    }

                    Interlocked.Increment(ref numFinishedTasks);

                } else {
                    throw new ArgumentOutOfRangeException("Wrong scheduler id: " + schedId);
                }

            }, false, schedId);
        }

        while (numFinishedTasks != numTasks) {
            Thread.Sleep(1);
        }

        sw.Stop();

        Console.WriteLine("##teamcity[buildStatisticValue key='{0}' value='{1}']",
            "SmallAsyncTaskOnSpecificScheduler" + schedId + "_PerSec", (Int32)(numTasks / (sw.ElapsedMilliseconds / 1000.0)));

        return 0;
    }

    public static Int32 LargeAsyncTaskOnSpecificScheduler(Int32 numTasks, Byte schedId) {

        Console.WriteLine("Starting LargeAsyncTaskOnSpecificScheduler");

        Int32 numFinishedTasks = 0;

        Stopwatch sw = Stopwatch.StartNew();

        for (Int32 i = 0; i < numTasks; i++) {

            Scheduling.ScheduleTask(() => {

                if (StarcounterEnvironment.CurrentSchedulerId == schedId) {

                    for (Int32 c = 0; c < 100; c++) {

                        var x = Db.SQL<SomeClass>("select t from SomeClass t").First;

                        if (x.SomeProperty != "Blabla") {
                            throw new ArgumentOutOfRangeException("Required class object is not found!");
                        }
                    }

                    Interlocked.Increment(ref numFinishedTasks);

                } else {
                    throw new ArgumentOutOfRangeException("Wrong scheduler id: " + schedId);
                }

            }, false, schedId);
        }

        while (numFinishedTasks != numTasks) {
            Thread.Sleep(1);
        }

        sw.Stop();

        Console.WriteLine("##teamcity[buildStatisticValue key='{0}' value='{1}']",
            "LargeAsyncTaskOnSpecificScheduler" + schedId + "_PerSec", (Int32)(numTasks / (sw.ElapsedMilliseconds / 1000.0)));

        return 0;
    }

    public static Int32 SimplestAsyncTaskOnSpecificScheduler(Int32 numTasks, Byte schedId) {

        Console.WriteLine("Starting SimplestAsyncTaskOnSpecificScheduler");

        Int32 numFinishedTasks = 0;

        Stopwatch sw = Stopwatch.StartNew();

        for (Int32 i = 0; i < numTasks; i++) {

            Scheduling.ScheduleTask(() => {

                if (StarcounterEnvironment.CurrentSchedulerId == schedId) {
                    Interlocked.Increment(ref numFinishedTasks);
                } else {
                    throw new ArgumentOutOfRangeException("Wrong scheduler id: " + schedId);
                }

            }, false, schedId);
        }

        while (numFinishedTasks != numTasks) {
            Thread.Sleep(1);
        }

        sw.Stop();

        Console.WriteLine("##teamcity[buildStatisticValue key='{0}' value='{1}']",
            "SimplestAsyncTaskOnSpecificScheduler" + schedId + "_PerSec", (Int32)(numTasks / (sw.ElapsedMilliseconds / 1000.0)));

        return 0;
    }

    public static Int32 SimplestAsyncTaskOnAnyScheduler(Int32 numTasks) {

        Console.WriteLine("Starting SimplestAsyncTaskOnAnyScheduler");

        Int32 numFinishedTasks = 0;

        Stopwatch sw = Stopwatch.StartNew();

        for (Int32 i = 0; i < numTasks; i++) {

            Scheduling.ScheduleTask(() => {
                Interlocked.Increment(ref numFinishedTasks);
            });
        }

        while (numFinishedTasks != numTasks) {
            Thread.Sleep(1);
        }

        sw.Stop();

        Console.WriteLine("##teamcity[buildStatisticValue key='{0}' value='{1}']",
            "SimplestAsyncTaskOnAnyScheduler_PerSec", (Int32)(numTasks / (sw.ElapsedMilliseconds / 1000.0)));

        return 0;
    }

    public static Int32 Run() {

        Console.WriteLine("Starting scheduling performance tests");

        Db.Transact(() => {

            SomeClass dbi = new SomeClass() {
                SomeProperty = "Blabla"
            };
        });

        Int32 errCode;

        errCode = EasyhookPerformanceTest(1000000);
        if (0 != errCode) {
            return errCode;
        }

        errCode = TestRunDetachedPerformance(1000000);
        if (0 != errCode) {
            return errCode;
        }

        errCode = CooperativeSchedulingTest(10);
        if (0 != errCode) {
            return errCode;
        }

        errCode = SimplestAsyncTaskOnAnyScheduler(10000000);
        if (0 != errCode) {
            return errCode;
        }

        errCode = SimplestAsyncTaskOnSpecificScheduler(10000000, 1);
        if (0 != errCode) {
            return errCode;
        }

        errCode = SimplestAsyncTaskOnSpecificScheduler(10000000, 0);
        if (0 != errCode) {
            return errCode;
        }

        errCode = SmallAsyncTaskOnAnyScheduler(10000000);
        if (0 != errCode) {
            return errCode;
        }

        errCode = SmallAsyncTaskOnSpecificScheduler(10000000, 1);
        if (0 != errCode) {
            return errCode;
        }

        errCode = SmallAsyncTaskOnSpecificScheduler(10000000, 0);
        if (0 != errCode) {
            return errCode;
        }

        String detNetThreadResultString = "";
        Thread t = new Thread(() => {
            detNetThreadResultString = SimplestSyncTaskOnAnySchedulerFromDotNetThread(1000000);
        });
        t.Start();
        StarcounterEnvironment.RunDetached(() => {
            t.Join();
        });

        Console.WriteLine(detNetThreadResultString);

        errCode = SimplestSyncTaskOnAnyScheduler(1000000);
        if (0 != errCode) {
            return errCode;
        }

        errCode = SimplestSyncTaskOnSpecificScheduler(1000000, 2);
        if (0 != errCode) {
            return errCode;
        }

        errCode = SimplestSyncTaskOnSpecificScheduler(1000000, 1);
        if (0 != errCode) {
            return errCode;
        }

        errCode = SimplestSyncTaskOnSpecificScheduler(1000000, 0);
        if (0 != errCode) {
            return errCode;
        }

        errCode = LargeAsyncTaskOnAnyScheduler(200000);
        if (0 != errCode) {
            return errCode;
        }

        errCode = LargeAsyncTaskOnSpecificScheduler(200000, 1);
        if (0 != errCode) {
            return errCode;
        }

        errCode = LargeAsyncTaskOnSpecificScheduler(200000, 0);
        if (0 != errCode) {
            return errCode;
        }

        errCode = LargeSyncTaskOnAnyScheduler(200000);
        if (0 != errCode) {
            return errCode;
        }        

        return 0;
    }
}