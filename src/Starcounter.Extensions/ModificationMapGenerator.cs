using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Starcounter.Internal;
using Starcounter.Metadata;

namespace Starcounter.Extensions {
    internal static class ModificationMapGenerator {
        private const string sqlOneTableSelect = "SELECT t FROM Starcounter.Metadata.RawView t WHERE t.FullName=?";
        private const string sqlAllColumnsForTableSelect = "SELECT c FROM Starcounter.Metadata.Column c WHERE c.Table=?";
        private const string sqlOneColumnForTableSelect = "SELECT c FROM Starcounter.Metadata.Column c WHERE c.Table=? AND c.Name=?";

        private static MethodInfo fromIDMethod = typeof(DbHelper).GetMethod("FromID", BindingFlags.Static | BindingFlags.Public);
        private static MethodInfo getMappedObjBaseMethod = typeof(DbMapping).GetMethod("GetMappedObject", BindingFlags.Static | BindingFlags.Public);
        private static MethodInfo debugViewMethod = typeof(Expression).GetMethod("get_DebugView", BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>
        /// Builds an expression tree using database metadata for the specified generic types and compiles 
        /// the resulting expression to a delegate that is used for modification maps.
        /// 
        /// The current implementation maps all properties that have the same name and type in both 
        /// to and from types. Also handles one-to-one references including getting mapped object from 
        /// reference.
        /// </summary>
        /// <typeparam name="TFrom">The source object</typeparam>
        /// <typeparam name="TTo">The destination object</typeparam>
        /// <returns>Modification converter</returns>
        internal static Action<ulong, ulong> Create<TFrom, TTo>() {
            Expression expr;
            Expression ifTrueExpr;
            Expression ifFalseExpr;
            Expression ifTestExpr;
            Expression fromPropertyExpr;
            Expression toPropertyExpr;
            List<Expression> blockExpressions;
            Column fromColumn;
            PropertyInfo toProperty;
            PropertyInfo fromProperty;
            Table fromTable;
            Table toTable;

            fromTable = Db.SQL<Table>(sqlOneTableSelect, typeof(TFrom).FullName).First;
            if (fromTable == null)
                throw new Exception("Cannot find metadata for type " + typeof(TFrom).FullName);

            toTable = Db.SQL<Table>(sqlOneTableSelect, typeof(TTo).FullName).First;
            if (toTable == null)
                throw new Exception("Cannot find metadata for type " + typeof(TTo).FullName);

            var fromObjExpr = Expression.Variable(typeof(TFrom), "fromObj");
            var toObjExpr = Expression.Variable(typeof(TTo), "toObj");
            var toOIDdParam = Expression.Parameter(typeof(ulong), "toOID");
            var fromOIDdParam = Expression.Parameter(typeof(ulong), "fromOID");

            blockExpressions = new List<Expression>();

            expr = Expression.Call(fromIDMethod, fromOIDdParam);
            expr = Expression.Convert(expr, typeof(TFrom));
            expr = Expression.Assign(fromObjExpr, expr);
            blockExpressions.Add(expr);

            expr = Expression.Call(fromIDMethod, toOIDdParam);
            expr = Expression.Convert(expr, typeof(TTo));
            expr = Expression.Assign(toObjExpr, expr);
            blockExpressions.Add(expr);

            foreach (var toColumn in Db.SQL<Column>(sqlAllColumnsForTableSelect, toTable)) {
                if (IsKeyColumn(toColumn))
                    continue;

                // Checking if we have a corresponding property in source table.
                fromColumn = Db.SQL<Column>(sqlOneColumnForTableSelect, fromTable, toColumn.Name).First;
                if (fromColumn == null)
                    continue;

                if (IsReferenceType(toColumn.DataType)) {
                    if (!IsReferenceType(fromColumn.DataType))
                        continue;
                } else if (!fromColumn.DataType.Equals(toColumn.DataType)) // Same name but different types.
                    continue;

                toProperty = typeof(TTo).GetProperty(toColumn.Name);
                if (!toProperty.CanWrite) // Property is readonly
                    continue;

                fromProperty = typeof(TFrom).GetProperty(fromColumn.Name);

                fromPropertyExpr = Expression.Property(fromObjExpr, fromProperty);
                toPropertyExpr = Expression.Property(toObjExpr, toProperty);

                if (IsReferenceType(toColumn.DataType)) {
                    // Resulting code should look like this (RefObj = name of property):
                    //
                    // if (fromObj.RefObj != null) {
                    //    toObj.RefObj = DbMapping.GetMappedObject<TTo>(fromObj.RefObj);
                    // } else {
                    //    toObj.RefObj = null;
                    // }

                    var method = getMappedObjBaseMethod.MakeGenericMethod(toProperty.PropertyType);

                    // Call DbMapping.GetMappedObject<TTo>(objectno)
                    ifTrueExpr = Expression.Call(null, method, fromPropertyExpr);

                    // Assign the return value to toObj reference property.
                    ifTrueExpr = Expression.Assign(toPropertyExpr, ifTrueExpr);

                    // If false set destionation ref to null.
                    ifFalseExpr = Expression.Assign(toPropertyExpr, Expression.Constant(null, toProperty.PropertyType));

                    // Test for if reference from source is not null.
                    ifTestExpr = Expression.NotEqual(fromPropertyExpr, Expression.Constant(null, fromProperty.PropertyType));

                    // Add if-then-else block.
                    expr = Expression.IfThenElse(ifTestExpr, ifTrueExpr, ifFalseExpr);

                    // Add the whole block.
                    blockExpressions.Add(expr);
                } else {
                    blockExpressions.Add(Expression.Assign(toPropertyExpr, fromPropertyExpr));
                }
            }

            expr = Expression.Block(new ParameterExpression[] { fromObjExpr, toObjExpr }, blockExpressions);
            var convertLambda = Expression.Lambda<Action<ulong, ulong>>(expr, fromOIDdParam, toOIDdParam);

//            LogMapModification(typeof(TFrom).FullName, typeof(TTo).FullName, convertLambda);

            return convertLambda.Compile();
        }

        private static bool IsReferenceType(DataType dataType) {
            return dataType.Name.Equals("reference");
        }

        private static bool IsKeyColumn(Column column) {
            return column.Name.Equals("__id")
                    || column.Name.Equals("__setspecifier")
                    || column.Name.Equals("__type_id");
        }

        [Conditional("DEBUG")]
        private static void LogMapModification(string fromTypeName, string toTypeName, Expression lambda) {
            try {
                var filePath = StarcounterEnvironment.Directories.UserAppDataDirectory + "\\mapmodifications.log";
                var debugStr = (string)debugViewMethod.Invoke(lambda, new object[0]);
                File.AppendAllText(filePath, fromTypeName + " => " + toTypeName + "\r\n" + debugStr + "\r\n\r\n");
            } catch { }
        }
    }
}
