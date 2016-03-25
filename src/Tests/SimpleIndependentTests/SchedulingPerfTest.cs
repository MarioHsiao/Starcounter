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

    public static Int32 SimplestSyncTaskOnAnyScheduler(Int32 numTasks) {

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

        Console.WriteLine("##teamcity[buildStatisticValue key='{0}' value='{1}']",
            "SimplestSyncTaskOnAnyScheduler_PerSec", (Int32)(numTasks / (sw.ElapsedMilliseconds / 1000.0)));

        return 0;
    }

    public static Int32 SimplestSyncTaskOnSpecificScheduler(Int32 numTasks, Byte schedId) {

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

    public static Int32 SmallAsyncTaskOnSpecificScheduler(Int32 numTasks, Byte schedId) {

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

    public static Int32 SimplestAsyncTaskOnSpecificScheduler(Int32 numTasks, Byte schedId) {

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

        Db.Transact(() => {

            SomeClass dbi = new SomeClass() {
                SomeProperty = "Blabla"
            };
        });

        Int32 errCode = SimplestAsyncTaskOnAnyScheduler(10000000);
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

        errCode = SimplestSyncTaskOnAnyScheduler(1000000);
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

        return 0;
    }
}