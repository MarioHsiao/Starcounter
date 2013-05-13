// ***********************************************************************
// <copyright file="Scheduler.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using se.sics.prologbeans;
using Starcounter.Query.Sql;
using System.Text;
using System.Runtime.InteropServices;
using Starcounter.Query.Execution;
using System.Collections.Generic;

namespace Starcounter
{

    /// <summary>
    /// Class Scheduler
    /// </summary>
    public sealed class Scheduler
    {
        // Contains all virtual processor instances
        // (up to number of logical cores on a machine).
        /// <summary>
        /// The _instances
        /// </summary>
        static Scheduler[] _instances;

        // Contains a list of last error messages.
        /// <summary>
        /// The _last error messages
        /// </summary>
        static LinkedList<String> _lastErrorMessages = new LinkedList<String>();

        // Global query cache.
        /// <summary>
        /// The _global cache
        /// </summary>
        static GlobalQueryCache _globalCache = new GlobalQueryCache(0);

        /// <summary>
        /// Gets the global cache.
        /// </summary>
        /// <value>The global cache.</value>
        internal static GlobalQueryCache GlobalCache
        {
            get { return _globalCache; }
        }

        /// <summary>
        /// The invalidate lock
        /// </summary>
        internal static object InvalidateLock = new object();

        // Cache for SQL enumerators per virtual processor.
        // It means that cache is shared between several threads but
        // with non-preemptive scheduling (only one thread is executed at a time).
        /// <summary>
        /// The _SQL enum cache
        /// </summary>
        readonly SqlEnumCache _sqlEnumCache;

        /// <summary>
        /// Gets the SQL enum cache.
        /// </summary>
        /// <value>The SQL enum cache.</value>
        public SqlEnumCache SqlEnumCache
        {
            get
            {
                return _sqlEnumCache;
            }
        }

        /// <summary>
        /// The Prolog session
        /// </summary>
        internal PrologSession PrologSession;

        readonly Byte _id;

        /// <summary>
        /// Global id of this scheduler.
        /// </summary>
        public Byte Id
        {
            get { return _id; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Scheduler" /> class.
        /// </summary>
        /// <param name="schedId">The scheduler ID.</param>
        private Scheduler(Byte schedId)
        {
            _sqlEnumCache = new SqlEnumCache();
            _id = schedId;
        }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <param name="nullIfNotAttached">The null if not attached.</param>
        /// <returns>Scheduler.</returns>
        internal static Scheduler GetInstance(Boolean nullIfNotAttached)
        {
            ThreadData thread;
            thread = ThreadData.GetCurrentIfAttachedAndReattachIfAutoDetached();
            if (thread != null)
            {
                return thread.Scheduler;
            }
            if (nullIfNotAttached)
            {
                return null;
            }
            throw ErrorCode.ToException(Error.SCERRTHREADNOTATTACHED);
        }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <returns>Scheduler.</returns>
        internal static Scheduler GetInstance()
        {
            return GetInstance(false);
        }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <param name="cpuNumber">The scheduler ID.</param>
        /// <returns>Scheduler.</returns>
        internal static Scheduler GetInstance(Byte schedId)
        {
            return _instances[schedId];
        }

        /// <summary>
        /// Setups the specified CPU count.
        /// </summary>
        /// <param name="cpuCount">The cpu count.</param>
        public static void Setup(Byte schedCount)
        {
            Scheduler[] instances = new Scheduler[schedCount];

            for (Byte i = 0; i < instances.Length; i++)
            {
                instances[i] = new Scheduler(i);
            }

            _instances = instances;
        }

        /// <summary>
        /// Adding new error message.
        /// Mutually exclusive.
        /// </summary>
        /// <param name="errorMsg">The error MSG.</param>
        internal static void AddErrorMessage(String errorMsg)
        {
            lock (_lastErrorMessages)
            {
                _lastErrorMessages.AddLast(errorMsg);
            }
        }

        /// <summary>
        /// Fetches all error messages into one big string.
        /// </summary>
        /// <returns>String.</returns>
        internal static String GetErrorMessages()
        {
            String concatErrMsg = "";
            lock (_lastErrorMessages)
            {
                // Checking if there are any elements.
                if (_lastErrorMessages.Count <= 0)
                    return concatErrMsg;

                // Concatenating together all stored error messages.
                while (true)
                {
                    concatErrMsg += _lastErrorMessages.Last.Value;
                    _lastErrorMessages.RemoveLast();

                    if (_lastErrorMessages.Count <= 0)
                        break;

                    concatErrMsg += Environment.NewLine + "------------------------------------------" + Environment.NewLine;
                }
            }

            return concatErrMsg;
        }

        // Returns number of virtual processors.
        /// <summary>
        /// Gets the scheduler count.
        /// </summary>
        /// <value>The scheduler count.</value>
        internal static Byte SchedulerCount
        {
            get
            {
                return (Byte)_instances.Length;
            }
        }

        /// <summary>
        /// Invalidates global cache, local cache of this scheduler and sets to invalidate local caches of other schedulers
        /// </summary>
        /// <param name="generation">The generation.</param>
        public void InvalidateCache(ulong generation)
        {
            // Calling thread will be yield blocked.

            GlobalQueryCache cacheToUse;

            lock (InvalidateLock)
            {
                cacheToUse = _globalCache;
                ulong cacheGeneration = cacheToUse.Generation;

                if (cacheGeneration == generation)
                {
                    // Global query cache if of the same generation as
                    // requested.
                }
                else if (cacheGeneration < generation)
                {
                    cacheToUse = _globalCache = new GlobalQueryCache(generation);
                }
                else
                {
                    // The generation we want is older then the generation of
                    // the global query cache. We create a temporary cache
                    // until the current scheduler can get up to date and use
                    // the latest shared version.

                    cacheToUse = new GlobalQueryCache(generation);
                }
            }

            this._sqlEnumCache.InvalidateCache(cacheToUse);
        }
    }
}
