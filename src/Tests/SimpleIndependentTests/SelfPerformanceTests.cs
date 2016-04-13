using Starcounter;
using System;
using Starcounter.Metadata;
using Starcounter.Internal;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;

class TestSelfPerformance {

    public static string RandomString(Random ran, int length) {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        return new string(Enumerable.Repeat(chars, length)
          .Select(s => s[ran.Next(s.Length)]).ToArray());
    }

    public static void CreateGetHandlers(String[] names) {

        foreach (String name in names) {
            Handle.GET(name, () => {
                return name;
            });
        }
    }

    public static Int32 Run() {

        Console.WriteLine("Starting 'Self' performance tests...");

        Int32 numToGenerate = 1000;
        String[] uris = new String[numToGenerate];
        Random rand = new Random();
        for (Int32 i = 0; i < numToGenerate; i++) {
            uris[i] = "/" + RandomString(rand, 20);
        }

        CreateGetHandlers(uris);

        Int32 numSelfGets = 1000000;

        // NOTE: Excluding first Self call because it does expensive code generation.
        Self.GET<String>(uris[0]);

        Stopwatch sw = Stopwatch.StartNew();

        for (Int32 i = 0; i < numSelfGets; i++) {
            Int32 randIndex = rand.Next(0, numToGenerate - 1);
            String s = Self.GET<String>(uris[randIndex]);
            Assert.IsTrue(s == uris[randIndex]);
        }

        sw.Stop();

        Console.WriteLine("##teamcity[buildStatisticValue key='{0}' value='{1}']",
            "NumSimpleSelfGetsPerSec", (Int32) (numSelfGets / (sw.ElapsedMilliseconds / 1000.0)));

        return 0;
    }
}