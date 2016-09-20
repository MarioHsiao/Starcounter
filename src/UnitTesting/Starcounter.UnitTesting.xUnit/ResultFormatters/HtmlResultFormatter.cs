using System.IO;
using System.Text;
using Xunit.Abstractions;

namespace Starcounter.UnitTesting.xUnit.ResultFormatters
{
    // TODO: Execution time: total and per test

    internal class HtmlResultFormatter : IResultFormatter
    {
        readonly StreamWriter writer;
        int passedCount;
        int failedCount;
        int skippedCount;
        xUnitTestAssembly[] assemblies;
        StreamWriter header;
        StreamWriter failures;
        StreamWriter allTests;

        public HtmlResultFormatter(StreamWriter w)
        {
            writer = w;
            passedCount = failedCount = skippedCount = 0;
            header = new StreamWriter(new MemoryStream());
            failures = new StreamWriter(new MemoryStream());
            allTests = new StreamWriter(new MemoryStream());
        }

        void IResultFormatter.Open(xUnitTestAssembly[] assemblies)
        {
            this.assemblies = assemblies;
            failures.WriteLine("<h2>Failed tests</h2>");
            failures.WriteLine("<ul>");
            allTests.WriteLine("<h2>All tests</h2>");
            allTests.WriteLine("<ul>");
        }
        
        void IResultFormatter.TestFailed(xUnitTestAssembly assembly, ITestFailed failed)
        {
            failedCount++;

            var sb = new StringBuilder();
            sb.AppendLine("<li>");
            sb.AppendLine($"<div style=\"color: red\"><b>{failed.Test.DisplayName}</b></div>");
            for (int i = 0; i < failed.Messages.Length; i++)
            {
                var msg = failed.Messages[i];
                var stack = failed.StackTraces[i];

                sb.AppendLine($"<pre>{msg}</pre><pre>{stack}</pre>");
            }
            if (!string.IsNullOrEmpty(failed.Output))
            {
                sb.AppendLine($"<pre>{failed.Output}</pre>");
            }
            sb.AppendLine("</li>");

            var content = sb.ToString();
            
            failures.WriteLine(content);
            allTests.WriteLine(content);
        }

        void IResultFormatter.TestPassed(xUnitTestAssembly assembly, ITestPassed passed)
        {
            passedCount++;

            var sb = new StringBuilder();
            sb.AppendLine("<li>");
            sb.AppendLine($"<div style=\"color: green\"><b>{passed.Test.DisplayName}</b></div>");
            if (!string.IsNullOrEmpty(passed.Output)) {
                sb.AppendLine($"<pre>{passed.Output}</pre>");
            }
            sb.AppendLine("</li>");

            var content = sb.ToString();
            
            allTests.WriteLine(content);
        }

        void IResultFormatter.TestSkipped(xUnitTestAssembly assembly, ITestSkipped skipped)
        {
            skippedCount++;
        }
        
        void IResultFormatter.Close()
        {
            WriteHeader();
            CopyTestResultsToMainOutput();

            header.Close();
            failures.Close();
            allTests.Close();
        }

        void IResultFormatter.BeginAssembly(xUnitTestAssembly assembly)
        {
        }

        void IResultFormatter.EndAssembly(xUnitTestAssembly assembly)
        {

        }

        void WriteHeader()
        {
            header.WriteLine("<!DOCTYPE html><html><body>");
            header.WriteLine("<h3>Assemblies run</h3>");
            foreach (var a in assemblies)
            {
                header.WriteLine(a.AssemblyPath);
            }

            header.WriteLine("<h3>Summary</h3>");
            header.WriteLine($"Tests run: <b>{failedCount + passedCount}</b>, Failures: <b>{failedCount}</b>, Skipped: <b>{skippedCount}</b>");
        }

        void CopyTestResultsToMainOutput()
        {
            failures.WriteLine("</ul>");
            allTests.WriteLine("</ul>");
            header.WriteLine("</body></html>");

            failures.Flush();
            allTests.Flush();
            header.Flush();
            failures.BaseStream.Seek(0, SeekOrigin.Begin);
            allTests.BaseStream.Seek(0, SeekOrigin.Begin);
            header.BaseStream.Seek(0, SeekOrigin.Begin);

            header.BaseStream.CopyTo(writer.BaseStream);
            failures.BaseStream.CopyTo(writer.BaseStream);
            allTests.BaseStream.CopyTo(writer.BaseStream);
        } 
    }
}
