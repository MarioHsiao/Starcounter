
using System;
using System.IO;

namespace Starcounter.UnitTesting.Runtime
{
    internal static class DateTimeResultFormatExtensions
    {
        public static string ToResultFormat(this DateTime cargo)
        {
            // Central spot to format timings. Decide what to use.
            // TODO:
            return cargo.ToString();
        }
    }

    internal class ResultWriter
    {
        readonly string fileNameBase;
        readonly string outputPath;
        const string fileExtension = ".testresult.txt";
        readonly StreamWriter writer;
        int hostId = 0;

        public ResultWriter(ResultHeader header, string path = null)
        {
            fileNameBase = $"{header.Database}-{header.UniqueId}";

            var fileName = $"{fileNameBase}{fileExtension}";
            path = path ?? Path.GetTempPath();
            outputPath = path;

            writer = File.CreateText(Path.Combine(outputPath, fileName));

            WriteHeader(header);
        }

        public HostResultWriter OpenHostWriter(TestHost host)
        {
            var resultFile = $"{fileNameBase}-{++hostId}{fileExtension}";
            return new HostResultWriter(host, Path.Combine(outputPath, resultFile));
        }

        public void CloseHostWriterWithResult(HostResultWriter hostWriter, TestResult result)
        {
            WriteHostResult(hostWriter, result);
            hostWriter.Close();
        }

        public void Close(ResultFooter footer)
        {
            WriteFooter(footer);
            writer.Flush();
            writer.Close();
        }

        void WriteHeader(ResultHeader header)
        {
            writer.WriteLine($"Root:{Property("Db", header.Database)}{Property("Id", header.UniqueId)}{Property("Time", header.Start.ToResultFormat())}");
        }

        void WriteHostResult(HostResultWriter hw, TestResult result)
        {
            var resultSummary = $"{Property("Start", result.Started.ToResultFormat())}";
            resultSummary += $"{Property("Finished", result.Finished.ToResultFormat())}";
            resultSummary += $"{Property("Failed", result.TestsFailed.ToString())}";
            resultSummary += $"{Property("Succeeded", result.TestsSucceeded.ToString())}";
            resultSummary += $"{Property("Skipped", result.TestsSkipped.ToString())}";

            writer.WriteLine($"Host:{Property("Type", hw.Host.GetType().ToString())}{Property("File", hw.ResultFile)}{Property("Result", resultSummary)}");
        }

        void WriteFooter(ResultFooter footer)
        {
            writer.WriteLine($"Footer:{Property("Ended", footer.Ended.ToResultFormat())}");
        }

        string Property(string key, string value)
        {
            return $"{key}={value};";
        }
    }
}