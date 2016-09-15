using System.IO;
using Xunit.Abstractions;

namespace Starcounter.UnitTesting.xUnit
{
    internal class xUnitTestAssemblyRunner : IMessageSink
    {
        readonly xUnitTestAssembly assembly;
        readonly StreamWriter resultWriter;
        readonly TestResult result;

        public xUnitTestAssemblyRunner(xUnitTestAssembly a, TestResult r, StreamWriter writer)
        {
            assembly = a;
            result = r;
            resultWriter = writer;
        }
        
        public void Run()
        {
            var header = $"--{assembly.AssemblyPath}";
            resultWriter.WriteLine(header);

            if (assembly.ContainTests)
            {
                var front = assembly.Front;
                var tests = assembly.TestCases;

                front.RunTests(tests, this, null);
            }
            else
            {
                resultWriter.WriteLine("-- No tests defined");
            }
            
            resultWriter.WriteLine("--");
        }
        
        void React(ITestStarting test)
        {
            if (test != null)
            {
                resultWriter.WriteLine($"Starting-{test.Test.DisplayName}");
            }
        }

        void React(ITestFinished test)
        {
            if (test != null)
            {
                resultWriter.WriteLine($"Finished-{test.Test.DisplayName}");
            }
        }

        void React(ITestPassed test)
        {
            if (test != null)
            {
                result.TestsSucceeded++;
                resultWriter.WriteLine($"Passed-{test.Test.DisplayName}");
            }
        }

        void React(ITestFailed test)
        {
            if (test != null)
            {
                result.TestsFailed++;
                resultWriter.WriteLine($"Failed-{test.Test.DisplayName}");
            }
        }

        void React(ITestSkipped test)
        {
            if (test != null)
            {
                result.TestsSkipped++;
                resultWriter.WriteLine($"Skipped-{test.Test.DisplayName}:{test.Reason}");
            }
        }

        bool IMessageSink.OnMessage(IMessageSinkMessage message)
        {
            React(message as ITestStarting);
            React(message as ITestFinished);
            React(message as ITestPassed);
            React(message as ITestFailed);
            React(message as ITestSkipped);
            
            // Continue running tests
            return true;
        }
    }
}