using Starcounter;
using Starcounter.Binding;
using Sc.Server.Internal;
using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using Starcounter.Internal;

namespace Starcounter
{

    public sealed class Enumerator<T> : Object, IEnumerator<T> where T : Entity
    {
        private T _current = null;

        private UInt64 _handle = 0;
        public UInt64 CursorHandle { get { return _handle; } }

        private UInt64 _verify = 0; // Also used to check if enumerator has been disposed or not.
        public UInt64 CursorVerify { get { return _verify; } }

        private FilterCallback _filterCallback = null;

        public Enumerator(UInt64 handle, UInt64 verify) : this(handle, verify, null) { }
        public Enumerator(UInt64 handle, UInt64 verify, FilterCallback filterCallback)
            : base()
        {
            _handle = handle;
            _verify = verify;
            _filterCallback = filterCallback;
        }

        // Enumerator already exists, we need to update the contents.
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
            }
        }
        public void UpdateFilter(FilterCallback filterCallback)
        {
            _filterCallback = filterCallback;
        }

        public T Current
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
                throw new InvalidOperationException(
                    "The enumerator is positioned before the first element of the collection or after the last element."
                );
            }
        }

        public T CurrentRaw
        {
            get
            {
                return _current;
            }
        }

        public void Dispose()
        {
            // Checking if enumerator was already disposed or not yet created.
            if (_handle == 0 || _verify == 0)
                return;

            // Removing reference to current object.
            _current = null;
            UInt32 err = sccoredb.SCIteratorFree(_handle, _verify);

            // Marking this enumerator as disposed.
            if (err == 0)
            {
                _handle = 0;
                _verify = 0;
                return;
            }

            // Checking for specific behavior.
            if ((err == Error.SCERRITERATORNOTOWNED) && (_verify == 0))
                return;

            // Otherwise returning error.
            throw ErrorCode.ToException(err);
        }

        /// <summary>
        /// Fetches iterator local time.
        /// </summary>
        public unsafe UInt32 GetIteratorLocalTime()
        {
            UInt32 local_time;
            UInt32 err = sccoredb.sccoredb_iterator_get_local_time(_handle, _verify, &local_time);
            if (err != 0)
                throw TranslateErrorCode(sccoredb.Mdb_GetLastError());

            return local_time;
        }

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

        public Boolean MoveNext()
        {
            TypeBinding typeBinding = null;
            T current;
            UInt16 previousCCI;
            UInt32 ir;
            ObjectRef currentRef;
            UInt16 currentCCI;
            UInt64 dummy;
            Boolean br;
            current = default(T);
            previousCCI = UInt16.MaxValue;

        next:
            //Application.Profiler.Start("SCIteratorNext", 2);
            unsafe
            {
                ir = sccoredb.SCIteratorNext(_handle, _verify, &currentRef.ObjectID, &currentRef.ETI, &currentCCI, &dummy);
            }
            //Application.Profiler.Stop(2);

            if (ir != 0)
                goto err;

            if (currentRef.ObjectID == sccoredb.MDBIT_OBJECTID)
                goto last;

            // Check if the current object has the same code class as the
            // previous object. If so we re-use the instance instead of
            // creating a new (this will only happen if the previous object was
            // dropped from the result by the filter).
            if (previousCCI == currentCCI)
                goto attach;

            typeBinding = Bindings.GetTypeBinding(currentCCI);
            current = (typeBinding.NewInstanceUninit() as T);
            if (current == null)
            {
                // A proxy couldn't be created or casted to the correct type so
                // we skip it.
                goto next;
            }
            previousCCI = currentCCI;

        attach:
            current.Attach(currentRef, typeBinding);
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
            throw TranslateErrorCode(sccoredb.Mdb_GetLastError());
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
        public void MarkAsDisposed()
        {
            _handle = 0;
            _verify = 0;
        }

        public void Reset()
        {
            throw new NotSupportedException();
        }

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
}
