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
        public static SqlResult<Object> SQL(String query, params Object[] values) {
            return SQL<Object>(query, values);
        }

        /// <summary>
        /// Returns the result of an SQL query as an SqlResult&lt;T&gt; which implements IEnumerable&lt;T&gt;.
        /// </summary>
        /// <typeparam name="T">The type the items in the result set should have.</typeparam>
        /// <param name="query">An SQL query.</param>
        /// <param name="values">The values to be used for variables in the query.</param>
        /// <returns>The result of the SQL query.</returns>
        public static SqlResult<T> SQL<T>(String query, params Object[] values) {
            if (query == null)
                throw ErrorCode.ToException(Error.SCERRBADARGUMENTS, "Input query string cannot be null");
            SqlResult<T> enumerableResult = null;
            try {
                enumerableResult = new SqlResult<T>(0, query, false, values);
                enumerableResult.CacheExecutionEnumerator();
            } catch (Exception) {
                try {
                    if (Starcounter.Query.Sql.SqlProcessor.ParseNonSelectQuery(query, false, values))
                        return null;
                } catch (Exception e) {
                    if (!(e is SqlException) || ((uint?)e.Data[ErrorCode.EC_TRANSPORT_KEY] == Error.SCERRSQLUNKNOWNNAME))
                        throw;
                }
                throw;
            }
            Debug.Assert(enumerableResult != null);
#if true
            return enumerableResult;
#else
            if (Starcounter.Transaction.Current != null)
                return new SqlResult<T>(Starcounter.Transaction.Current.TransactionId, query, false, values); 
            else
                return new SqlResult<T>(0, query, false, values);
#endif
        }

        /// <summary>
        /// Returns the result of an SQL query as an SqlResult which implements IEnumerable.
        /// Especially queries with expected slow execution are supported, as for example aggregations.
        /// </summary>
        /// <param name="query">An SQL query.</param>
        /// <param name="values">The values to be used for variables in the query.</param>
        /// <returns>The result of the SQL query.</returns>
        public static SqlResult<Object> SlowSQL(String query, params Object[] values)
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
        public static SqlResult<T> SlowSQL<T>(String query, params Object[] values) {
            if (query == null)
                throw new ArgumentNullException("query");

            UInt64 transactionId = 0;
#if false
            if (Starcounter.Transaction.Current != null)
				transactionId = Starcounter.Transaction.Current.TransactionId;
#endif

            //if (query == "")
            //    return new SqlResult<T>(transactionId, query, true, values);
            if (Starcounter.Query.Sql.SqlProcessor.ParseNonSelectQuery(query, true, values))
                return null;
            else {
                SqlResult<T> enumerableResult = new SqlResult<T>(transactionId, query, true, values);
                enumerableResult.CacheExecutionEnumerator();
                return enumerableResult;
            }
        }
    }

    /// <summary>
    /// Enables you to use SQL (Structured Query Language) on the
    /// Starcounter database.
    /// </summary>
    public static class SQL {
        public static SqlResult<T> SELECT<T>(string query, params Object[] values) {
            return Db.SQL<T>( String.Concat( "SELECT _O_ FROM ", typeof(T).FullName, " _O_ ", query ), values);
        }
    }


    /// <summary>
    /// Enables you to use SQL (Structured Query Language) on the
    /// Starcounter database.
    /// </summary>
    public static class SELECT<T> {
       public static SqlResult<T> WHERE(string query, params Object[] values) {
          return Db.SQL<T>(String.Concat("SELECT _O_ FROM ", typeof(T).FullName, " _O_ WHERE ", query), values);
       }
    }

    public static class Table<T> {
       public static Type Get { get { return typeof(T); } }
    }

    public static class SELECT {
       public static class FROM<T> {
          public static SqlResult<T> WHERE(string query, params Object[] values) {
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
