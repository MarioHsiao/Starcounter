using Starcounter;
using System;
using Starcounter.Metadata;
using Starcounter.Internal;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

class TestAppName {

    public static Int32 Run() {

        Console.WriteLine("Starting 'AppName' tests...");

        String expectedAppName = StarcounterEnvironment.AppName;

        Assert.IsTrue(!String.IsNullOrEmpty(expectedAppName));

        AutoResetEvent resetEvent = new AutoResetEvent(false);

        Thread queryThread = new Thread(() => {

            try {

                Assert.IsTrue(StarcounterEnvironment.AppName == expectedAppName);

                AutoResetEvent scEvent = new AutoResetEvent(false);

                Scheduling.ScheduleTask(() => {

                    Assert.IsTrue(StarcounterEnvironment.AppName == expectedAppName);

                    scEvent.Set();
                });

                scEvent.WaitOne(3000);

            } finally {

                resetEvent.Set();
            }
        });

        queryThread.Start();

        resetEvent.WaitOne(3000);

        Assert.IsTrue(StarcounterEnvironment.AppName == expectedAppName);

        return 0;
    }
}