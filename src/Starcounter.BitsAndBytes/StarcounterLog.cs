// ***********************************************************************
// <copyright file="StarcounterLog.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;

namespace Starcounter.Internal
{
    /// <summary>
    /// Enum LogLevel
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// The debug
        /// </summary>
        Debug,
        /// <summary>
        /// The info
        /// </summary>
        Info,
        /// <summary>
        /// The warn
        /// </summary>
        Warn,
        /// <summary>
        /// The error
        /// </summary>
        Error
    }

    /// <summary>
    /// Temporary logging class. To be replaced by proper Starcounter logging.
    /// </summary>
    public class StarcounterLog
    {
        /// <summary>
        /// The level
        /// </summary>
        public static LogLevel Level = LogLevel.Info;

        /// <summary>
        /// The log action
        /// </summary>
        public static Action<LogLevel, string, Exception> LogAction = (level, message, ex) =>
        {
            if (level >= Level)
                Console.WriteLine("{0} [{1}] {2} {3}", DateTime.Now, level, message, ex);
        };

        /// <summary>
        /// Warns the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="ex">The ex.</param>
        public static void Warn(string message, Exception ex = null)
        {
            LogAction(LogLevel.Warn, message, ex);
        }

        /// <summary>
        /// Errors the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="ex">The ex.</param>
        public static void Error(string message, Exception ex = null)
        {
            LogAction(LogLevel.Error, message, ex);
        }

        /// <summary>
        /// Debugs the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="ex">The ex.</param>
        public static void Debug(string message, Exception ex = null)
        {
            LogAction(LogLevel.Debug, message, ex);
        }

        /// <summary>
        /// Infoes the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="ex">The ex.</param>
        public static void Info(string message, Exception ex = null)
        {
            LogAction(LogLevel.Info, message, ex);
        }

    }
}
