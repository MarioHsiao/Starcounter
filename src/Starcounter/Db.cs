// ***********************************************************************
// <copyright file="Db.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Advanced;
using Starcounter.Binding;
using Starcounter.Internal;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Reflection;

namespace Starcounter
{

    public static partial class Db
    {
        /// <summary>
        /// </summary>
        public static DbEnvironment Environment { get; private set; }

        /// <summary>
        /// Gets the set of <see cref="Application"/>s currently running
        /// in the <see cref="Db"/>.
        /// </summary>
        public static Application[] Applications {
            get {
                return Application.GetAllApplications();
            }
        }

        /// <summary>
        /// Occurs when the database is being stopped.
        /// </summary>
        public static event EventHandler DatabaseStopping;

        /// <summary>
        /// Lookups the table.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>TableDef.</returns>
        public static TableDef LookupTable(string name)
        {
            unsafe
            {
                ulong token = sccoredb.GetTokenFromString(name);
                if (token != 0) {
                    sccoredb.STARI_LAYOUT_INFO tableInfo;
                    var r = sccoredb.stari_context_get_layout_info_by_token(ThreadData.ContextHandle, token, out tableInfo);
                    if (r == 0) {
                        return TableDef.ConstructTableDef(tableInfo);
                    }
                    if (r == Error.SCERRTABLENOTFOUND) return null;
                    throw ErrorCode.ToException(r);
                }
                return null;
            }
        }

        /// <summary>
        /// Creates the table.
        /// </summary>
        /// <param name="tableDef">The table def.</param>
        public static void CreateTable(TableDef tableDef)
        {
            CreateTable(tableDef, null);
        }

        /// <summary>
        /// Creates the table.
        /// </summary>
        /// <param name="tableDef">The table def.</param>
        /// <param name="inheritedTableDef">The inherited table def.</param>
        public static void CreateTable(TableDef tableDef, TableDef inheritedTableDef)
        {
            unsafe
            {
                int implicitColumnCount;
                ushort inheritedTableId = 0;
                if (inheritedTableDef == null)
                {
                    implicitColumnCount = 2; // Implicit key column.
                }
                else
                {
                    // TODO:
                    // We're assume that the base table definition is complete
                    // (has definition address) and that the current table
                    // definition and the inherited table definition matches.

                    implicitColumnCount = inheritedTableDef.ColumnDefs.Length;
                    inheritedTableId = inheritedTableDef.TableId;
                }
                ColumnDef[] columns = tableDef.ColumnDefs;
                sccoredb.STARI_COLUMN_DEFINITION[] column_definitions = new sccoredb.STARI_COLUMN_DEFINITION[columns.Length - implicitColumnCount + 1];
                Debug.Assert(column_definitions.Length > 0);
                try
                {
                    for (int cc = column_definitions.Length - 1, ci = implicitColumnCount, di = 0; di < cc; ci++, di++)
                    {
                        column_definitions[di].token = sccoredb.AssureTokenForString(columns[ci].Name);
                        column_definitions[di].type = columns[ci].Type;
                        column_definitions[di].is_nullable = columns[ci].IsNullable ? (byte)1 : (byte)0;
                    }
                    ulong token = sccoredb.AssureTokenForString(tableDef.Name);
                    Db.Transact(() => {
                        fixed (sccoredb.STARI_COLUMN_DEFINITION* fixed_column_definitions = column_definitions)
                        {
                            uint e = sccoredb.stari_context_create_layout(ThreadData.ContextHandle, token, inheritedTableId, fixed_column_definitions, 0);
                            if (e != 0) throw ErrorCode.ToException(e);
                        }
                    });

                    // TODO EOH: Same transaction. Handle errors (comes pretty automatically if same transaction).
                    Db.Transact(() => {
                        uint e;
                        sccoredb.STARI_LAYOUT_INFO layoutInfo;
                        e = sccoredb.stari_context_get_layout_info_by_token(ThreadData.ContextHandle, token, out layoutInfo);
                        if (e != 0) throw ErrorCode.ToException(e);

                        ushort tableId = layoutInfo.layout_handle;
                        ulong indexToken = sccoredb.AssureTokenForString(tableDef.Name + "_auto"); // TODO EOH: Default index name?

                        short *columnIndexes = stackalloc short[2];
                        columnIndexes[0] = 0;
                        columnIndexes[1] = -1;

                        e = sccoredb.stari_context_create_index(
                            ThreadData.ContextHandle, indexToken, sccoredb.TableIdToSetSpec(tableId), tableId, columnIndexes, 0, 0
                            );
                        if (e != 0) throw ErrorCode.ToException(e);
                    });
                }
                finally { }
            }
        }

        /// <summary>
        /// Renames the table.
        /// </summary>
        /// <param name="tableId">The table id.</param>
        /// <param name="newName">The new name.</param>
        public static void RenameTable(ushort tableId, string newName)
        {
            uint e = sccoredb.star_rename_table(0, tableId, newName);
            if (e == 0) return;
            throw ErrorCode.ToException(e);
        }

        /// <summary>
        /// Drops the table.
        /// </summary>
        /// <param name="name">The name.</param>
        public static void DropTable(string name)
        {
            uint e = sccoredb.star_drop_table(0, name);
            if (e == 0) return;
            throw ErrorCode.ToException(e);
        }

        /// <summary>
        /// Executes the given <paramref name="action"/> within a new transaction.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <param name="forceSnapshot">
        /// If set, instructs Starcounter to raise an error if the transaction can't
        /// be executed within a single snapshot (taken at the time of the transaction
        /// start). The default is false, allowing the isolation to drop to "read
        /// committed" in case the transaction for some reason should block or take a
        /// long time.
        /// </param>
        /// <param name="maxRetries">Number of times to retry the execution of the
        /// transaction if committing it fails because of a conflict with another
        /// transaction. Specify <c>int.MaxValue</c> to instruct Starcounter
        /// to try until the transaction succeeds. Specify 0 to disable retrying.
        /// </param>
        public static void Transact(Action action, bool forceSnapshot = false, int maxRetries = 100) {
            Transact(action, 0, forceSnapshot, maxRetries);
        }

        internal static void Transact(Action action, uint flags, bool forceSnapshot = false, int maxRetries = 100) {
            int retries;
            uint r;
            ulong handle;
            ulong verify;

            if (maxRetries < 0) {
                throw new ArgumentOutOfRangeException("maxRetries", string.Format("Valid range: 0-{0}", int.MaxValue));
            }

            if (forceSnapshot) {
                throw ErrorCode.ToException(Error.SCERRNOTIMPLEMENTED, "Forcing snapshot isolation is not yet implemented.");
            }

            retries = 0;

            // TODO EOH: Lock transaction on thread. Not supported currently.

            for (; ; ) {
                r = sccoredb.star_context_create_transaction(ThreadData.ContextHandle, flags, out handle);
                if (r == 0) {
                    verify = sccorelib.GetCpuNumber(); // TODO EOH:
                    r = sccoredb.star_context_set_current_transaction(ThreadData.ContextHandle, handle); // TODO EOH: Handle error.

                    // We only set the handle to none here, since Transaction.Current will follow this value.
                    var currentTransaction = TransactionManager.GetCurrentAndSetToNoneManagedOnly();

                    try {
                        action();
                        TransactionManager.Commit(handle, 1, 1);
                        return;
                    } catch (Exception ex) {
                        uint cr = sccoredb.star_context_set_current_transaction(
                            ThreadData.ContextHandle, 0
                            );
                        if (cr == 0) cr = sccoredb.star_transaction_free(handle);
                        if (cr == 0) {
                            if (ex is ITransactionConflictException) {
                                if (++retries <= maxRetries) continue;
                                throw ErrorCode.ToException(Error.SCERRUNHANDLEDTRANSACTCONFLICT, ex);
                            }
                            throw;
                        }
                        HandleFatalErrorInTransactionScope(cr);
                    } finally {
                        TransactionManager.SetCurrentTransaction(currentTransaction);
                    }
                }

                if (r == Error.SCERRTRANSACTIONLOCKEDONTHREAD) {
                    // We already have a transaction locked on thread so we're
                    // already in a transaction scope (possibly an implicit one if
                    // for example in the context of a trigger): Just invoke the
                    // callback and exit.

                    try {
                        action();
                    } catch {
                        // Operation will fail only if transaction is already
                        // aborted (in which case we need not abort it).

                        sccoredb.sccoredb_external_abort();
                        throw;
                    }
                    return;
                }

                throw ErrorCode.ToException(r);
            }
        }

        internal static void SystemTransact(Action action, bool forceSnapshot = false, int maxRetries = 100) {
            Transact(action, 0, forceSnapshot, maxRetries);
        }

        public static void Scope(Action action, bool isReadOnly = false) {
            TransactionHandle transactionHandle = TransactionHandle.Invalid;
            TransactionHandle old = StarcounterBase.TransactionManager.CurrentTransaction;
            bool create = (old.handle == 0 || old.IsImplicit);
            try {
                if (create)
                    transactionHandle = TransactionManager.CreateAndSetCurrent(isReadOnly, false);
                action();
            } finally {
                TransactionManager.SetCurrentTransaction(old);
                if (create)
                    TransactionManager.CheckForRefOrDisposeTransaction(transactionHandle);
            }
        }

        public static void Scope<T>(Action<T> action, T arg, bool isReadOnly = false) {
            TransactionHandle transactionHandle = TransactionHandle.Invalid;
            TransactionHandle old = StarcounterBase.TransactionManager.CurrentTransaction;
            bool create = (old.handle == 0 || old.IsImplicit);
            try {
                if (create)
                    transactionHandle = TransactionManager.CreateAndSetCurrent(isReadOnly, false);
                action(arg);
            } finally {
                TransactionManager.SetCurrentTransaction(old);
                if (create)
                    TransactionManager.CheckForRefOrDisposeTransaction(transactionHandle);
            }
        }

        public static void Scope<T1, T2>(Action<T1, T2> action, T1 arg1, T2 arg2, bool isReadOnly = false) {
            TransactionHandle transactionHandle = TransactionHandle.Invalid;
            TransactionHandle old = StarcounterBase.TransactionManager.CurrentTransaction;
            bool create = (old.handle == 0 || old.IsImplicit);
            try {
                if (create) 
                    transactionHandle = TransactionManager.CreateAndSetCurrent(isReadOnly, false);
                action(arg1, arg2);
            } finally {
                TransactionManager.SetCurrentTransaction(old);
                if (create)
                    TransactionManager.CheckForRefOrDisposeTransaction(transactionHandle);
            }
        }

        public static void Scope<T1, T2, T3>(Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3, bool isReadOnly = false) {
            TransactionHandle transactionHandle = TransactionHandle.Invalid;
            TransactionHandle old = StarcounterBase.TransactionManager.CurrentTransaction;
            bool create = (old.handle == 0 || old.IsImplicit);
            try {
                if (create)
                    transactionHandle = TransactionManager.CreateAndSetCurrent(isReadOnly, false);
                action(arg1, arg2, arg3);
            } finally {
                TransactionManager.SetCurrentTransaction(old);
                if (create) 
                    TransactionManager.CheckForRefOrDisposeTransaction(transactionHandle);
            }
        }

        public static TResult Scope<TResult>(Func<TResult> func, bool isReadOnly = false) {
            TransactionHandle transactionHandle = TransactionHandle.Invalid;
            TransactionHandle old = StarcounterBase.TransactionManager.CurrentTransaction;
            bool create = (old.handle == 0 || old.IsImplicit);
            try {
                if (create)
                    transactionHandle = TransactionManager.CreateAndSetCurrent(isReadOnly, false);
                return func();
            } finally {
                TransactionManager.SetCurrentTransaction(old);
                if (create)
                    TransactionManager.CheckForRefOrDisposeTransaction(transactionHandle);
            }
        }

        public static TResult Scope<T, TResult>(Func<T, TResult> func, T arg, bool isReadOnly = false) {
            TransactionHandle transactionHandle = TransactionHandle.Invalid;
            TransactionHandle old = StarcounterBase.TransactionManager.CurrentTransaction;
            bool create = (old.handle == 0 || old.IsImplicit);
            try {
                if (create)
                    transactionHandle = TransactionManager.CreateAndSetCurrent(isReadOnly, false); 
                return func(arg);
            } finally {
                TransactionManager.SetCurrentTransaction(old);
                if (create)
                    TransactionManager.CheckForRefOrDisposeTransaction(transactionHandle);
            }
        }

        public static TResult Scope<T1, T2, TResult>(Func<T1, T2, TResult> func, T1 arg1, T2 arg2, bool isReadOnly = false) {
           TransactionHandle transactionHandle = TransactionHandle.Invalid;
           TransactionHandle old = StarcounterBase.TransactionManager.CurrentTransaction;
           bool create = (old.handle == 0 || old.IsImplicit);
           try {
               if (create) 
                   transactionHandle = TransactionManager.CreateAndSetCurrent(isReadOnly, false); 
                return func(arg1, arg2);
            } finally {
                TransactionManager.SetCurrentTransaction(old);
                if (create) 
                    TransactionManager.CheckForRefOrDisposeTransaction(transactionHandle);
            }
        }

        /// <summary>
        /// </summary>
        public static void SetEnvironment(DbEnvironment e) // TODO: Internal
        {
            Environment = e;
        }

        /// <summary>
        /// Gets a value indicating of the given object is a
        /// Starcounter database instance.
        /// </summary>
        /// <param name="obj">The object to evaluate.</param>
        /// <returns>True if considered a database object; false
        /// otherwise.</returns>
        public static bool IsPersistent(this object obj) {
            // This is probably not the final solution; we should compare
            // this one (and maybe other alternatives) to the one that is
            // probably fastest (but most complex), as suggested here:
            // http://www.starcounter.com/forum/showthread.php?2493-Making-a-polymorphic-Object.IsPersistent()
            return obj is IObjectProxy;
        }

        /// <summary>
        /// Deletes an object from the database. The runtime will check
        /// the object to see if it's a valid database object. If it is,
        /// the delete will be carried out. If not, an exception will be
        /// raised.
        /// </summary>
        /// <param name="target">
        /// The database object to delete.</param>
        public static void Delete(this object target) {
            var proxy = target as IObjectProxy;
            if (proxy == null) {
                // Proper error message
                // TODO:
                throw ErrorCode.ToException(Error.SCERRUNSPECIFIED, "Not a database object.");
            }
            Db.Delete(proxy);
        }

        /// <summary>
        /// Deletes a database object from the database.
        /// </summary>
        /// <param name="proxy">The database object to delete,
        /// reprsented as a <see cref="IObjectProxy"/>.</param>
        public static void Delete(this IObjectProxy proxy) {
            ulong oid;
            ulong address;

            oid = proxy.Identity;
            address = proxy.ThisHandle;

            int ir = sccoredb.star_context_set_trans_flags(
                ThreadData.ContextHandle, oid, address, sccoredb.DELETE_PENDING
                );
            if (ir != 0) {
                // Positive value contains previously set flags, negative value indicates and error.

                if (ir > 0)
                {
                    if ((ir & sccoredb.DELETE_PENDING) != 0)
                    {
                        // Delete already was issued. We ignore it and just return. We are
                        // processing the delete of this object so it will be deleted eventually.
                        return;
                    }
                }
                else throw ErrorCode.ToException((uint)(-ir));
            }

            // TODO EOH: Lock transaction on thread executing hook callback.

            // Invoke all callbacks. If any of theese throws an exception then
            // we rollback the issued delete and pass on the thrown exception
            // to the caller.

            // Invoke mapper
            // Temporary, prototypish solution. Follow progress and see what we
            // are planning in https://github.com/Starcounter/Starcounter/issues/2683
            //
            // When should this be called? Before or after OnDelete?
            // TODO:
            if (MapConfig.Enabled) {
                MapInvoke.DELETE(proxy.TypeBinding.Name, oid);
            }

            try {
                InvokeOnDelete(proxy);
            } catch (Exception ex) {
                // We can't generate an exception from an error in this
                // function since this will hide the original error.
                //
                // We can handle any error that can occur except for a fatal
                // error (and this will kill the process) and that the thread
                // has been detached (shouldn't occur). The most important
                // thing is that the transaction lock set when the delete was
                // issued is released and this will be the case as long as none
                // of the above errors occur.

                sccoredb.star_context_reset_trans_flags(
                    ThreadData.ContextHandle, oid, address, sccoredb.DELETE_PENDING
                    );
                if (ex is System.Threading.ThreadAbortException) throw;
                throw ErrorCode.ToException(Error.SCERRERRORINHOOKCALLBACK, ex);
            }

            uint r = sccoredb.star_context_delete(ThreadData.ContextHandle, oid, address);
            if (r == 0) return;
            throw ErrorCode.ToException(r);
        }

        /// <summary>
        /// Raise the event that signals the database is stopping.
        /// </summary>
        internal static void RaiseDatabaseStoppingEvent() {
            if (DatabaseStopping != null) {
                DatabaseStopping(null, EventArgs.Empty);
            }
        }

        static void InvokeOnDelete(IObjectProxy proxy) {
            // These flags really don't do their work right now, since we
            // are in fact doing a cast just to retreive them. The whole
            // purpose of them is to not do an extra cast if not neccessary.
            // From the proxy, we must all for a non-generated way of
            // getting the binding.
            // TODO:

            var binding = proxy.TypeBinding as TypeBinding;
            if ((binding.Flags & TypeBindingFlags.Callback_OnDelete) != 0) {
                ((IEntity)proxy).OnDelete();
            }

            if ((binding.Flags & TypeBindingFlags.Hook_OnDelete) != 0) {
                var key = HookKey.FromTable(binding.TableId, HookType.BeforeDelete);
                InvokableHook.InvokeBeforeDelete(key, proxy);
            }
        }

        private static void HandleFatalErrorInTransactionScope(uint e)
        {
            ExceptionManager.HandleInternalFatalError(e);
        }
    }
}
