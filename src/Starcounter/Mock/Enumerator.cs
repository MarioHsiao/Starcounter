// ***********************************************************************
// <copyright file="Enumerator.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;
using Starcounter.Binding;
using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using Starcounter.Internal;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace Starcounter
{

    /// <summary>
    /// Class Enumerator
    /// </summary>
    public sealed class Enumerator : Object, IEnumerator<IObjectView>
    {
        private IObjectView _current = null;

        private UInt64 _handle = 0;
        /// <summary>
        /// Gets the cursor handle.
        /// </summary>
        /// <value>The cursor handle.</value>
        public UInt64 CursorHandle { get { return _handle; } }

        private UInt64 _verify = 0; // Also used to check if enumerator has been disposed or not.

        /// <summary>
        /// Gets the cursor verify.
        /// </summary>
        /// <value>The cursor verify.</value>
        public UInt64 CursorVerify { get { return _verify; } }

        /// <summary>
        /// Reference to the scheduler instance, where the enumerator is created
        /// </summary>
        internal readonly Scheduler SchedulerOwner;

        private FilterCallback _filterCallback = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="Enumerator{T}" /> class.
        /// </summary>
        /// <param name="handle">The handle.</param>
        /// <param name="verify">The verify.</param>
        public Enumerator() : this(null) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="Enumerator{T}" /> class.
        /// </summary>
        /// <param name="handle">The handle.</param>
        /// <param name="verify">The verify.</param>
        /// <param name="filterCallback">The filter callback.</param>
        public Enumerator(FilterCallback filterCallback)
            : base()
        {
            _filterCallback = filterCallback;
            SchedulerOwner = Scheduler.GetInstance();
        }

        // Enumerator already exists, we need to update the contents.
        /// <summary>
        /// Updates the cached.
        /// </summary>
        /// <param name="handle">The handle.</param>
        /// <param name="verify">The verify.</param>
        public void UpdateCached(UInt64 handle, UInt64 verify)
        {
            if (handle == 0 || verify == 0)
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "UpdateCached: wrong parameters.");
            }
            else
            {
                _handle = handle;
                _verify = verify;
                Debug.Assert(SchedulerOwner == Scheduler.GetInstance());
            }
        }
        /// <summary>
        /// Updates the filter.
        /// </summary>
        /// <param name="filterCallback">The filter callback.</param>
        public void UpdateFilter(FilterCallback filterCallback)
        {
            _filterCallback = filterCallback;
        }

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        /// <value>The current.</value>
        /// <exception cref="System.ObjectDisposedException">null</exception>
        /// <exception cref="System.InvalidOperationException">The enumerator is positioned before the first element of the collection or after the last element.</exception>
        /// <returns>The element in the collection at the current position of the enumerator.</returns>
        public IObjectView Current
        {
            get
            {
                if (_current != null)
                {
                    return _current;
                }
                if (_verify == 0)
                {
                    throw new ObjectDisposedException(null);
                }
                throw ErrorCode.ToException(Error.SCERRINVALIDCURRENT, (m, e) => new InvalidOperationException(m));
            }
        }

        /// <summary>
        /// Gets the current raw.
        /// </summary>
        /// <value>The current raw.</value>
        public IObjectView CurrentRaw
        {
            get
            {
                return _current;
            }
        }


        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() {
            // Checking if enumerator was already disposed or not yet created.
            if (_handle == 0 || _verify == 0)
                return;

            // Removing reference to current object.
            _current = null;

            UInt32 err = sccoredb.star_iterator_free(_handle, _verify);

            // Marking this enumerator as disposed.
            if (err == 0) {
                MarkAsDisposed();
                return;
            }

#if false // Should not be reachable
            // Checking for specific behavior.
            if ((err == Error.SCERRITERATORNOTOWNED) && (_verify == 0))
                return;
#endif

            // Otherwise returning error.
            throw ErrorCode.ToException(err);
        }

#if false
        /// <summary>
        /// Fetches iterator local time.
        /// </summary>
        public unsafe UInt32 GetIteratorLocalTime()
        {
            UInt32 local_time;
            UInt32 err = sccoredb.sccoredb_iterator_get_local_time(_handle, _verify, &local_time);
            if (err != 0)
                throw TranslateErrorCode(sccoredb.star_get_last_error());

            return local_time;
        }
#endif

#if false
        /// <summary>
        /// Continuously fills the provided buffer with object ETIs and IDs
        /// of objects found during search by engine (no managed filtering).
        /// </summary>
        public unsafe UInt32 NativeFillupFoundObjectIDs(Byte* results, UInt32 resultsMaxBytes, UInt32* resultsNum, UInt32* flags)
        {
            // Setting current object to null since no managed instance is created.
            _current = null;

            // Calling kernel to fill up the buffer in one execution.
            return sccoredb.SCIteratorFillUp(_handle, _verify, results, resultsMaxBytes, resultsNum, flags);
        }
#endif

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.</returns>
        public Boolean MoveNext()
        {
            TypeBinding typeBinding = null;
            IObjectProxy current;
            UInt16 previousCCI;
            UInt32 ir;
            ObjectRef currentRef;
            UInt16 currentCCI;
            Boolean br;
            current = null;
            previousCCI = UInt16.MaxValue;

        next:
            //Application.Profiler.Start("SCIteratorNext", 2);
            Boolean newIterator = _handle == 0;
            unsafe
            {
                ir = sccoredb.star_context_read_iterator(
                    ThreadData.ContextHandle, _handle, &currentRef.ObjectID, &currentRef.ETI,
                    _verify
                    );
            }
            //Application.Profiler.Stop(2);

            if (ir != 0) goto err;
            Debug.Assert(_handle != 0);

            if (newIterator) {
                Debug.Assert(SchedulerOwner == Scheduler.GetInstance());
                if (SchedulerOwner.NrOpenIterators < Scheduler.NROPENITERATORSPERSCHEDULER)
                    SchedulerOwner.NrOpenIterators++;
                else
                    ErrorCode.ToException(Error.SCERRTOMANYOPENITERATORS);
            }

            if (currentRef.ObjectID == sccoredb.MDBIT_OBJECTID)
                goto last;

            currentCCI = (ushort)(currentRef.ETI & 0xFFFF);

            // Check if the current object has the same code class as the
            // previous object. If so we re-use the instance instead of
            // creating a new (this will only happen if the previous object was
            // dropped from the result by the filter).
            if (previousCCI == currentCCI)
                goto attach;

            typeBinding = Bindings.GetTypeBinding(currentCCI);

            // Check and update expected layouthandle
            if (currentCCI != typeBinding.TableId) {
                currentRef.ETI = DbHelper.EncodeObjectRefWithLayoutHandle(currentRef.ETI, typeBinding.TableId);
                currentCCI = typeBinding.TableId;
            }

            current = typeBinding.NewInstanceUninit();
            if (current == null)
            {
                // A proxy couldn't be created or casted to the correct type so
                // we skip it.
                goto next;
            }
            previousCCI = currentCCI;

        attach:        
            current.Bind(currentRef.ETI, currentRef.ObjectID, typeBinding);
            if (_filterCallback != null)
            {
                br = _filterCallback(current);

                if (!br)
                    goto next;
            }
            _current = current;
            return true;

        last:
            _current = null;
            return false;

        err:
            throw TranslateErrorCode(ir);
        }

#if false
        public Boolean CreateProxyObject(UInt64 eti, UInt64 oid, UInt16 currentCCI)
        {
            _current = Bindings.GetTypeBinding(currentCCI).NewInstance(eti, oid) as T;
            return true;
        }
#endif

        /// <summary>
        /// Assumes that enumerator was already disposed and marks it like this.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MarkAsDisposed()
        {
            _handle = 0;
            _verify = 0;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Reset()
        {
            throw new NotSupportedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Boolean IsDisposed()
        {
            return (_verify == 0);
        }

        private Exception TranslateErrorCode(UInt32 ec)
        {
            // If the error indicates that the object isn't owned we check if
            // verification is set to 0. If so the object has been disposed.
            if (ec == Error.SCERRITERATORNOTOWNED && _verify == 0)
            {
                return new ObjectDisposedException(null);
            }
            return ErrorCode.ToException(ec);
        }

        Object IEnumerator.Current
        {
            get
            {
                return Current;
            }
        }
    }

    /// <summary>
    /// </summary>
    public sealed class FilterEnumerator : Object, IEnumerator<IObjectView> {
        private IObjectView _current = null;

        private UInt64 _handle = 0;
        /// <summary>
        /// Gets the cursor handle.
        /// </summary>
        /// <value>The cursor handle.</value>
        public UInt64 CursorHandle { get { return _handle; } }

        private UInt64 _verify = 0; // Also used to check if enumerator has been disposed or not.
        /// <summary>
        /// Gets the cursor verify.
        /// </summary>
        /// <value>The cursor verify.</value>
        public UInt64 CursorVerify { get { return _verify; } }

        /// <summary>
        /// Reference to the scheduler instance, where the enumerator is created
        /// </summary>
        internal readonly Scheduler SchedulerOwner;

        private FilterCallback _filterCallback = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="Enumerator{T}" /> class.
        /// </summary>
        /// <param name="handle">The handle.</param>
        /// <param name="verify">The verify.</param>
        public FilterEnumerator() : this(null) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="Enumerator{T}" /> class.
        /// </summary>
        /// <param name="handle">The handle.</param>
        /// <param name="verify">The verify.</param>
        /// <param name="filterCallback">The filter callback.</param>
        public FilterEnumerator(FilterCallback filterCallback)
            : base() {
            _filterCallback = filterCallback;
            SchedulerOwner = Scheduler.GetInstance();
        }

        // Enumerator already exists, we need to update the contents.
        /// <summary>
        /// Updates the cached.
        /// </summary>
        /// <param name="handle">The handle.</param>
        /// <param name="verify">The verify.</param>
        public void UpdateCached(UInt64 handle, UInt64 verify) {
            if (handle == 0 || verify == 0) {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "UpdateCached: wrong parameters.");
            }
            else {
                _handle = handle;
                _verify = verify;
                Debug.Assert(SchedulerOwner == Scheduler.GetInstance());
            }
        }
        /// <summary>
        /// Updates the filter.
        /// </summary>
        /// <param name="filterCallback">The filter callback.</param>
        public void UpdateFilter(FilterCallback filterCallback) {
            _filterCallback = filterCallback;
        }

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        /// <value>The current.</value>
        /// <exception cref="System.ObjectDisposedException">null</exception>
        /// <exception cref="System.InvalidOperationException">The enumerator is positioned before the first element of the collection or after the last element.</exception>
        /// <returns>The element in the collection at the current position of the enumerator.</returns>
        public IObjectView Current {
            get {
                if (_current != null) {
                    return _current;
                }
                if (_verify == 0) {
                    throw new ObjectDisposedException(null);
                }
                throw ErrorCode.ToException(Error.SCERRINVALIDCURRENT, (m, e) => new InvalidOperationException(m));
            }
        }

        /// <summary>
        /// Gets the current raw.
        /// </summary>
        /// <value>The current raw.</value>
        public IObjectView CurrentRaw {
            get {
                return _current;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() {
            // Checking if enumerator was already disposed or not yet created.
            if (_handle == 0 || _verify == 0)
                return;

            // Removing reference to current object.
            _current = null;
            UInt32 err = sccoredb.star_filter_iterator_free(_handle, _verify);

            // Marking this enumerator as disposed.
            if (err == 0) {
                MarkAsDisposed();
                return;
            }

#if false // Should not be reachable
            // Checking for specific behavior.
            if ((err == Error.SCERRITERATORNOTOWNED) && (_verify == 0))
                return;
#endif

            // Otherwise returning error.
            throw ErrorCode.ToException(err);
        }

#if false
        /// <summary>
        /// Fetches iterator local time.
        /// </summary>
        public unsafe UInt32 GetIteratorLocalTime() {
            UInt32 local_time;
            UInt32 err = sccoredb.filter_iterator_get_local_time(_handle, _verify, &local_time);
            if (err != 0)
                throw TranslateErrorCode(sccoredb.star_get_last_error());

            return local_time;
        }
#endif

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.</returns>
        public Boolean MoveNext() {
            TypeBinding typeBinding = null;
            IObjectProxy current;
            UInt16 previousCCI;
            UInt32 ir;
            ObjectRef currentRef;
            UInt16 currentCCI;
            Boolean br;
            current = null;
            previousCCI = UInt16.MaxValue;

        next:
            //Application.Profiler.Start("filter_iterator_next", 2);
            Boolean newIterator = _handle == 0;
            unsafe {
                ir = sccoredb.star_context_read_filter_iterator(
                    ThreadData.ContextHandle, _handle, &currentRef.ObjectID, &currentRef.ETI,
                    _verify
                    );
            }
            //Application.Profiler.Stop(2);

            if (ir != 0) goto err;
            Debug.Assert(_handle != 0);

            if (newIterator) {
                Debug.Assert(SchedulerOwner == Scheduler.GetInstance());
                if (SchedulerOwner.NrOpenIterators < Scheduler.NROPENITERATORSPERSCHEDULER)
                    SchedulerOwner.NrOpenIterators++;
                else
                    ErrorCode.ToException(Error.SCERRTOMANYOPENITERATORS);
            }

            if (currentRef.ObjectID == sccoredb.MDBIT_OBJECTID)
                goto last;

            currentCCI = (ushort)(currentRef.ETI & 0xFFFF);

            // Check if the current object has the same code class as the
            // previous object. If so we re-use the instance instead of
            // creating a new (this will only happen if the previous object was
            // dropped from the result by the filter).
            if (previousCCI == currentCCI)
                goto attach;

            typeBinding = Bindings.GetTypeBinding(currentCCI);

            // Check and update expected layouthandle
            if (currentCCI != typeBinding.TableId) {
                currentRef.ETI = DbHelper.EncodeObjectRefWithLayoutHandle(currentRef.ETI, typeBinding.TableId);
                currentCCI = typeBinding.TableId;
            }

            current = typeBinding.NewInstanceUninit();
            if (current == null) {
                // A proxy couldn't be created or casted to the correct type so
                // we skip it.
                goto next;
            }
            previousCCI = currentCCI;

        attach:
            current.Bind(currentRef.ETI, currentRef.ObjectID, typeBinding);
            if (_filterCallback != null) {
                br = _filterCallback(current);

                if (!br)
                    goto next;
            }
            _current = current;
            return true;

        last:
            _current = null;
            return false;

        err:
            throw TranslateErrorCode(ir);
        }

#if false
        public Boolean CreateProxyObject(UInt64 eti, UInt64 oid, UInt16 currentCCI)
        {
            _current = Bindings.GetTypeBinding(currentCCI).NewInstance(eti, oid) as T;
            return true;
        }
#endif

        /// <summary>
        /// Assumes that enumerator was already disposed and marks it like this.
        /// </summary>
        public void MarkAsDisposed() {
            _handle = 0;
            _verify = 0;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Reset() {
            throw new NotSupportedException();
        }

        internal Boolean IsDisposed() {
            return (_verify == 0);
        }

        private Exception TranslateErrorCode(UInt32 ec) {
            // If the error indicates that the object isn't owned we check if
            // verification is set to 0. If so the object has been disposed.
            if (ec == Error.SCERRITERATORNOTOWNED && _verify == 0) {
                return new ObjectDisposedException(null);
            }
            return ErrorCode.ToException(ec);
        }

        Object IEnumerator.Current {
            get {
                return Current;
            }
        }
    }
}
