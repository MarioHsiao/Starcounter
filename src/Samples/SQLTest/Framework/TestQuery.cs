using System;
using System.Text;
using System.Text.RegularExpressions;
using Starcounter;

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
        internal Boolean Evaluated;
        internal Boolean CorrectResult;

        // First execution (not using cache).
        internal String ActualExecutionPlan1;
        internal String ActualResult1;
        internal String ActualExceptionMessage1;
        internal String ActualFullException1;

        // Second execution (using cache).
        internal String ActualExecutionPlan2;
        internal String ActualResult2;
        internal String ActualExceptionMessage2;
        internal String ActualFullException2;

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

        private void AppendVariableValues(StringBuilder stringBuilder)
        {
            stringBuilder.Append("VariableValues: ");

            if (VariableValuesArr != null)
            {
                String type = null;
                String value = null;
                for (Int32 i = 0; i < VariableValuesArr.Length; i++)
                {
                    if (VariableValuesArr[i] is Entity)
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
