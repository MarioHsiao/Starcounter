using System;
using System.IO;
using NUnit.Framework;
using Starcounter.Templates;

#if !DEBUG // Serializer benchmark tests - only in release.

namespace Starcounter.Internal.XSON.Tests {
    [TestFixture]
    public class JsonSerializeBenchmarkTests {
        [Test]
        [Category("LongRunning"), Timeout(5 * 60000)] // timeout 5 minutes
        [Ignore("Requires fixing FTJ serializer")]
        public static void BenchmarkFTJSerializer() {
            int numberOfTimes = 1000000;

            Console.WriteLine("Benchmarking ftj serializer, repeats: " + numberOfTimes);
            Console.WriteLine(AddSpaces("File", 20) + AddSpaces("Type", 16) + AddSpaces("Serialize", 12) + "Deseralize");
            Console.WriteLine("----------------------------------------------------------");

            RunFTJBenchmark("jsstyle.json", File.ReadAllText("Input\\jsstyle.json"), numberOfTimes, false);
            RunFTJBenchmark("person.json", File.ReadAllText("Input\\person.json"), numberOfTimes, false);
            RunFTJBenchmark("supersimple.json", File.ReadAllText("Input\\supersimple.json"), numberOfTimes, false);
            RunFTJBenchmark("simple.json", File.ReadAllText("Input\\simple.json"), numberOfTimes, false);
            RunFTJBenchmark("TestMessage.json", File.ReadAllText("Input\\TestMessage.json"), numberOfTimes, false);
        }

        [Test]
        [Category("LongRunning"), Timeout(5 * 60000)] // timeout 5 minutes
        [Ignore("Requires fixing FTJ serializer")]
        public static void BenchmarkFTJCodegenSerializer() {
            int numberOfTimes = 1000000;

            Console.WriteLine("Benchmarking ftj serializer, repeats: " + numberOfTimes);
            Console.WriteLine(AddSpaces("File", 20) + AddSpaces("Type", 16) + AddSpaces("Serialize", 12) + "Deseralize");
            Console.WriteLine("----------------------------------------------------------");

            RunFTJBenchmark("jsstyle.json", File.ReadAllText("Input\\jsstyle.json"), numberOfTimes, true);
            RunFTJBenchmark("person.json", File.ReadAllText("Input\\person.json"), numberOfTimes, true);
            RunFTJBenchmark("supersimple.json", File.ReadAllText("Input\\supersimple.json"), numberOfTimes, true);
            RunFTJBenchmark("simple.json", File.ReadAllText("Input\\simple.json"), numberOfTimes, true);
            RunFTJBenchmark("TestMessage.json", File.ReadAllText("Input\\TestMessage.json"), numberOfTimes, true);
        }

        [Test]
        [Category("LongRunning"), Timeout(5 * 60000)] // timeout 5 minutes
        public static void BenchmarkStandardJsonSerializer() {
            int numberOfTimes = 1000000;

            Console.WriteLine("Benchmarking standard serializer, repeats: " + numberOfTimes);
            Console.WriteLine(AddSpaces("File", 20) + AddSpaces("Type", 16) + AddSpaces("Serialize", 12) + "Deseralize");
            Console.WriteLine("----------------------------------------------------------");

            RunStandardJsonBenchmark("jsstyle.json", File.ReadAllText("Input\\jsstyle.json"), numberOfTimes, false);
            RunStandardJsonBenchmark("person.json", File.ReadAllText("Input\\person.json"), numberOfTimes, false);
            RunStandardJsonBenchmark("supersimple.json", File.ReadAllText("Input\\supersimple.json"), numberOfTimes, false);
            RunStandardJsonBenchmark("simple.json", File.ReadAllText("Input\\simple.json"), numberOfTimes, false);
            RunStandardJsonBenchmark("TestMessage.json", File.ReadAllText("Input\\TestMessage.json"), numberOfTimes, false);
        }

        [Test]
        [Category("LongRunning"), Timeout(5 * 60000)] // timeout 5 minutes
        public static void BenchmarkStandardCodegenJsonSerializer() {
            int numberOfTimes = 1000000;

            Console.WriteLine("Benchmarking standard serializer, repeats: " + numberOfTimes);
            Console.WriteLine(AddSpaces("File", 20) + AddSpaces("Type", 16) + AddSpaces("Serialize", 12) + "Deseralize");
            Console.WriteLine("----------------------------------------------------------");

            RunStandardJsonBenchmark("jsstyle.json", File.ReadAllText("Input\\jsstyle.json"), numberOfTimes, true);
            RunStandardJsonBenchmark("person.json", File.ReadAllText("Input\\person.json"), numberOfTimes, true);
            RunStandardJsonBenchmark("supersimple.json", File.ReadAllText("Input\\supersimple.json"), numberOfTimes, true);
            RunStandardJsonBenchmark("simple.json", File.ReadAllText("Input\\simple.json"), numberOfTimes, true);
            RunStandardJsonBenchmark("TestMessage.json", File.ReadAllText("Input\\TestMessage.json"), numberOfTimes, true);
        }

        [Test]
        [Category("LongRunning"), Timeout(5 * 60000)] // timeout 5 minutes
        public static void BenchmarkAllSerializers() {
            int numberOfTimes = 1000000;

            Console.WriteLine("Benchmarking serializers, repeats: " + numberOfTimes);
            Console.WriteLine(AddSpaces("File", 20) + AddSpaces("Type", 16) + AddSpaces("Serialize", 12) + "Deseralize");
            Console.WriteLine("----------------------------------------------------------");

            RunFTJBenchmark("jsstyle.json", File.ReadAllText("Input\\jsstyle.json"), numberOfTimes, false);
            RunFTJBenchmark("jsstyle.json", File.ReadAllText("Input\\jsstyle.json"), numberOfTimes, true);
            RunStandardJsonBenchmark("jsstyle.json", File.ReadAllText("Input\\jsstyle.json"), numberOfTimes, false);
            RunStandardJsonBenchmark("jsstyle.json", File.ReadAllText("Input\\jsstyle.json"), numberOfTimes, true);

            Console.WriteLine();

            RunFTJBenchmark("person.json", File.ReadAllText("Input\\person.json"), numberOfTimes, false);
            RunFTJBenchmark("person.json", File.ReadAllText("Input\\person.json"), numberOfTimes, true);
            RunStandardJsonBenchmark("person.json", File.ReadAllText("Input\\person.json"), numberOfTimes, false);
            RunStandardJsonBenchmark("person.json", File.ReadAllText("Input\\person.json"), numberOfTimes, true);

            Console.WriteLine();

            RunFTJBenchmark("supersimple.json", File.ReadAllText("Input\\supersimple.json"), numberOfTimes, false);
            RunFTJBenchmark("supersimple.json", File.ReadAllText("Input\\supersimple.json"), numberOfTimes, true);
            RunStandardJsonBenchmark("supersimple.json", File.ReadAllText("Input\\supersimple.json"), numberOfTimes, false);
            RunStandardJsonBenchmark("supersimple.json", File.ReadAllText("Input\\supersimple.json"), numberOfTimes, true);

            Console.WriteLine();

            RunFTJBenchmark("simple.json", File.ReadAllText("Input\\simple.json"), numberOfTimes, false);
            RunFTJBenchmark("simple.json", File.ReadAllText("Input\\simple.json"), numberOfTimes, true);
            RunStandardJsonBenchmark("simple.json", File.ReadAllText("Input\\simple.json"), numberOfTimes, false);
            RunStandardJsonBenchmark("simple.json", File.ReadAllText("Input\\simple.json"), numberOfTimes, true);

            Console.WriteLine();

            RunFTJBenchmark("TestMessage.json", File.ReadAllText("Input\\TestMessage.json"), numberOfTimes, false);
            RunFTJBenchmark("TestMessage.json", File.ReadAllText("Input\\TestMessage.json"), numberOfTimes, true);
            RunStandardJsonBenchmark("TestMessage.json", File.ReadAllText("Input\\TestMessage.json"), numberOfTimes, false);
            RunStandardJsonBenchmark("TestMessage.json", File.ReadAllText("Input\\TestMessage.json"), numberOfTimes, true);
        }

        private static void RunFTJBenchmark(string name, string json, int numberOfTimes, bool useCodegen) {
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
                Console.Write(AddSpaces(name, 20) + AddSpaces("FTJ-Codegen", 16));

                ftj = new byte[tObj.JsonSerializer.EstimateSizeBytes(jsonInst)];
                
                // Call serialize once to make sure that the codegenerated serializer is created.
                size = tObj.ToFasterThanJson(jsonInst, ftj, 0);
            } else {
                Console.Write(AddSpaces(name, 20) + AddSpaces("FTJ", 16));
            }

            // Serializing to FTJ.
            start = DateTime.Now;
            for (int i = 0; i < numberOfTimes; i++) {
                ftj = new byte[tObj.JsonSerializer.EstimateSizeBytes(jsonInst)];

                // Call serialize once to make sure that the codegenerated serializer is created.
                size = tObj.ToFasterThanJson(jsonInst, ftj, 0);
            }
            stop = DateTime.Now;

            PrintResult(stop, start, numberOfTimes, 12);

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

            PrintResult(stop, start, numberOfTimes, 0);
            Console.Write("\n");
            */
        }

        private static void RunStandardJsonBenchmark(string name, string json, int numberOfTimes, bool useCodegen) {
            byte[] jsonArr = null;
            int size = 0;
            TObject tObj;
            Json jsonInst;
            DateTime start;
            DateTime stop;

            TObject.UseCodegeneratedSerializer = false;

            tObj = Helper.CreateJsonTemplateFromContent(name, json);
            jsonInst = (Json)tObj.CreateInstance();

            // using standard json serializer to populate object with values.
            jsonInst.PopulateFromJson(json);

            TObject.UseCodegeneratedSerializer = useCodegen;
            TObject.DontCreateSerializerInBackground = true;

            if (useCodegen) {
                Console.Write(AddSpaces(name, 20) + AddSpaces("STD-Codegen", 16));

                // Call serialize once to make sure that the codegenerated serializer is created.
                jsonArr = jsonInst.ToJsonUtf8();
            } else {
                Console.Write(AddSpaces(name, 20) + AddSpaces("STD", 16));
            }

            // Serializing to standard json.
            start = DateTime.Now;
            for (int i = 0; i < numberOfTimes; i++) {

                jsonArr = new byte[tObj.JsonSerializer.EstimateSizeBytes(jsonInst)];

                // Call serialize once to make sure that the codegenerated serializer is created.
                size = tObj.ToJsonUtf8(jsonInst, jsonArr, 0);
            }
            stop = DateTime.Now;
            PrintResult(stop, start, numberOfTimes, 12);

            // Deserializing from standard json.
            unsafe {
                fixed (byte* p = jsonArr) {
                    start = DateTime.Now;
                    for (int i = 0; i < numberOfTimes; i++) {
                        size = tObj.PopulateFromJson(jsonInst, (IntPtr)p, size);
                    }
                }
            }
            stop = DateTime.Now;
            PrintResult(stop, start, numberOfTimes, 0);
            Console.Write("\n");
        }

        private static string AddSpaces(string str, int totalLength) {
            string after = str;
            for (int i = 0; i < (totalLength - str.Length); i++) {
                after += " ";
            }
            return after;
        }

        private static void PrintResult(DateTime stop, DateTime start, int numberOfTimes, int space) {
            var tms = (stop - start).TotalMilliseconds;
            var kps = numberOfTimes / tms;

            string str = AddSpaces(kps.ToString(".00") + " k/s", space);

            Console.Write(AddSpaces(kps.ToString(".00") + " k/s", space));
        }
    }
}

#endif // Serializer benchmark tests - only in release.
