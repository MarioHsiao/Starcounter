using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace RapidMinds.BuildSystem.Common.Tasks
{
    public class RunUnitTests
    {

        #region Properties

        public string Executable { get; protected set; }
        public string TestContainer { get; protected set; }

        #endregion

        public RunUnitTests(string executable, string testContainer)
        {
            if (string.IsNullOrEmpty(executable)) throw new ArgumentException("Invalid executable", "executable");
            if (string.IsNullOrEmpty(testContainer)) throw new ArgumentException("Invalid solutionFile", "solutionFile");

            this.Executable = executable;
            this.TestContainer = testContainer;
        }

        public void Execute()
        {

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine();
            Console.Error.WriteLine("------ UnitTest started: Running unit tests ------");
            Console.ResetColor();


            DirectoryInfo directoryInfo = new DirectoryInfo(this.TestContainer);

            ExecuteCommandLineTool task = new ExecuteCommandLineTool();

            string resultsFile = Path.Combine( directoryInfo.Parent.FullName, "unittestresult.trx");

            if (File.Exists(resultsFile))
            {
                //// Remove readonly attribute
                FileInfo info = new FileInfo(resultsFile);
                info.Attributes &= ~FileAttributes.ReadOnly;

                File.Delete(resultsFile);
            }

            task.Execute(this.Executable, string.Format("/testcontainer:{0} /resultsfile:{1}", this.TestContainer, resultsFile));


            // TODO: Only if triggerd by teamcity
            Console.WriteLine("##teamcity[importData type='mstest' path='{0}']", resultsFile);

            // Time Elapsed 00:00:00.72
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.Error.WriteLine("UnitTest succeeded.");
            Console.ResetColor();

        }

 

    }

}
