using System;
using se.sics.prologbeans;
using Sc.Server.Internal;
using Starcounter.Query.Sql;
using System.Text;
using System.Runtime.InteropServices;
using Starcounter.Query.Execution;
using System.Collections.Generic;

namespace Starcounter
{
 
    public sealed class Scheduler
    {
        // Contains all virtual processor instances
        // (up to number of logical cores on a machine).
        static Scheduler[] _instances;

        // Contains a list of last error messages.
        static LinkedList<String> _lastErrorMessages = new LinkedList<String>();

        // Global query cache.
        static GlobalQueryCache _globalCache = new GlobalQueryCache();

        internal static GlobalQueryCache GlobalCache
        {
            get { return _globalCache; }
        }

        internal static object InvalidateLock = new object();

        // Cache for SQL enumerators per virtual processor.
        // It means that cache is shared between several threads but
        // with non-preemptive scheduling (only one thread is executed at a time).
        readonly SqlEnumCache _sqlEnumCache;

        internal SqlEnumCache SqlEnumCache
        {
            get
            {
                return _sqlEnumCache;
            }
        }

        internal PrologSession PrologSession;

        private Scheduler(Int32 vpContextID)
            : base()
        {
            _sqlEnumCache = new SqlEnumCache();
        }
        
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

        internal static Scheduler GetInstance()
        {
            return GetInstance(false);
        }

        internal static Scheduler GetInstance(Byte cpuNumber)
        {
            return _instances[cpuNumber];
        }

        public static void Setup(Byte cpuCount)
        {
            Scheduler[] instances;
            Int32 i;
            instances = new Scheduler[cpuCount];
            for (i = 0; i < instances.Length; i++)
            {
                instances[i] = new Scheduler(i);
            }

            _instances = instances;
        }

        /// <summary>
        /// Adding new error message.
        /// Mutually exclusive.
        /// </summary>
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
        internal static byte SchedulerCount
        {
            get
            {
                return (byte)_instances.Length;
            }
        }

        /// <summary>
        /// Invalidates global cache, local cache of this scheduler and sets to invalidate local caches of other schedulers
        /// </summary>
        internal void InvalidateCache()
        {
            lock (InvalidateLock)
            {
                _globalCache = new GlobalQueryCache();
                foreach (Scheduler s in _instances)
                    s._sqlEnumCache.SetToInvalidate();
                this._sqlEnumCache.InvalidateCache();
            }
        }
    }
}
