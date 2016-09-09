
using Xunit.Runners;

namespace Starcounter.UnitTesting.xUnit
{
    public class HostedAssemblyTestRunner
    {
        readonly string assemblyPath;

        public HostedAssemblyTestRunner(string path)
        {
            assemblyPath = path;
        }

        public void Run()
        {
            var runner = AssemblyRunner.WithoutAppDomain(assemblyPath);
            runner.Start();
        }

        void WaitForRunnerCompletion(AssemblyRunner runner)
        {
            while (runner.Status != AssemblyRunnerStatus.Idle)
            {
                System.Threading.Thread.Sleep(200);
            }
        }
    }
}