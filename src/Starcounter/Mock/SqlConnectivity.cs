// ***********************************************************************
// <copyright file="SqlConnectivity.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;

namespace Starcounter
{
    /// <summary>
    /// Class SqlConnectivity
    /// </summary>
    public class SqlConnectivity
    {
        /// <summary>
        /// Inits the SQL functions.
        /// </summary>
        /// <returns>System.UInt32.</returns>
        public static uint InitSqlFunctions()
        {
            return 0;
        }

        /// <summary>
        /// Throws the converted server error.
        /// </summary>
        /// <param name="ec">The ec.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public static void ThrowConvertedServerError(uint ec)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the server profiling string.
        /// </summary>
        /// <returns>System.String.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static string GetServerProfilingString()
        {
            throw new NotImplementedException();
        }
    }

}