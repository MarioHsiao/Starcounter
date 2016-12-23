using System;
using System.Runtime.InteropServices;
using Starcounter.Internal;

namespace Starcounter
{
    public class TransactionToken : IDisposable
    {
        private class TransactionHandle : SafeHandle
        {
            public TransactionHandle(ulong th) : base(IntPtr.Zero, true)
            {
                SetHandle(new IntPtr((long)th));
            }

            public override bool IsInvalid
            {
                get
                {
                    return handle == IntPtr.Zero;
                }
            }

            protected override bool ReleaseHandle()
            {
                //safe to free on any thread as transaction shouldn't be attached to any context
                return sccoredb.star_transaction_free_unsafe(Handle) == 0;
            }

            public ulong Handle => (ulong)handle.ToInt64();
        }

        private TransactionHandle _handle;

        internal TransactionToken(ulong source_transaction)
        {
            ulong th;

            var r = sccoredb.star_clone_transaction(source_transaction, out th);
            if ( r != 0 )
                throw ErrorCode.ToException(r);

            _handle = new TransactionHandle(th);
        }

        internal uint CloneTransaction(out ulong new_transaction)
        {
            lock (_handle)
            {
                if (_handle.IsClosed)
                    throw new ObjectDisposedException(nameof(TransactionToken));

                return sccoredb.star_clone_transaction(_handle.Handle, out new_transaction);
            }
        }

        public void Dispose()
        {
            lock (_handle)
            {
                _handle.Dispose();
            }
        }
    }
}
