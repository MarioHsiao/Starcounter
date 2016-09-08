// ***********************************************************************
// <copyright file="Db.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
//using System.Data;
using System.Reflection;
//using Sc.Server.Weaver;
using Starcounter.Query.Execution;
using Starcounter.Binding;
using System.Text;
using Starcounter.Advanced;
using System.Diagnostics;


namespace Starcounter
{
    /// <summary>
    /// Class Db
    /// </summary>
    public static partial class Db
    {

        /// <summary>
        /// Used to represent the "null" value as a string in a uniform way.
        /// </summary>
        public const String NullString = "<NULL>";

        /// <summary>
        /// The no id string
        /// </summary>
        public const String NoIdString = "<NoId>";
        /// <summary>
        /// The field separator
        /// </summary>
        public const String FieldSeparator = " | ";

#if true
        /// <summary>
        /// Binaries to hex.
        /// </summary>
        /// <param name="binValue">The bin value.</param>
        /// <returns>String.</returns>
        public static String BinaryToHex(Binary binValue)
        {
            if (binValue == null)
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect binValue.");

            String hexString = BitConverter.ToString(binValue.ToArray());
            return hexString.Replace("-", "");
        }

        /// <summary>
        /// Hexes to binary.
        /// </summary>
        /// <param name="hexString">The hex string.</param>
        /// <returns>Binary.</returns>
        public static Binary HexToBinary(String hexString)
        {
            if (hexString == null)
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect hexString.");

            Byte[] byteArr = new Byte[hexString.Length / 2];
            for (Int32 i = 0; i < byteArr.Length; i++)
            {
                byteArr[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            }
            return new Binary(byteArr);
        }
#endif

        /// <summary>
        /// Returns the result of an SQL query as an Rows which implements IEnumerable.
        /// </summary>
        /// <param name="query">An SQL query.</param>
        /// <param name="values">The values to be used for variables in the query.</param>
        /// <returns>The result of the SQL query.</returns>
        public static QueryResultRows<Object> SQL(String query, params Object[] values) {
            return SQL<Object>(query, values);
        }

        /// <summary>
        /// Returns the result of an SQL query as an SqlResult&lt;T&gt; which implements IEnumerable&lt;T&gt;.
        /// </summary>
        /// <typeparam name="T">The type the items in the result set should have.</typeparam>
        /// <param name="query">An SQL query.</param>
        /// <param name="values">The values to be used for variables in the query.</param>
        /// <returns>The result of the SQL query.</returns>
        public static QueryResultRows<T> SQL<T>(String query, params Object[] values) {
            return FullQueryProcess<T>(query, false, values);
        }

        /// <summary>
        /// Returns the result of an SQL query as an SqlResult which implements IEnumerable.
        /// Especially queries with expected slow execution are supported, as for example aggregations.
        /// </summary>
        /// <param name="query">An SQL query.</param>
        /// <param name="values">The values to be used for variables in the query.</param>
        /// <returns>The result of the SQL query.</returns>
        public static QueryResultRows<Object> SlowSQL(String query, params Object[] values)
        {
            return SlowSQL<Object>(query, values);
        }

        /// <summary>
        /// Returns the result of an SQL query as an SqlResult which implements IEnumerable.
        /// Especially queries with expected slow execution are supported, as for example aggregations.
        /// </summary>
        /// <param name="query">An SQL query.</param>
        /// <param name="values">The values to be used for variables in the query.</param>
        /// <returns>The result of the SQL query.</returns>
        public static QueryResultRows<T> SlowSQL<T>(String query, params Object[] values) {
            return FullQueryProcess<T>(query, true, values);
        }

        public static SqlProcessor.NewQueryResultRows<T> NewSQL<T>(String query, params Object[] values) {
            return SqlProcessor.NewQueryResultRows<T>.ProcessSqlQuery(query, values);
        }

        public static int Update(String query, params Object[] values) {
            return SqlProcessor.SqlProcessor.ExecuteQuerySqlProcessor(query);
        }

        private static QueryResultRows<T> FullQueryProcess<T>(String query, bool slowSQL, params Object[] values) {
            if (query == null)
                throw ErrorCode.ToException(Error.SCERRBADARGUMENTS, "Input query string cannot be null");

            // Capture thread. Yield if time slice for current task is up or if there is a higher
            // priority task to run.
            Starcounter.Internal.sccorelib.cm3_yieldc(IntPtr.Zero);

            int cacheResult = Scheduler.GetInstance().SqlEnumCache.CacheOrExecuteEnumerator<T>(query, slowSQL, values);
            if (cacheResult == -1)
                return null;
            QueryResultRows<T> enumerableResult = new QueryResultRows<T>(0, query, slowSQL, values);
            return enumerableResult;
        }

        /// <summary>
        /// Unloads entire database into given file as set of INSERT INTO statements
        /// </summary>
        /// <param name="fileName">Path of the file were the statements are written.</param>
        /// <returns>Number of unloaded objects.</returns>
        public static int Unload(string fileName, ulong shiftId = 0, Boolean unloadAll = true) {
            return Starcounter.Reload.Unload(fileName, shiftId, unloadAll);
        }

        /// <summary>
        /// Reloads entire database by executing INSERT INTO statements from the given file.
        /// </summary>
        /// <param name="fileName">Name of the file with INSERT INTO statements.</param>
        /// <returns>Number of inserted objects.</returns>
        public static int Reload(string fileName) {
            return Starcounter.Reload.Load(fileName);
        }
    }

    /// <summary>
    /// Enables you to use SQL (Structured Query Language) on the
    /// Starcounter database.
    /// </summary>
    public static class SQL {
        public static QueryResultRows<T> SELECT<T>(string query, params Object[] values) {
            return Db.SQL<T>( String.Concat( "SELECT _O_ FROM ", typeof(T).FullName, " _O_ ", query ), values);
        }
    }


    /// <summary>
    /// Enables you to use SQL (Structured Query Language) on the
    /// Starcounter database.
    /// </summary>
    public static class SELECT<T> {
       public static QueryResultRows<T> WHERE(string query, params Object[] values) {
          return Db.SQL<T>(String.Concat("SELECT _O_ FROM ", typeof(T).FullName, " _O_ WHERE ", query), values);
       }
    }

    public static class Table<T> {
       public static Type Get { get { return typeof(T); } }
    }

    public static class SELECT {
       public static class FROM<T> {
          public static QueryResultRows<T> WHERE(string query, params Object[] values) {
             return Db.SQL<T>(String.Concat("SELECT _O_ FROM ", typeof(T).FullName, " _O_ WHERE ", query), values);
          }
       }
    }


   /// <summary>
   /// Holds the global functions SQL, GET/POST/PUT/DELETE/PATCH and Transaction functions that operates on 
   /// Starcounters database and communication server.
   /// </summary>
    public class F : StarcounterBase {
    }
}
