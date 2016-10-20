using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using Starcounter.Templates;
using XSONModule = Starcounter.Internal.XSON.Modules.Starcounter_XSON;

#if !DEBUG // Serializer benchmark tests - only in release.

namespace Starcounter.Internal.XSON.Tests {
    [TestFixture]
    public class JsonSerializeBenchmarkTests {
        private const int COL1 = 24;
        private const int COL2 = 8;
        private const int COL3 = 22;
        private const int COL4 = 0;
        private const int LINE_LENGTH = COL1 + COL2 + COL3 + COL4 + 20;

        [Test]
        [Category("LongRunning"), Timeout(5 * 60000)] // timeout 5 minutes
        public static void BenchmarkStandardJsonSerializer() {
            int numberOfTimes = 1000000;

            Helper.ConsoleWriteLine("Benchmarking standard serializer, repeats: " + numberOfTimes);
            Helper.ConsoleWriteLine(AddSpaces("File", COL1) + AddSpaces("Type", COL2) + AddSpaces("Serialize", COL3) + "Deserialize");
            Helper.ConsoleWriteLine(AddChars("", '-', LINE_LENGTH));

            RunStandardJsonBenchmark("jsstyle.json", File.ReadAllText("Json\\jsstyle.json"), numberOfTimes);
            RunStandardJsonBenchmark("person.json", File.ReadAllText("Json\\person.json"), numberOfTimes);
            RunStandardJsonBenchmark("supersimple.json", File.ReadAllText("Json\\supersimple.json"), numberOfTimes);
            RunStandardJsonBenchmark("simple.json", File.ReadAllText("Json\\simple.json"), numberOfTimes);
            RunStandardJsonBenchmark("TestMessage.json", File.ReadAllText("Json\\TestMessage.json"), numberOfTimes);
            RunStandardJsonBenchmark("PlayerAndAccounts", File.ReadAllText("Json\\PlayerAndAccounts.json"), numberOfTimes);
        }

        [Test]
        [Category("LongRunning"), Timeout(5 * 60000)] // timeout 5 minutes
        public static void BenchmarkStandardJsonSerializerWithCompiledJson() {
            int numberOfTimes = 1000000;

            Helper.ConsoleWriteLine("Benchmarking standard serializer (compiled json), repeats: " + numberOfTimes);
            Helper.ConsoleWriteLine(AddSpaces("File", COL1) + AddSpaces("Type", COL2) + AddSpaces("Serialize", COL3) + "Deserialize");
            Helper.ConsoleWriteLine(AddChars("", '-', LINE_LENGTH));

            RunStandardJsonBenchmark("jsstyle.json", jsstyle.DefaultTemplate, numberOfTimes);
            RunStandardJsonBenchmark("person.json", person.DefaultTemplate, numberOfTimes);
            RunStandardJsonBenchmark("supersimple.json", supersimple.DefaultTemplate, numberOfTimes);
            RunStandardJsonBenchmark("simple.json", simple.DefaultTemplate, numberOfTimes);
            RunStandardJsonBenchmark("TestMessage.json", TestMessage.DefaultTemplate, numberOfTimes);
            RunStandardJsonBenchmark("PlayerAndAccounts", PlayerAndAccounts.DefaultTemplate, numberOfTimes);
        }
        
        [Test]
        [Category("LongRunning"), Timeout(5 * 60000)] // timeout 5 minutes
        public static void BenchmarkAllSerializers() {
            int numberOfTimes = 1000000;

            Helper.ConsoleWriteLine("Benchmarking serializers, repeats: " + numberOfTimes);
            Helper.ConsoleWriteLine(AddSpaces("File", COL1) + AddSpaces("Type", COL2) + AddSpaces("Serialize", COL3) + "Deserialize");
            Helper.ConsoleWriteLine(AddChars("", '-', LINE_LENGTH));

            RunStandardJsonBenchmark("jsstyle.json", File.ReadAllText("Json\\jsstyle.json"), numberOfTimes);
            
            Helper.ConsoleWriteLine("");

            RunStandardJsonBenchmark("person.json", File.ReadAllText("Json\\person.json"), numberOfTimes);
            
            Helper.ConsoleWriteLine("");

            RunStandardJsonBenchmark("supersimple.json", File.ReadAllText("Json\\supersimple.json"), numberOfTimes);
            
            Helper.ConsoleWriteLine("");
            
            RunStandardJsonBenchmark("simple.json", File.ReadAllText("Json\\simple.json"), numberOfTimes);
            
            Helper.ConsoleWriteLine("");

            RunStandardJsonBenchmark("TestMessage.json", File.ReadAllText("Json\\TestMessage.json"), numberOfTimes);
            
            Helper.ConsoleWriteLine("");

            RunStandardJsonBenchmark("PlayerAndAccounts.json", File.ReadAllText("Json\\PlayerAndAccounts.json"), numberOfTimes);
        }
        
        private static void RunStandardJsonBenchmark(string name, string json, int numberOfTimes) {
            TValue tObj = Helper.CreateJsonTemplateFromContent(name, json);
            RunStandardJsonBenchmark(name, tObj, numberOfTimes);
        }

        private static void RunStandardJsonBenchmark(string name, TValue tObj, int numberOfTimes) {
            byte[] jsonArr = null;
            int size = 0;
            Json jsonInst;
            DateTime start;
            DateTime stop;
            
            jsonInst = (Json)tObj.CreateInstance();
            Helper.ConsoleWrite(AddSpaces(name, COL1) + AddSpaces("STD", COL2));
           
            // Serializing to standard json.
            start = DateTime.Now;
            for (int i = 0; i < numberOfTimes; i++) {
                jsonArr = jsonInst.ToJsonUtf8();
                size = jsonArr.Length;
            }
            stop = DateTime.Now;
            PrintResult(stop, start, numberOfTimes, COL3);

            // Deserializing from standard json.
            unsafe {
                fixed (byte* p = jsonArr) {
                    start = DateTime.Now;
                    for (int i = 0; i < numberOfTimes; i++) {
                        size = jsonInst.PopulateFromJson((IntPtr)p, size);
                    }
                }
            }
            stop = DateTime.Now;
            PrintResult(stop, start, numberOfTimes, COL4);
            Helper.ConsoleWrite("\n");
        }


        private static string AddSpaces(string org, int totalLength) {
            return AddChars(org, ' ', totalLength);
        }

        private static string AddChars(string org, char c, int totalLength) {
            string after = org;
            for (int i = 0; i < (totalLength - org.Length); i++) {
                after += c;
            }
            return after;
        }

        private static void PrintResult(DateTime stop, DateTime start, int numberOfTimes, int space) {
            var tms = (stop - start).TotalMilliseconds;
            var kps = numberOfTimes / tms;
            var one = (tms * 1000) / numberOfTimes;
            //            Helper.ConsoleWrite(AddSpaces(one.ToString(".00") + " µs", space));
            //            string str = AddSpaces(kps.ToString(".00") + " k/s", space);

            Helper.ConsoleWrite(AddSpaces(kps.ToString(".00") + " k/s (" + one.ToString(".00") + " µs)", space));
        }
    }
}

#endif // Serializer benchmark tests - only in release.
