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

        //[Test]
        //[Category("LongRunning"), Timeout(5 * 60000)] // timeout 5 minutes
        //public static void BenchmarkPlayerAndAccounts() {
        //    int numberOfTimes = 1000000;
        //    //            int numberOfTimes = 1;

        //    Helper.ConsoleWriteLine("Benchmarking PlayerAndAccounts.json, repeats: " + numberOfTimes);
        //    Helper.ConsoleWriteLine(AddSpaces("File", COL1) + AddSpaces("Type", COL2) + AddSpaces("Serialize", COL3) + AddSpaces("Deseralize", COL4));
        //    Helper.ConsoleWriteLine("-------------------------------------------------------------------------------");

        //    string jsonStr = File.ReadAllText("Json\\PlayerAndAccounts.json");

        //    // First we use runtime created json
        //    //            RunStandardJsonBenchmark("PlayerAndAccounts", jsonStr, numberOfTimes, false);

        //    // Then we run the same benchmark but with compile-time generated json.
        //    RunStandardJsonBenchmark("PlayerAndAccounts(gen)", jsonStr, numberOfTimes, false, PlayerAndAccounts.DefaultTemplate);
        //}

        [Test]
        [Category("LongRunning"), Timeout(5 * 60000)] // timeout 5 minutes
        [Ignore("Requires fixing FTJ serializer")]
        public static void BenchmarkFTJSerializer() {
            int numberOfTimes = 1000000;

            Helper.ConsoleWriteLine("Benchmarking ftj serializer, repeats: " + numberOfTimes);
            Helper.ConsoleWriteLine(AddSpaces("File", COL1) + AddSpaces("Type", COL2) + AddSpaces("Serialize", COL3) + "Deserialize");
            Helper.ConsoleWriteLine(AddChars("", '-', LINE_LENGTH));

            RunFTJBenchmark("jsstyle.json", File.ReadAllText("Json\\jsstyle.json"), numberOfTimes, false);
            RunFTJBenchmark("person.json", File.ReadAllText("Json\\person.json"), numberOfTimes, false);
            RunFTJBenchmark("supersimple.json", File.ReadAllText("Json\\supersimple.json"), numberOfTimes, false);
            RunFTJBenchmark("simple.json", File.ReadAllText("Json\\simple.json"), numberOfTimes, false);
            RunFTJBenchmark("TestMessage.json", File.ReadAllText("Json\\TestMessage.json"), numberOfTimes, false);
            RunFTJBenchmark("PlayerAndAccounts", File.ReadAllText("Json\\PlayerAndAccounts.json"), numberOfTimes, false);
        }

        [Test]
        [Category("LongRunning"), Timeout(5 * 60000)] // timeout 5 minutes
        [Ignore("Requires fixing FTJ serializer")]
        public static void BenchmarkFTJSerializerWithCompiledJson() {
            int numberOfTimes = 1000000;

            Helper.ConsoleWriteLine("Benchmarking ftj serializer (compiled json), repeats: " + numberOfTimes);
            Helper.ConsoleWriteLine(AddSpaces("File", COL1) + AddSpaces("Type", COL2) + AddSpaces("Serialize", COL3) + "Deserialize");
            Helper.ConsoleWriteLine(AddChars("", '-', LINE_LENGTH));

            RunFTJBenchmark("jsstyle.json", jsstyle.DefaultTemplate, numberOfTimes, false);
            RunFTJBenchmark("person.json", person.DefaultTemplate, numberOfTimes, false);
            RunFTJBenchmark("supersimple.json", supersimple.DefaultTemplate, numberOfTimes, false);
            RunFTJBenchmark("simple.json", simple.DefaultTemplate, numberOfTimes, false);
            RunFTJBenchmark("TestMessage.json", TestMessage.DefaultTemplate, numberOfTimes, false);
            RunFTJBenchmark("PlayerAndAccounts", PlayerAndAccounts.DefaultTemplate, numberOfTimes, false);
        }

        [Test]
        [Category("LongRunning"), Timeout(5 * 60000)] // timeout 5 minutes
        [Ignore("Requires fixing FTJ serializer")]
        public static void BenchmarkFTJCodegenSerializer() {
            int numberOfTimes = 1000000;

            Helper.ConsoleWriteLine("Benchmarking ftj serializer, repeats: " + numberOfTimes);
            Helper.ConsoleWriteLine(AddSpaces("File", COL1) + AddSpaces("Type", COL2) + AddSpaces("Serialize", COL3) + "Deserialize");
            Helper.ConsoleWriteLine(AddChars("", '-', LINE_LENGTH));

            RunFTJBenchmark("jsstyle.json", File.ReadAllText("Json\\jsstyle.json"), numberOfTimes, true);
            RunFTJBenchmark("person.json", File.ReadAllText("Json\\person.json"), numberOfTimes, true);
            RunFTJBenchmark("supersimple.json", File.ReadAllText("Json\\supersimple.json"), numberOfTimes, true);
            RunFTJBenchmark("simple.json", File.ReadAllText("Json\\simple.json"), numberOfTimes, true);
            RunFTJBenchmark("TestMessage.json", File.ReadAllText("Json\\TestMessage.json"), numberOfTimes, true);
            RunFTJBenchmark("PlayerAndAccounts", File.ReadAllText("Json\\PlayerAndAccounts.json"), numberOfTimes, true);
        }

        [Test]
        [Category("LongRunning"), Timeout(5 * 60000)] // timeout 5 minutes
        [Ignore("Requires fixing FTJ serializer")]
        public static void BenchmarkFTJCodegenSerializerWithCompiledJson() {
            int numberOfTimes = 1000000;

            Helper.ConsoleWriteLine("Benchmarking ftj serializer (compiled json), repeats: " + numberOfTimes);
            Helper.ConsoleWriteLine(AddSpaces("File", COL1) + AddSpaces("Type", COL2) + AddSpaces("Serialize", COL3) + "Deserialize");
            Helper.ConsoleWriteLine(AddChars("", '-', LINE_LENGTH));

            RunFTJBenchmark("jsstyle.json", jsstyle.DefaultTemplate, numberOfTimes, true);
            RunFTJBenchmark("person.json", person.DefaultTemplate, numberOfTimes, true);
            RunFTJBenchmark("supersimple.json", supersimple.DefaultTemplate, numberOfTimes, true);
            RunFTJBenchmark("simple.json", simple.DefaultTemplate, numberOfTimes, true);
            RunFTJBenchmark("TestMessage.json", TestMessage.DefaultTemplate, numberOfTimes, true);
            RunFTJBenchmark("PlayerAndAccounts", PlayerAndAccounts.DefaultTemplate, numberOfTimes, true);
        }

        [Test]
        [Category("LongRunning"), Timeout(5 * 60000)] // timeout 5 minutes
        public static void BenchmarkStandardJsonSerializer() {
            int numberOfTimes = 1000000;

            Helper.ConsoleWriteLine("Benchmarking standard serializer, repeats: " + numberOfTimes);
            Helper.ConsoleWriteLine(AddSpaces("File", COL1) + AddSpaces("Type", COL2) + AddSpaces("Serialize", COL3) + "Deserialize");
            Helper.ConsoleWriteLine(AddChars("", '-', LINE_LENGTH));

            RunStandardJsonBenchmark("jsstyle.json", File.ReadAllText("Json\\jsstyle.json"), numberOfTimes, false);
            RunStandardJsonBenchmark("person.json", File.ReadAllText("Json\\person.json"), numberOfTimes, false);
            RunStandardJsonBenchmark("supersimple.json", File.ReadAllText("Json\\supersimple.json"), numberOfTimes, false);
            RunStandardJsonBenchmark("simple.json", File.ReadAllText("Json\\simple.json"), numberOfTimes, false);
            RunStandardJsonBenchmark("TestMessage.json", File.ReadAllText("Json\\TestMessage.json"), numberOfTimes, false);
            RunStandardJsonBenchmark("PlayerAndAccounts", File.ReadAllText("Json\\PlayerAndAccounts.json"), numberOfTimes, false);
        }

        [Test]
        [Category("LongRunning"), Timeout(5 * 60000)] // timeout 5 minutes
        public static void BenchmarkStandardJsonSerializerWithCompiledJson() {
            int numberOfTimes = 1000000;

            Helper.ConsoleWriteLine("Benchmarking standard serializer (compiled json), repeats: " + numberOfTimes);
            Helper.ConsoleWriteLine(AddSpaces("File", COL1) + AddSpaces("Type", COL2) + AddSpaces("Serialize", COL3) + "Deserialize");
            Helper.ConsoleWriteLine(AddChars("", '-', LINE_LENGTH));

            RunStandardJsonBenchmark("jsstyle.json", jsstyle.DefaultTemplate, numberOfTimes, false);
            RunStandardJsonBenchmark("person.json", person.DefaultTemplate, numberOfTimes, false);
            RunStandardJsonBenchmark("supersimple.json", supersimple.DefaultTemplate, numberOfTimes, false);
            RunStandardJsonBenchmark("simple.json", simple.DefaultTemplate, numberOfTimes, false);
            RunStandardJsonBenchmark("TestMessage.json", TestMessage.DefaultTemplate, numberOfTimes, false);
            RunStandardJsonBenchmark("PlayerAndAccounts", PlayerAndAccounts.DefaultTemplate, numberOfTimes, false);
        }

        [Test]
        [Category("LongRunning"), Timeout(5 * 60000)] // timeout 5 minutes
        public static void BenchmarkStandardCodegenJsonSerializer() {
            int numberOfTimes = 1000000;

            Helper.ConsoleWriteLine("Benchmarking standard serializer, repeats: " + numberOfTimes);
            Helper.ConsoleWriteLine(AddSpaces("File", COL1) + AddSpaces("Type", COL2) + AddSpaces("Serialize", COL3) + "Deserialize");
            Helper.ConsoleWriteLine(AddChars("", '-', LINE_LENGTH));

            RunStandardJsonBenchmark("jsstyle.json", File.ReadAllText("Json\\jsstyle.json"), numberOfTimes, true);
            RunStandardJsonBenchmark("person.json", File.ReadAllText("Json\\person.json"), numberOfTimes, true);
            RunStandardJsonBenchmark("supersimple.json", File.ReadAllText("Json\\supersimple.json"), numberOfTimes, true);
            RunStandardJsonBenchmark("simple.json", File.ReadAllText("Json\\simple.json"), numberOfTimes, true);
            RunStandardJsonBenchmark("TestMessage.json", File.ReadAllText("Json\\TestMessage.json"), numberOfTimes, true);
            RunStandardJsonBenchmark("PlayerAndAccounts", File.ReadAllText("Json\\PlayerAndAccounts.json"), numberOfTimes, true);
        }

        [Test]
        [Category("LongRunning"), Timeout(5 * 60000)] // timeout 5 minutes
        public static void BenchmarkStandardCodegenJsonSerializerWithCompiledJson() {
            int numberOfTimes = 1000000;

            Helper.ConsoleWriteLine("Benchmarking standard serializer, repeats: " + numberOfTimes);
            Helper.ConsoleWriteLine(AddSpaces("File", COL1) + AddSpaces("Type", COL2) + AddSpaces("Serialize", COL3) + "Deserialize");
            Helper.ConsoleWriteLine(AddChars("", '-', LINE_LENGTH));

            RunStandardJsonBenchmark("jsstyle.json", jsstyle.DefaultTemplate, numberOfTimes, true);
            RunStandardJsonBenchmark("person.json", person.DefaultTemplate, numberOfTimes, true);
            RunStandardJsonBenchmark("supersimple.json", supersimple.DefaultTemplate, numberOfTimes, true);
            RunStandardJsonBenchmark("simple.json", simple.DefaultTemplate, numberOfTimes, true);
            RunStandardJsonBenchmark("TestMessage.json", TestMessage.DefaultTemplate, numberOfTimes, true);
            RunStandardJsonBenchmark("PlayerAndAccounts", PlayerAndAccounts.DefaultTemplate, numberOfTimes, true);
        }

        [Test]
        [Category("LongRunning"), Timeout(5 * 60000)] // timeout 5 minutes
        public static void BenchmarkAllSerializers() {
            int numberOfTimes = 1000000;

            Helper.ConsoleWriteLine("Benchmarking serializers, repeats: " + numberOfTimes);
            Helper.ConsoleWriteLine(AddSpaces("File", COL1) + AddSpaces("Type", COL2) + AddSpaces("Serialize", COL3) + "Deserialize");
            Helper.ConsoleWriteLine(AddChars("", '-', LINE_LENGTH));

            RunFTJBenchmark("jsstyle.json", File.ReadAllText("Json\\jsstyle.json"), numberOfTimes, false);
            RunFTJBenchmark("jsstyle.json", File.ReadAllText("Json\\jsstyle.json"), numberOfTimes, true);
            RunStandardJsonBenchmark("jsstyle.json", File.ReadAllText("Json\\jsstyle.json"), numberOfTimes, false);
            RunStandardJsonBenchmark("jsstyle.json", File.ReadAllText("Json\\jsstyle.json"), numberOfTimes, true);

            Helper.ConsoleWriteLine("");

            RunFTJBenchmark("person.json", File.ReadAllText("Json\\person.json"), numberOfTimes, false);
            RunFTJBenchmark("person.json", File.ReadAllText("Json\\person.json"), numberOfTimes, true);
            RunStandardJsonBenchmark("person.json", File.ReadAllText("Json\\person.json"), numberOfTimes, false);
            RunStandardJsonBenchmark("person.json", File.ReadAllText("Json\\person.json"), numberOfTimes, true);

            Helper.ConsoleWriteLine("");

            RunFTJBenchmark("supersimple.json", File.ReadAllText("Json\\supersimple.json"), numberOfTimes, false);
            RunFTJBenchmark("supersimple.json", File.ReadAllText("Json\\supersimple.json"), numberOfTimes, true);
            RunStandardJsonBenchmark("supersimple.json", File.ReadAllText("Json\\supersimple.json"), numberOfTimes, false);
            RunStandardJsonBenchmark("supersimple.json", File.ReadAllText("Json\\supersimple.json"), numberOfTimes, true);

            Helper.ConsoleWriteLine("");

            RunFTJBenchmark("simple.json", File.ReadAllText("Json\\simple.json"), numberOfTimes, false);
            RunFTJBenchmark("simple.json", File.ReadAllText("Json\\simple.json"), numberOfTimes, true);
            RunStandardJsonBenchmark("simple.json", File.ReadAllText("Json\\simple.json"), numberOfTimes, false);
            RunStandardJsonBenchmark("simple.json", File.ReadAllText("Json\\simple.json"), numberOfTimes, true);

            Helper.ConsoleWriteLine("");

            RunFTJBenchmark("TestMessage.json", File.ReadAllText("Json\\TestMessage.json"), numberOfTimes, false);
            RunFTJBenchmark("TestMessage.json", File.ReadAllText("Json\\TestMessage.json"), numberOfTimes, true);
            RunStandardJsonBenchmark("TestMessage.json", File.ReadAllText("Json\\TestMessage.json"), numberOfTimes, false);
            RunStandardJsonBenchmark("TestMessage.json", File.ReadAllText("Json\\TestMessage.json"), numberOfTimes, true);

            Helper.ConsoleWriteLine("");

            RunFTJBenchmark("PlayerAndAccounts.json", File.ReadAllText("Json\\PlayerAndAccounts.json"), numberOfTimes, false);
            RunFTJBenchmark("PlayerAndAccounts.json", File.ReadAllText("Json\\PlayerAndAccounts.json"), numberOfTimes, true);
            RunStandardJsonBenchmark("PlayerAndAccounts.json", File.ReadAllText("Json\\PlayerAndAccounts.json"), numberOfTimes, false);
            RunStandardJsonBenchmark("PlayerAndAccounts.json", File.ReadAllText("Json\\PlayerAndAccounts.json"), numberOfTimes, true);
        }

        private static void RunFTJBenchmark(string name, string json, int numberOfTimes, bool useCodegen) {
            TObject tObj = Helper.CreateJsonTemplateFromContent(name, json);
            RunFTJBenchmark(name, tObj, numberOfTimes, useCodegen);
        }

        private static void RunFTJBenchmark(string name, TObject tObj, int numberOfTimes, bool useCodegen) {
            return;
            // TODO: Rewrite FTJ serializer.
            /*
            byte[] ftj = null;
            int size = 0;
            TObject tObj;
            Json jsonInst;
            DateTime start;
            DateTime stop;

            TJson.UseCodegeneratedSerializer = false;

            tObj = CreateJsonTemplate(Path.GetFileNameWithoutExtension(name), json);
            jsonInst = (Json)tObj.CreateInstance();

            // using standard json serializer to populate object with values.
            jsonInst.PopulateFromJson(json);

            TJson.UseCodegeneratedSerializer = useCodegen;
            TJson.DontCreateSerializerInBackground = true;

            if (useCodegen) {
                Helper.ConsoleWrite(AddSpaces(name, COL1) + AddSpaces("FTJ(gen)", COL2));

                ftj = new byte[tObj.JsonSerializer.EstimateSizeBytes(jsonInst)];
                
                // Call serialize once to make sure that the codegenerated serializer is created.
                size = tObj.ToFasterThanJson(jsonInst, ftj, 0);
            } else {
                Helper.ConsoleWrite(AddSpaces(name, COL1) + AddSpaces("FTJ", COL2));
            }

            // Serializing to FTJ.
            start = DateTime.Now;
            for (int i = 0; i < numberOfTimes; i++) {
                ftj = new byte[tObj.JsonSerializer.EstimateSizeBytes(jsonInst)];

                // Call serialize once to make sure that the codegenerated serializer is created.
                size = tObj.ToFasterThanJson(jsonInst, ftj, 0);
            }
            stop = DateTime.Now;

            PrintResult(stop, start, numberOfTimes, COL3);

            // Deserializing from FTJ.
            unsafe {
                fixed (byte* p = ftj) {
                    start = DateTime.Now;
                    for (int i = 0; i < numberOfTimes; i++) {
                        jsonInst = (Json)tObj.CreateInstance();
                        size = tObj.PopulateFromFasterThanJson(jsonInst, (IntPtr)p, size);
                    }
                }
            }
            stop = DateTime.Now;

            PrintResult(stop, start, numberOfTimes, COL4);
            Helper.ConsoleWrite("\n");
            */
        }

        private static void RunStandardJsonBenchmark(string name, string json, int numberOfTimes, bool useCodegen) {
            TObject tObj = Helper.CreateJsonTemplateFromContent(name, json);
            RunStandardJsonBenchmark(name, tObj, numberOfTimes, useCodegen);
        }

        private static void RunStandardJsonBenchmark(string name, TObject tObj, int numberOfTimes, bool useCodegen) {
            byte[] jsonArr = null;
            int size = 0;
            Json jsonInst;
            DateTime start;
            DateTime stop;

            XSONModule.UseCodegeneratedSerializer = false;

            jsonInst = (Json)tObj.CreateInstance();

            XSONModule.UseCodegeneratedSerializer = useCodegen;
            XSONModule.DontCreateSerializerInBackground = true;

            if (useCodegen) {
                Helper.ConsoleWrite(AddSpaces(name, COL1) + AddSpaces("STD(gen)", COL2));

                // Call serialize once to make sure that the codegenerated serializer is created.
                jsonArr = jsonInst.ToJsonUtf8();
            } else {
                Helper.ConsoleWrite(AddSpaces(name, COL1) + AddSpaces("STD", COL2));
            }

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
