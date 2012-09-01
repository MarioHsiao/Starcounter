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
    /// <summary>
    /// This class represents the virtual processor,
    /// which can schedule tasks(jobs) for threads.
    /// The number of virtual processors normally is the number
    /// of physical CPUs/cores on execution machine.
    /// </summary>
    internal sealed class VPContext : Object
    {
        // Contains all virtual processor instances
        // (up to number of logical cores on a machine).
        static VPContext[] _instances;

        // Contains a list of last error messages.
        static LinkedList<String> _lastErrorMessages = new LinkedList<String>();

        // Global query cache.
        static GlobalQueryCache _globalCache = new GlobalQueryCache();

        internal static GlobalQueryCache GlobalCache
        {
            get { return _globalCache; }
        }

        internal static VPContext GetInstance(Boolean nullIfNotAttached)
        {
            throw new System.NotImplementedException();
        }

        internal static VPContext GetInstance()
        {
            return GetInstance(false);
        }

        internal static VPContext GetInstance(Byte cpuNumber)
        {
            if (_instances == null)
            {
                return null;
            }
            return _instances[cpuNumber];
        }

        internal static void Setup(Byte cpuCount)
        {
            Console.WriteLine("Number of schedulers: " + cpuCount);

            VPContext[] instances;
            Int32 i;
            instances = new VPContext[cpuCount];
            for (i = 0; i < instances.Length; i++)
            {
                instances[i] = new VPContext(i);
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
        internal static Byte VPCount
        {
            get
            {
                if (_instances != null)
                {
                    return (Byte)_instances.Length;
                }

                return 0;
            }
        }

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

        /*readonly ClientQueryCache _clientSqlEnumCache;

        internal ClientQueryCache ClientSqlEnumCache
        {
            get
            {
                return _clientSqlEnumCache;
            }
        }*/

        internal PrologSession PrologSession;

        private VPContext(Int32 vpContextID)
            : base()
        {
            _sqlEnumCache = new SqlEnumCache();
            //clientSqlEnumCache = new ClientQueryCache();
        }
    }
}
