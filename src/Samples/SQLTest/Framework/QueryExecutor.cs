using System;
using Starcounter;
using System.Collections.Generic;
using System.Text;
using Starcounter.Binding;

namespace SQLTest
{
    public static class QueryExecutor
    {
        const String headerFieldSeparator = " ! ";

        public static void ResultExecuteQueries(List<TestQuery> queryList, Boolean firstExecution)
        {
            Db.Transaction(delegate
            {
                List<String> resultList = null;
                SqlEnumerator<dynamic> sqlEnum = null;

                for (Int32 i = 0; i < queryList.Count; i++)
                {
                    try
                    {
                        // If DataManipulation then there is no result enumerator.
                        if (queryList[i].DataManipulation)
                        {
                            if (queryList[i].VariableValuesArr != null && queryList[i].VariableValuesArr.Length > 0)
                            {
                                Db.SlowSQL(queryList[i].QueryString, queryList[i].VariableValuesArr);
                                continue;
                            }
                            else
                            {
                                Db.SlowSQL(queryList[i].QueryString);
                                continue;
                            }
                        }

                        // TODO: One call both with or without variables.
                        if (queryList[i].VariableValuesArr != null && queryList[i].VariableValuesArr.Length > 0)
                        {
                            // Call appropriate method to create execution enumerator.
                            if (queryList[i].IncludesLiteral)
                                sqlEnum = Db.SlowSQL(queryList[i].QueryString, queryList[i].VariableValuesArr).GetEnumerator() as SqlEnumerator<dynamic>;
                            else
                                sqlEnum = Db.SQL(queryList[i].QueryString, queryList[i].VariableValuesArr).GetEnumerator() as SqlEnumerator<dynamic>;
                        }
                        else
                        {
                            // Call appropriate method to create execution enumerator.
                            if (queryList[i].IncludesLiteral)
                                sqlEnum = Db.SlowSQL(queryList[i].QueryString).GetEnumerator() as SqlEnumerator<dynamic>;
                            else
                                sqlEnum = Db.SQL(queryList[i].QueryString).GetEnumerator() as SqlEnumerator<dynamic>;
                        }
                        // Collect the result of the query.
                        if (!queryList[i].SingleObjectProjection)
                            resultList = CreateResultComposite(sqlEnum);
                        else
                            resultList = CreateResultSingleton(sqlEnum);

                        // Save execution plan and result.
                        if (firstExecution)
                        {
                            queryList[i].ActualExecutionPlan1 = sqlEnum.ToString();
                            queryList[i].ActualResult1 = CreateResultString(resultList, queryList[i].ShouldBeReordered);
                        }
                        else
                        {
                            queryList[i].ActualExecutionPlan2 = sqlEnum.ToString();
                            queryList[i].ActualResult2 = CreateResultString(resultList, queryList[i].ShouldBeReordered);
                        }
                    }
                    catch (Exception exception)
                    {
                        if (firstExecution)
                        {
                            queryList[i].ActualExceptionMessage1 = exception.Message.Trim();
                            queryList[i].ActualFullException1 = exception.ToString();
                        }
                        else
                        {
                            queryList[i].ActualExceptionMessage2 = exception.Message.Trim();
                            queryList[i].ActualFullException2 = exception.ToString();
                        }
                    }
                    finally
                    {
                        if (sqlEnum != null)
                            sqlEnum.Dispose();

                        // Need to assign NULL value in order to prevent disposal of already disposed enumerator.
                        sqlEnum = null;
                    }
                }
            });
        }

        private static void ResultLoop(SqlEnumerator<IObjectView> sqlEnum)
        {
            IObjectView obj = null;
            while (sqlEnum.MoveNext())
            {
                obj = sqlEnum.Current;
            }
        }

        private static List<String> CreateResultSingleton(SqlEnumerator<dynamic> sqlEnum)
        {
            String result = headerFieldSeparator + sqlEnum.ProjectionTypeCode.ToString() + headerFieldSeparator;
            List<String> resultList = new List<String>();
            String strValue = null;

            resultList.Add(result);

            while (sqlEnum.MoveNext())
            {
                if (sqlEnum.ProjectionTypeCode == null)
                    throw new Exception("Incorrect Entity object. Maybe due to incorrect declaration \"SingleObjectProjection: True\".");
                strValue = Utilities.GetSingletonResult((DbTypeCode)sqlEnum.ProjectionTypeCode, sqlEnum.Current);
                result = Utilities.FieldSeparator + strValue + Utilities.FieldSeparator;
                resultList.Add(result);
            }

            return resultList;
        }

        private static List<String> CreateResultComposite(SqlEnumerator<dynamic> sqlEnum)
        {
            String result = headerFieldSeparator;
            List<String> resultList = new List<String>();
            ITypeBinding typeBind = sqlEnum.TypeBinding;
            IPropertyBinding propBind = null;

            for (Int32 i = 0; i < typeBind.PropertyCount; i++)
            {
                propBind = typeBind.GetPropertyBinding(i);
                result += propBind.Name + ":" + propBind.TypeCode.ToString() + headerFieldSeparator;
            }
            resultList.Add(result);

            while (sqlEnum.MoveNext())
            {
                if (sqlEnum.Current == null || sqlEnum.Current is Entity)
                    throw new Exception("Incorrect CompositeObject. Maybe due to incorrect declaration \"SingleObjectProjection: False\".");
                result = Utilities.CreateObjectString(typeBind, sqlEnum.Current);
                resultList.Add(result);
            }

            return resultList;
        }

        private static String CreateResultString(List<String> resultList, Boolean shouldBeReordered)
        {
            if (shouldBeReordered)
            {
                resultList.Sort(StringComparer.InvariantCulture);
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
