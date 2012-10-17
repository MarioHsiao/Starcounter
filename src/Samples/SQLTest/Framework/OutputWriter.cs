using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SQLTest
{
    public enum FileType
    {
        Output, Debug, Input
    }

    public static class OutputWriter
    {
        public static void WriteOutputToFile(String filePath, List<TestQuery> queryList, FileType fileType)
        {
            //Transaction transaction = null;
            StreamWriter writer = null;

            try
            {
                //transaction = Transaction.NewCurrent();
                writer = new StreamWriter(filePath, false, Encoding.Unicode);
                writer.WriteLine("// THIS FILE WAS AUTO-GENERATED [" + DateTime.Now + "]. DO NOT EDIT!");

                switch (fileType)
                {
                    case FileType.Output:
                        Int32 errorFound = 0;
                        for (Int32 i = 0; i < queryList.Count; i++)
                        {
                            if (!queryList[i].CorrectResult)
                            {
                                writer.WriteLine(queryList[i].ToString(fileType));
                                errorFound++;
                            }
                        }
                        if (errorFound == 0)
                            writer.WriteLine("// TEST SUCCEEDED! [" + queryList.Count + " queries]");
                        else writer.WriteLine("// TEST FAILED! [" + errorFound + " failed of " + queryList.Count + " queries]");
                        break;

                    case FileType.Debug:
                        for (Int32 i = 0; i < queryList.Count; i++)
                        {
                            writer.WriteLine(queryList[i].ToString(fileType));
                        }
                        break;

                    case FileType.Input:
                        for (Int32 i = 0; i < queryList.Count; i++)
                        {
                            writer.WriteLine(queryList[i].ToString(fileType));
                        }
                        break;
                }
                writer.WriteLine("// END");
            }
            finally
            {
                //if (transaction != null)
                //    transaction.Dispose();

                if (writer != null)
                    writer.Close();
            }
        }

        private static String CreateResultString(List<String> resultList, Boolean shouldBeReordered)
        {
            if (shouldBeReordered)
            {
                resultList.Sort(StringComparer.InvariantCultureIgnoreCase);
            }
            StringBuilder stringBuilder = new StringBuilder();
            for (Int32 i = 0; i < resultList.Count; i++)
            {
                stringBuilder.AppendLine(resultList[i]);
            }
            return stringBuilder.ToString();
        }
    }
}
