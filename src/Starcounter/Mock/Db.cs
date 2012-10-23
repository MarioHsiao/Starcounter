// ***********************************************************************
// <copyright file="Db.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
//using System.Data;
using System.Reflection;
//using Sc.Server.Weaver;
using Starcounter.LucentObjects;
using Starcounter.Query.Execution;
using Starcounter.Query.Sql;
using Starcounter.Binding;


namespace Starcounter
{
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
        /// Returns the result of an SQL query as an SqlResult which implements IEnumerable.
        /// </summary>
        /// <param name="query">An SQL query.</param>
        /// <param name="values">The values to be used for variables in the query.</param>
        /// <returns>The result of the SQL query.</returns>
        public static SqlResult SQL(String query, params Object[] values)
        {
            if (query == null)
                throw new ArgumentNullException("query");

#if true
            return new SqlResult(0, query, false, values);
#else
            if (Starcounter.Transaction.Current != null)
                return new SqlResult(Starcounter.Transaction.Current.TransactionId, query, false, values); 
            else
                return new SqlResult(0, query, false, values);
#endif
        }

        /// <summary>
        /// Returns the result of an SQL query as an SqlResult which implements IEnumerable.
        /// Especially queries with expected slow execution are supported, as for example queries including literals.
        /// </summary>
        /// <param name="query">An SQL query.</param>
        /// <param name="values">The values to be used for variables in the query.</param>
        /// <returns>The result of the SQL query.</returns>
        public static SqlResult SlowSQL(String query, params Object[] values)
        {
            if (query == null)
                throw new ArgumentNullException("query");

            UInt64 transactionId = 0;
#if false
            if (Starcounter.Transaction.Current != null)
				transactionId = Starcounter.Transaction.Current.TransactionId;
#endif

            if (query == "")
				return new SqlResult(transactionId, query, true, values);

            switch (query[0])
            {
                case 'S':
                case 's':
                    return new SqlResult(transactionId, query, true, values);

                case 'C':
                case 'c':
                    SqlProcessor.ProcessCreateIndex(query);
                    return null;

                case 'D':
                case 'd':
                    if (SqlProcessor.ProcessDQuery(query, values))
                        return null;
                    else
                        return new SqlResult(transactionId, query, true, values);

                case ' ':
                case '\t':
                    query = query.TrimStart(' ', '\t');
                    switch (query[0])
                    {
                        case 'S':
                        case 's':
                            return new SqlResult(transactionId, query, true, values);

                        case 'C':
                        case 'c':
                            SqlProcessor.ProcessCreateIndex(query);
                            return null;

                        case 'D':
                        case 'd':
                            if (SqlProcessor.ProcessDQuery(query, values))
                                return null;
                            else
                                return new SqlResult(transactionId, query, true, values);

                        default:
                            return new SqlResult(transactionId, query, true, values);
                    }

                default:
                    return new SqlResult(transactionId, query, true, values);
            }
        }
    }
}
