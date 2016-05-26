using System;
using System.Text;
using System.Text.RegularExpressions;
using Starcounter;
using System.Linq;
using System.Collections.Generic;

namespace SQLTest
{
    public class TestQuery
    {
        internal String ErrorMessage;
        internal String Description;
        internal String QueryString;
        internal Object[] VariableValuesArr;
        internal Boolean DataManipulation; // DELETE statement?
        internal String ExpectedExecutionPlan;
        internal String ExpectedResult;
        internal String ExpectedExceptionMessage;
        internal Boolean SingleObjectProjection;
        internal Boolean SingleObjectPathProjection;
        internal Boolean IncludesLiteral;
        internal Boolean IncludesObjectValue;
        internal Boolean ShouldBeReordered;
        internal Boolean UseBisonParser;
        internal Boolean Evaluated;
        internal Boolean CorrectResult;
        internal int NumberOfPartialResultsExpected;
        internal Tuple<int, int> ResultReorderIndexes;


        // First execution (not using cache).
        internal String ActualExecutionPlan1;
        internal String ActualResult1;
        internal String ActualExceptionMessage1;
        internal String ActualFullException1;
        internal Boolean ActualUseBisonParser1;

        // Second execution (using cache).
        internal String ActualExecutionPlan2;
        internal String ActualResult2;
        internal String ActualExceptionMessage2;
        internal String ActualFullException2;
        internal Boolean ActualUseBisonParser2;

        internal TestQuery()
        {
            ErrorMessage = "";
            Description = "";
            QueryString = "";
            VariableValuesArr = null;
            DataManipulation = false;
            ExpectedExecutionPlan = "";
            ExpectedResult = "";
            ExpectedExceptionMessage = "";
            SingleObjectProjection = true;
            SingleObjectPathProjection = false;
            IncludesLiteral = false;
            IncludesObjectValue = false;
            ShouldBeReordered = true;
            UseBisonParser = false;
            Evaluated = false;
            CorrectResult = true;
            ActualExecutionPlan1 = "";
            ActualResult1 = "";
            ActualExceptionMessage1 = "";
            ActualFullException1 = "";
            ActualExecutionPlan2 = "";
            ActualResult2 = "";
            ActualExceptionMessage2 = "";
            ActualFullException2 = "";
        }

        /// <summary>
        /// Returns true if two strings are equal.
        /// </summary>
        public static Boolean EqualStringsIgnoreWhiteSpace(String string1, String string2)
        {
            String normalized1 = Regex.Replace(string1, "\\s", "");
            String normalized2 = Regex.Replace(string2, "\\s", "");

            return String.Equals(normalized1, normalized2);
        }

        public Boolean EvaluateResult(Boolean startedOnClient)
        {
            CorrectResult = true;
            ErrorMessage = "";

            if (UseBisonParser != ActualUseBisonParser1) {
                CorrectResult = false;
                ErrorMessage += "Incorrect parser used (first execution). ";
            }

            if (UseBisonParser != ActualUseBisonParser2) {
                CorrectResult = false;
                ErrorMessage += "Incorrect parser used (second execution). ";
            }

            if (!EqualStringsIgnoreWhiteSpace(ExpectedExecutionPlan, ActualExecutionPlan1))
            {
                CorrectResult = false;
                ErrorMessage += "Incorrect execution plan (first execution). ";
            }

            if (!EqualStringsIgnoreWhiteSpace(ExpectedExecutionPlan, ActualExecutionPlan2))
            {
                CorrectResult = false;
                ErrorMessage += "Incorrect execution plan (second execution). ";
            }

            if (NumberOfPartialResultsExpected == 0)
            {

                if (ExpectedResult != ActualResult1)
                {
                    CorrectResult = false;
                    ErrorMessage += "Incorrect result (first execution). ";
                }

                if (ExpectedResult != ActualResult2)
                {
                    CorrectResult = false;
                    ErrorMessage += "Incorrect result (second execution). ";
                }
            }
            else
            {
                if ( !TestPartialResult(ExpectedResult, ActualResult1, NumberOfPartialResultsExpected) )
                {
                    CorrectResult = false;
                    ErrorMessage += "Incorrect result (first execution). ";
                }

                if (!TestPartialResult(ExpectedResult, ActualResult2, NumberOfPartialResultsExpected))
                {
                    CorrectResult = false;
                    ErrorMessage += "Incorrect result (second execution). ";
                }
            }


            if (!startedOnClient && ExpectedExceptionMessage != ActualExceptionMessage1) {
                CorrectResult = false;
                ErrorMessage += "Incorrect exception (first execution). ";
            }
            if (!startedOnClient && ExpectedExceptionMessage != ActualExceptionMessage2) {
                CorrectResult = false;
                ErrorMessage += "Incorrect exception (second execution). ";
            }

            Evaluated = true;

            return CorrectResult;
        }

        private static IEnumerable<string> cut_header_from_result(string result)
        {
            return result.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Skip(1);
        }

        private static bool is_subsequence<T>(IEnumerable<T> where, IEnumerable<T> what)
        {
            if (!what.Any())
                return true;

            if (!where.Any())
                return false;

            T first = what.First();
            var cmp = Comparer<T>.Default;

            var where_started_from_what = where.SkipWhile(t => cmp.Compare(t, first) != 0);

            if (!where_started_from_what.Any())
                return false;

            return is_subsequence(where_started_from_what.Skip(1), what.Skip(1));
        }

        private static bool TestPartialResult(string expectedResult, string actualResult, int numberOfPartialResultsExpected)
        {
            IEnumerable<string> expected_no_header = cut_header_from_result(expectedResult);
            IEnumerable<string> actual_no_header = cut_header_from_result(actualResult);

            return (actual_no_header.Count() == numberOfPartialResultsExpected) && (is_subsequence(expected_no_header, actual_no_header));
        }

        private void AppendVariableValues(StringBuilder stringBuilder)
        {
            stringBuilder.Append("VariableValues: ");

            if (VariableValuesArr != null)
            {
                String type = null;
                String value = null;
                for (Int32 i = 0; i < VariableValuesArr.Length; i++)
                {
                    if (VariableValuesArr[i] is IObjectView)
                    {
                        type = "Object";
                        //value = DbHelper.GetObjectIDString(VariableValuesArr[i] as Entity);
                        value = Utilities.GetObjectIdString(VariableValuesArr[i] as IObjectView);
                    }
                    else if (VariableValuesArr[i] is Binary)
                    {
                        type = "Binary";
                        value = Utilities.BinaryToHex((Binary)VariableValuesArr[i]);
                    } else if (VariableValuesArr[i] is Type) {
                        type = "Type";
                        value = VariableValuesArr[i].ToString();
                    } else if (VariableValuesArr[i] != null)
                    {
                        type = VariableValuesArr[i].GetType().Name;
                        value = VariableValuesArr[i].ToString();
                    }
                    else
                    {
                        type = Db.NullString;
                        value = Db.NullString;
                    }
                    stringBuilder.Append(type + ":" + value + "; ");
                }
            }

            stringBuilder.AppendLine();
        }

        public String ToString(FileType fileType)
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.AppendLine("<NEXT>");
            if (fileType != FileType.Input)
            {
                stringBuilder.Append("ErrorMessage: ");
                stringBuilder.AppendLine(ErrorMessage);
            }
            stringBuilder.Append("Description: ");
            stringBuilder.AppendLine(Description);
            stringBuilder.Append("QueryString: ");
            stringBuilder.AppendLine(QueryString);
            AppendVariableValues(stringBuilder);
            stringBuilder.Append("DataManipulation: ");
            stringBuilder.AppendLine(DataManipulation.ToString());
            stringBuilder.Append("SingleObjectProjection: ");
            stringBuilder.AppendLine(SingleObjectProjection.ToString());
            stringBuilder.Append("SingleObjectPathProjection: ");
            stringBuilder.AppendLine(SingleObjectPathProjection.ToString());
            stringBuilder.Append("IncludesLiteral: ");
            stringBuilder.AppendLine(IncludesLiteral.ToString());
            stringBuilder.Append("IncludesObjectValue: ");
            stringBuilder.AppendLine(IncludesObjectValue.ToString());
            stringBuilder.Append("ShouldBeReordered: ");
            stringBuilder.AppendLine(ShouldBeReordered.ToString());
            stringBuilder.Append("UseBisonParser: ");
            stringBuilder.AppendLine(UseBisonParser.ToString());
            if (fileType != FileType.Input) {
                stringBuilder.Append("ActualUseBisonParser1 ");
                stringBuilder.AppendLine(ActualUseBisonParser1.ToString());
                stringBuilder.Append("ActualUseBisonParser2 ");
                stringBuilder.AppendLine(ActualUseBisonParser2.ToString());
            }
            stringBuilder.Append("ExpectedExceptionMessage: ");
            if (fileType != FileType.Input)
            {
                stringBuilder.AppendLine(ExpectedExceptionMessage);
                stringBuilder.Append("ActualExceptionMessage1: ");
                stringBuilder.AppendLine(ActualExceptionMessage1);
                stringBuilder.Append("ActualExceptionMessage2: ");
            }
            stringBuilder.AppendLine(ActualExceptionMessage2);
            if (fileType != FileType.Input)
            {
                stringBuilder.AppendLine("ActualFullException1: ");
                stringBuilder.AppendLine(ActualFullException1);
                stringBuilder.AppendLine("ActualFullException2: ");
                stringBuilder.AppendLine(ActualFullException2);
            }
            stringBuilder.AppendLine("ExpectedExecutionPlan: ");
            if (fileType != FileType.Input)
            {
                stringBuilder.AppendLine(ExpectedExecutionPlan);
                stringBuilder.AppendLine("ActualExecutionPlan1: ");
                stringBuilder.AppendLine(ActualExecutionPlan1);
                stringBuilder.AppendLine("ActualExecutionPlan2: ");
            }
            stringBuilder.AppendLine(ActualExecutionPlan2);
            stringBuilder.AppendLine("ExpectedResult: ");
            if (fileType != FileType.Input)
            {
                stringBuilder.AppendLine(ExpectedResult);
                stringBuilder.AppendLine("ActualResult1: ");
                stringBuilder.AppendLine(ActualResult1);
                stringBuilder.AppendLine("ActualResult2: ");
            }
            stringBuilder.AppendLine(ActualResult2);

            return stringBuilder.ToString();
        }
    }
}
