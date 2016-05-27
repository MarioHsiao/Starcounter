using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Starcounter;
using System.Linq;

namespace SQLTest
{
    public static class InputReader
    {
        public static List<TestQuery> ReadQueryListFromFile(String filePath, Boolean startedOnClient, out Int32 counter)
        {
            int localCounter = 0;

            StreamReader reader = null;
            List<TestQuery> queryList = new List<TestQuery>();

            String line = null;
            StringBuilder stringBuilder = null;
            TestQuery testQuery = null;
            Boolean withinCommentBlock = false;

            try
            {
                Db.Transact(delegate
                {
                    reader = new StreamReader(filePath, Encoding.Unicode);

                    // Stop at end-of-file.
                    while (!reader.EndOfStream)
                    {
                        line = reader.ReadLine().Trim();

                        // Do not consider blocks starting with "/*..." and ending with "...*/".
                        if (line.EndsWith("*/"))
                        {
                            withinCommentBlock = false;
                            continue;
                        }
                        if (line.StartsWith("/*"))
                        {
                            withinCommentBlock = true;
                            continue;
                        }
                        if (withinCommentBlock)
                            continue;

                        // Do not consider lines with comments.
                        if (line.StartsWith("//"))
                            continue;

                        if (line == "<END>")
                            break;

                        if (line == "<NEXT>")
                        {
                            AddTestQuery(queryList, testQuery, startedOnClient);
                            localCounter++;
                            testQuery = new TestQuery();
                            continue;
                        }

                        if (line.StartsWith("Description:"))
                        {
                            testQuery.Description = line.Substring(12).Trim();
                            continue;
                        }

                        if (line.StartsWith("QueryString:"))
                        {
                            testQuery.QueryString = line.Substring(12).Trim();
                            continue;
                        }

                        if (line.StartsWith("VariableValues:"))
                        {
                            testQuery.VariableValuesArr = ParseVariableValues(line.Substring(15).Trim());
                            continue;
                        }

                        if (line.StartsWith("SingleObjectProjection:"))
                        {
                            testQuery.SingleObjectProjection = Boolean.Parse(line.Substring(23).Trim());
                            continue;
                        }

                        if (line.StartsWith("SingleObjectPathProjection:"))
                        {
                            testQuery.SingleObjectPathProjection = Boolean.Parse(line.Substring(27).Trim());
                            continue;
                        }

                        if (line.StartsWith("IncludesLiteral:"))
                        {
                            testQuery.IncludesLiteral = Boolean.Parse(line.Substring(16).Trim());
                            continue;
                        }

                        if (line.StartsWith("IncludesObjectValue:"))
                        {
                            testQuery.IncludesObjectValue = Boolean.Parse(line.Substring(20).Trim());
                            continue;
                        }

                        if (line.StartsWith("DataManipulation:"))
                        {
                            testQuery.DataManipulation = Boolean.Parse(line.Substring(17).Trim());
                            continue;
                        }

                        if (line.StartsWith("ShouldBeReordered:"))
                        {
                            testQuery.ShouldBeReordered = Boolean.Parse(line.Substring(18).Trim());
                            continue;
                        }

                        if (line.StartsWith("ShouldBeReorderedPartially:"))
                        {
                            var range = line.Substring(27).Split('-').Select(s => int.Parse(s.Trim()));

                            testQuery.ResultReorderIndexes = Tuple.Create( range.First(), range.Skip(1).First() );
                            testQuery.ShouldBeReordered = false;
                            continue;
                        }

                        if (line.StartsWith("UseBisonParser:")) {
                            testQuery.UseBisonParser = Boolean.Parse(line.Substring(15).Trim());
                            continue;
                        }

                        if (line.StartsWith("ExpectedExceptionMessage:"))
                        {
                            testQuery.ExpectedExceptionMessage = line.Substring(25).Trim();
                            continue;
                        }

                        if (line == "ExpectedExecutionPlan:")
                        {
                            stringBuilder = new StringBuilder();
                            while (!reader.EndOfStream && line != "")
                            {
                                line = reader.ReadLine();
                                if (line != "")
                                    stringBuilder.AppendLine(line);
                            }
                            testQuery.ExpectedExecutionPlan = stringBuilder.ToString();
                            continue;
                        }

                        if ((line == "ExpectedResult:") || (line.StartsWith("ExpectedPartialResult:")))
                        {
                            if ( line.StartsWith("ExpectedPartialResult:"))
                            {
                                testQuery.NumberOfPartialResultsExpected = int.Parse(line.Substring(22).Trim());
                            }

                            stringBuilder = new StringBuilder();
                            while (!reader.EndOfStream && line != "")
                            {
                                line = reader.ReadLine();
                                if (line != "")
                                    stringBuilder.AppendLine(line);
                            }
                            testQuery.ExpectedResult = stringBuilder.ToString();
                            continue;
                        }
                    }
                });
            }
            finally
            {
                if (reader != null)
                    reader.Close();
            }
            counter = localCounter;
            AddTestQuery(queryList, testQuery, startedOnClient);
            return queryList;
        }

        private static void AddTestQuery(List<TestQuery> queryList, TestQuery testQuery, Boolean clientSide)
        {
            if (testQuery == null)
                return;

            if (clientSide && testQuery.IncludesObjectValue)
                return;

            if (clientSide && !testQuery.SingleObjectProjection)
                return;

            if (clientSide && testQuery.SingleObjectPathProjection)
                return;

            queryList.Add(testQuery);
            return;
        }

        private static Object[] ParseVariableValues(String valueString)
        {
            Int32 index1 = -1;
            Int32 index2 = -1;
            String type = null;
            String strValue = null;
            List<Object> valueList = new List<Object>();

            index1 = valueString.IndexOf(':');
            index2 = valueString.IndexOf(';');
            while (index1 > 0 && index2 > 0)
            {
                type = valueString.Substring(0, index1).Trim();
                strValue = valueString.Substring(index1 + 1, index2 - index1 - 1).Trim();

                switch (type)
                {
                    case "Binary":
                        valueList.Add(Utilities.HexToBinary(strValue));
                        break;

                    case "Boolean":
                        valueList.Add(Boolean.Parse(strValue));
                        break;

                    case "Byte":
                        valueList.Add(Byte.Parse(strValue, NumberFormatInfo.InvariantInfo));
                        break;

                    case "DateTime":
                        valueList.Add(DateTime.Parse(strValue, DateTimeFormatInfo.InvariantInfo));
                        break;

                    case "Decimal":
                        valueList.Add(Decimal.Parse(strValue, NumberFormatInfo.InvariantInfo));
                        break;

                    case "Double":
                        valueList.Add(Double.Parse(strValue, NumberFormatInfo.InvariantInfo));
                        break;

                    case "Int16":
                        valueList.Add(Int16.Parse(strValue, NumberFormatInfo.InvariantInfo));
                        break;

                    case "Int32":
                        valueList.Add(Int32.Parse(strValue, NumberFormatInfo.InvariantInfo));
                        break;

                    case "Int64":
                        valueList.Add(Int64.Parse(strValue, NumberFormatInfo.InvariantInfo));
                        break;

                    case "Object":
                        //valueList.Add(DbHelper.FromIDString(strValue));
                        valueList.Add(Utilities.GetObject(strValue));
                        break;

                    case "SByte":
                        valueList.Add(SByte.Parse(strValue, NumberFormatInfo.InvariantInfo));
                        break;

                    case "Single":
                        valueList.Add(Single.Parse(strValue, NumberFormatInfo.InvariantInfo));
                        break;

                    case "String":
                        valueList.Add(strValue);
                        break;

                    case "UInt16":
                        valueList.Add(UInt16.Parse(strValue, NumberFormatInfo.InvariantInfo));
                        break;

                    case "UInt32":
                        valueList.Add(UInt32.Parse(strValue, NumberFormatInfo.InvariantInfo));
                        break;

                    case "UInt64":
                        valueList.Add(UInt64.Parse(strValue, NumberFormatInfo.InvariantInfo));
                        break;

                    case "Type":
                        valueList.Add(Type.GetType(strValue));
                        break;

                    default:
                        throw new Exception("Incorrect type.");
                }

                if (index2 + 2 >= valueString.Length)
                    valueString = "";
                else
                    valueString = valueString.Substring(index2 + 2);

                index1 = valueString.IndexOf(':');
                index2 = valueString.IndexOf(';');
            }

            return valueList.ToArray();
        }
    }
}
