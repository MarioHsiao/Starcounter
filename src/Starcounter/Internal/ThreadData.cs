// ***********************************************************************
// <copyright file="ThreadData.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

#define TRY_KEEP_DOWN_ENUM_NODE_COUNT

using Starcounter.Internal;
using System;

namespace Starcounter
{
    /// <summary>
    /// This class represents the context of the thread that is
    /// scheduled for execution by a virtual processor (VPContext).
    /// Every thread holds an independent instance of this class.
    /// </summary>
    public sealed class ThreadData : Object
    {

#if TRY_KEEP_DOWN_ENUM_NODE_COUNT
        private const int _DEFAULT_ENUM_NODE_COUNT_GC_THRESHOLD = 8;
#endif

        /// <summary>
        /// The current
        /// </summary>
        [ThreadStatic]
        public static ThreadData Current;

        /// <summary>
        /// Gets the current if attached and reattach if auto detached.
        /// </summary>
        /// <returns>ThreadData.</returns>
        internal static ThreadData GetCurrentIfAttachedAndReattachIfAutoDetached()
        {
            ThreadData current;
            UInt32 ec;
            current = Current;
            if (current == null)
            {
                return null;
            }
            unsafe
            {
                if (*current._pStateShare == 1)
                {
                    return current;
                }
            }
            ec = sccorelib.cm3_eautodet((IntPtr)0);
            if (ec == 0)
            {
                return current;
            }
            if (ec == Error.SCERRTHREADNOTAUTODETACHED)
            {
                // Ending auto detached failed because thread has been manually
                // detached.
                return null;
            }
            throw ErrorCode.ToException(ec);
        }

        //
        // Since an instance of this class only is tied to a single thread and
        // only can be accessed from the thread isn't tied to none of the
        // methods supported by this class need be thread-safe.
        //

        private uint _gcCounterMark;

        private int _enumNodeCount;

#if TRY_KEEP_DOWN_ENUM_NODE_COUNT	
        private int _enumNodeCountGCThreshold;
#endif

        private XNode _firstEnumNode;

        /// <summary>
        /// The scheduler
        /// </summary>
        public readonly Scheduler Scheduler;

        /// <summary>
        /// The _p state share
        /// </summary>
        private readonly unsafe UInt32* _pStateShare;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadData" /> class.
        /// </summary>
        /// <param name="schedulerNumber">The scheduler number.</param>
        /// <param name="pStateShare">The p state share.</param>
        public unsafe ThreadData(Byte schedulerNumber, UInt32* pStateShare)
        {
            Scheduler = Scheduler.GetInstance(schedulerNumber);
            _gcCounterMark = CaptureGC.Counter;
#if TRY_KEEP_DOWN_ENUM_NODE_COUNT
            _enumNodeCountGCThreshold = _DEFAULT_ENUM_NODE_COUNT_GC_THRESHOLD;
#endif
            _pStateShare = pStateShare;
        }

        internal void RegisterObject(XNode node) {
            uint gcCounter;
            int ir;

            gcCounter = CaptureGC.Counter;
            if (_gcCounterMark != gcCounter) {
                _gcCounterMark = gcCounter;
                ir = CleanupDeadObjects();
#if TRY_KEEP_DOWN_ENUM_NODE_COUNT
                if (_enumNodeCount == _enumNodeCountGCThreshold)
                    _enumNodeCountGCThreshold *= 2;
#endif
            }
#if TRY_KEEP_DOWN_ENUM_NODE_COUNT
            else if (_enumNodeCount == _enumNodeCountGCThreshold) {
                ir = CleanupDeadObjects();
                if (ir == 0)
                    _enumNodeCountGCThreshold *= 2;
            }
#endif

            _enumNodeCount++;

            node.Next = _firstEnumNode;
            _firstEnumNode = node;
        }

        /// <summary>
        /// </summary>
        public bool CollectAndTryToCleanupDeadObjects(bool fullGC) {
            if (!fullGC) GC.Collect(0, GCCollectionMode.Forced);
            else GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            int ir = CleanupDeadObjects();
            return (ir != 0);
        }

        internal void CleanupAllObjects() {
            XNode node;

            node = _firstEnumNode;

            while (node != null) {
                node.Cleanup();
                node = node.Next;
            }

            _enumNodeCount = 0;
#if TRY_KEEP_DOWN_ENUM_NODE_COUNT
            _enumNodeCountGCThreshold = _DEFAULT_ENUM_NODE_COUNT_GC_THRESHOLD;
#endif
            _firstEnumNode = null;
        }

        private Int32 CleanupDeadObjects() {
            int nodeCount;
            XNode prevNode;
            XNode node;
            XNode nextNode;
            bool br;
            int ret;

            nodeCount = _enumNodeCount;

            prevNode = null;
            node = _firstEnumNode;

            while (node != null) {
                nextNode = node.Next;

                br = node.TryCleanup();

                if (br) {
                    if (prevNode == null) _firstEnumNode = nextNode;
                    else prevNode.Next = nextNode;

                    node = nextNode;
                    nodeCount--;
                    continue;
                }

                prevNode = node;
                node = node.Next;
                continue;
            }

            ret = (_enumNodeCount - nodeCount);
            if (ret == 0) return 0;

            _enumNodeCount = nodeCount;

#if TRY_KEEP_DOWN_ENUM_NODE_COUNT
            int nodeCountGCThreshold;
            nodeCountGCThreshold = _enumNodeCountGCThreshold / 2;
            while (
                nodeCountGCThreshold >= _DEFAULT_ENUM_NODE_COUNT_GC_THRESHOLD &&
                nodeCount <= nodeCountGCThreshold
                ) {
                _enumNodeCountGCThreshold = nodeCountGCThreshold;
                nodeCountGCThreshold /= 2;
            }
#endif

            return ret;
        }
    }

    internal class XNode : WeakReference {

        public XNode Next;

        private IDisposable _resource;

        public XNode(IDisposable user, IDisposable resource)
            : base(user) {
            _resource = resource;
        }

        public void MarkAsDead() {
            _resource = null;
        }

        public bool TryCleanup() {
            if (_resource == null) return true;
            if (IsAlive) return false;
            DoCleanup();
            return true;
        }

        public void Cleanup() {
            if (_resource == null) return;
            var user = (IDisposable)Target;
            if (user != null) {
                user.Dispose();

                // Disposing the resource might very well mark the node as dead
                // and reset the resource reference so it might here already be
                // set to null.

                if (_resource == null) return;
            }
            DoCleanup();
        }

        private void DoCleanup() {
            _resource.Dispose();
            _resource = null;
        }
    }

    /// <summary>
    /// Class used to keep track of when garbage collections has been executed.
    /// Used in order to keep track of when job bound references should be
    /// verified.
    /// </summary>
    public sealed class CaptureGC { // Internal
        // Note that since Finalize is used to discover when a garbage
        // collection has been executed it will be some delay between the time
        // the garbage ran and the counter is updated.

        internal static volatile uint Counter;

        static CaptureGC() {
            Counter = 0;
        }

        /// <summary>
        /// </summary>
        public CaptureGC() : base() { }

        /// <summary>
        /// </summary>
        ~CaptureGC() {
            var counter = Counter;
            if (counter != UInt32.MaxValue) {
                Counter = (counter + 1);
            }
            else Counter = 0;

            if (!AppDomain.CurrentDomain.IsFinalizingForUnload()) {
                new CaptureGC();
            }
        }
    }
}
