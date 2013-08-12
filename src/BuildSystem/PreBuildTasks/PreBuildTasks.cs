using BuildSystemHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PreBuildTasks
{
    class PreBuildTasks
    {
        static Int32 Main(string[] args)
        {
            try
            {
                if (File.Exists(BuildSystem.BuildStatisticsFilePath))
                {
                    Console.WriteLine("Deleting existing build statistics file...");
                    File.Delete(BuildSystem.BuildStatisticsFilePath);
                }

                return 0;
            }
            catch (Exception generalException)
            {
                return BuildSystem.LogException(generalException);
            }
        }
    }
}
