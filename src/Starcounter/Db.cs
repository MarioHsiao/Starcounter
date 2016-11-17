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
using Starcounter.Metadata;

namespace Starcounter {

    public static partial class Db {
        /// <summary>
        /// </summary>
        public static DbEnvironment Environment { get; private set; }

        /// <summary>
        /// Gets the set of <see cref="Application"/>s currently running
        /// in the <see cref="Db"/>.
        /// </summary>
        public static Application[] Applications
        {
            get
            {
                return Application.GetAllApplications();
            }
        }

        /// <summary>
        /// Occurs when the database is being stopped.
        /// </summary>
        public static event EventHandler DatabaseStopping;

        /// <summary>
        /// Gets the table definition based on the name.
        /// </summary>
        /// <param name="name">The fullname of the table.</param>
        /// <returns>TableDef.</returns>
        public static TableDef LookupTable(string name) {
            unsafe
            {
                ulong token = SqlProcessor.SqlProcessor.GetTokenFromName(name);
                if (token != 0) {
                    uint layoutInfoCount = 1;
                    sccoredb.STARI_LAYOUT_INFO layoutInfo;
                    var r = sccoredb.stari_context_get_layout_infos_by_token(
                        ThreadData.ContextHandle, token, &layoutInfoCount, &layoutInfo
                        );
                    if (r == 0) {
                        if (layoutInfoCount > 1)
                            return LookupTableFromRawView(name, layoutInfoCount);

                        Debug.Assert(layoutInfoCount < 2);
                        if (layoutInfoCount != 0)
                            return TableDef.ConstructTableDef(layoutInfo, layoutInfoCount, true);
                        return null;
                    }
                    throw ErrorCode.ToException(r);
                }
                return null;
            }
        }

        private static TableDef LookupTableFromRawView(string name, uint layoutInfoCount) {
            unsafe
            {
                var rawView = Db.SQL<RawView>("SELECT r FROM Starcounter.Metadata.RawView r WHERE r.FullName=?", name).First;
                if (rawView != null) {
                    sccoredb.STARI_LAYOUT_INFO layoutInfo;
                    uint ec = sccoredb.stari_context_get_layout_info(
                                  ThreadData.ContextHandle, rawView.LayoutHandle, out layoutInfo
                              );
                    if (ec == 0)
                        return TableDef.ConstructTableDef(layoutInfo, layoutInfoCount, true);

                    throw ErrorCode.ToException(ec);
                }
                return null;
            }
        }

        /// <summary>
        /// Creates the table.
        /// </summary>
        /// <param name="tableDef">The table def.</param>
        public static void CreateTable(TableDef tableDef) {
            CreateTable(tableDef, null);
        }

        /// <summary>
        /// Creates the table.
        /// </summary>
        /// <param name="tableDef">The table def.</param>
        /// <param name="inheritedTableDef">The inherited table def.</param>
        public static void CreateTable(TableDef tableDef, TableDef inheritedTableDef) {
            // Operation creates its own transaction. Not allowed if transaction is locked on
            // thread.
            if (ThreadData.inTransactionScope_ != 0)
                throw ErrorCode.ToException(Error.SCERRTRANSACTIONLOCKEDONTHREAD);

            unsafe
            {
                int implicitColumnCount;
                ushort inheritedTableId = 0;
                if (inheritedTableDef == null) {
                    implicitColumnCount = 3; // Implicit key column.
                } else {
                    // TODO:
                    // We're assume that the base table definition is complete
                    // (has definition address) and that the current table
                    // definition and the inherited table definition matches.

                    implicitColumnCount = inheritedTableDef.ColumnDefs.Length;
                    inheritedTableId = inheritedTableDef.TableId;
                }
                ColumnDef[] columns = tableDef.ColumnDefs;
                SqlProcessor.SqlProcessor.STAR_COLUMN_DEFINITION_WITH_NAMES[] column_defs =
                    new SqlProcessor.SqlProcessor.STAR_COLUMN_DEFINITION_WITH_NAMES[columns.Length - implicitColumnCount + 1];
                Debug.Assert(column_defs.Length > 0);

                try {
                    for (int cc = column_defs.Length - 1, ci = implicitColumnCount, di = 0; di < cc; ci++, di++) {
                        column_defs[di].name = (char*)Marshal.StringToCoTaskMemUni(columns[ci].Name);
                        column_defs[di].primitive_type = columns[ci].Type;
                        column_defs[di].is_nullable = columns[ci].IsNullable ? (byte)1 : (byte)0;
                    }
                    ushort layout_id;
                    fixed (SqlProcessor.SqlProcessor.STAR_COLUMN_DEFINITION_WITH_NAMES* fixed_column_defs = column_defs)
                    {
                        SqlProcessor.SqlProcessor.CreatTableByNames(
                            tableDef.Name, tableDef.BaseName, fixed_column_defs,
                            out layout_id);
                    }
                } finally { }
            }
        }

        /// <summary>
        /// Renames the table.
        /// </summary>
        /// <param name="tableId">The table id.</param>
        /// <param name="newName">The new name.</param>
        public static void RenameTable(ushort tableId, string newName) {
            // Rename table no longer supported by kernel (no tables in kernel even). Used by table
            // upgrade so probably to be dropped (should not be public).
            throw new NotSupportedException(); // TODO EOH:
#if false
            uint e = sccoredb.star_rename_table(0, tableId, newName);
            if (e == 0) return;
            throw ErrorCode.ToException(e);
#endif
        }

        /// <summary>
        /// Drops the table.
        /// </summary>
        /// <param name="name">The name.</param>
        public static void DropTable(string name) {
            // Drop table no longer supported by kernel (no tables in kernel even). Used by table
            // upgrade so probably to be dropped (should not be public).
            throw new NotSupportedException(); // TODO EOH:
#if false
            uint e = sccoredb.star_drop_table(0, name);
            if (e == 0) return;
            throw ErrorCode.ToException(e);
#endif
        }

        /// <summary>
        /// Executes the given <paramref name="action"/> within a new transaction.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <param name="maxRetries">Number of times to retry the execution of the
        /// transaction if committing it fails because of a conflict with another
        /// transaction. Specify <c>int.MaxValue</c> to instruct Starcounter
        /// to try until the transaction succeeds. Specify 0 to disable retrying.
        /// </param>
        public static void Transact(Action action, int maxRetries = 100) {
            Transact(action, 0, new Advanced.TransactOptions() { maxRetries = maxRetries });
        }

        /// <summary>
        /// Executes the given <paramref name="action"/> within a new transaction.
        /// </summary>
        /// <typeparam name="T">The type of the parameter for the action.</typeparam>
        /// <param name="action">The action to execute.</param>
        /// <param name="arg">Parameter to use as input to the action.</param>
        /// <param name="maxRetries">Number of times to retry the execution of the
        /// transaction if committing it fails because of a conflict with another
        /// transaction. Specify <c>int.MaxValue</c> to instruct Starcounter
        /// to try until the transaction succeeds. Specify 0 to disable retrying.
        /// </param>
        [Obsolete]
        public static void Transact<T>(Action<T> action, T arg, int maxRetries = 100) {
            Transact(()=>action(arg), 0, new Advanced.TransactOptions() { maxRetries = maxRetries });
        }

        /// <summary>
        /// Executes the given <paramref name="func"/> within a new transaction.
        /// </summary>
        /// <typeparam name="TResult">The type of the return value of the func.</typeparam>
        /// <param name="func">The func to execute.</param>
        /// <param name="maxRetries">Number of times to retry the execution of the
        /// transaction if committing it fails because of a conflict with another
        /// transaction. Specify <c>int.MaxValue</c> to instruct Starcounter
        /// to try until the transaction succeeds. Specify 0 to disable retrying.
        /// </param>
        /// <returns>The return value of the func.</returns>
        public static TResult Transact<TResult>(Func<TResult> func, int maxRetries = 100) {
            return Transact<TResult>(func, 0, new Advanced.TransactOptions() { maxRetries = maxRetries });
        }

        /// <summary>
        /// Executes the given <paramref name="func"/> within a new transaction.
        /// </summary>
        /// <typeparam name="TResult">The type of the return value of the func.</typeparam>
        /// <typeparam name="T">The type of the parameter of the func.</typeparam>
        /// <param name="func">The func to execute.</param>
        /// <param name="arg">Parameter to use as input to the func</param>
        /// <param name="maxRetries">Number of times to retry the execution of the
        /// transaction if committing it fails because of a conflict with another
        /// transaction. Specify <c>int.MaxValue</c> to instruct Starcounter
        /// to try until the transaction succeeds. Specify 0 to disable retrying.
        /// </param>
        /// <returns>The return value of the func.</returns>
        [Obsolete]
        public static TResult Transact<T, TResult>(Func<T, TResult> func, T arg, int maxRetries = 100) {
            return Transact(()=>func(arg), 0, new Advanced.TransactOptions() { maxRetries = maxRetries });
        }
        
        public static class Advanced {
            public class TransactOptions {
                public int maxRetries {
                    get; set;
                } = 100;

                public bool applyHooks {
                    get; set;
                } = true;
            }
            
            public static void Transact(TransactOptions opts, Action action) {
                Db.Transact(action, 0, opts);
            }
        }

        internal static void Transact(Action action, uint flags, Advanced.TransactOptions opts) {
            int retries = 0;
            uint r;
            ulong handle;

            VerifyTransactOptions(opts);
            
            if (ThreadData.inTransactionScope_ == 0) {
                for (;;) {
                    r = sccoredb.star_create_transaction(flags, out handle);
                    if (r == 0) {
                        var currentTransaction = TransactionManager.GetCurrentAndSetToNoneManagedOnly();

                        try {
                            ThreadData.inTransactionScope_ = 1;
                            ThreadData.applyHooks_ = opts.applyHooks;

                            sccoredb.star_context_set_transaction( // Can not fail.
                                ThreadData.ContextHandle, handle
                                );

                            action();
                            TransactionManager.Commit(1).Wait();
                            return;
                        } catch (Exception ex) {
                            if (!HandleTransactException(ex, handle, ++retries, opts.maxRetries))
                                throw;
                            continue;
                        } finally {
                            Debug.Assert(ThreadData.inTransactionScope_ == 1);
                            ThreadData.inTransactionScope_ = 0;
                            ThreadData.applyHooks_ = false;
                            TransactionManager.SetCurrentTransaction(currentTransaction);
                        }
                    }
                    throw ErrorCode.ToException(r);
                }
            }

            // We already have a transaction locked on thread so we're already in a transaction
            // scope (possibly an implicit one if for example in the context of a trigger): Just
            // invoke the callback and exit.
            try {
                action();
            } catch {
                // Operation will fail only if transaction is already aborted (in which case we need
                // not abort it).
                sccoredb.star_context_external_abort(ThreadData.ContextHandle);
                throw;
            }
        }

        internal static TResult Transact<TResult>(Func<TResult> func, uint flags, Advanced.TransactOptions opts) {
            TResult r = default(TResult);
            Transact(() => { r = func(); }, flags, opts);
            return r;
        }

        internal static void SystemTransact(Action action, int maxRetries = 100) {
            Transact(action, 0, new Advanced.TransactOptions { maxRetries = maxRetries });
        }

        private static void VerifyTransactOptions(Advanced.TransactOptions opts) {
            if (opts.maxRetries < 0) {
                throw new ArgumentOutOfRangeException("maxRetries", string.Format("Valid range: 0-{0}", int.MaxValue));
            }
        }

        /// <summary>
        /// Checks the specified exception. If the exception is of type <see cref="ITransactionConflictException"/>
        /// and the number of retries is lower then max number of retries true is returned and no other action is taken.
        /// If the maximum number of retries is reached an unhandled transaction conflict is thrown.
        /// For other exception types false is returned. 
        /// </summary>
        /// <param name="ex">The catched exception.</param>
        /// <param name="handle">Handle of the transaction in use.</param>
        /// <param name="verify">Verify of the transaction in use.</param>
        /// <param name="retries">The number of times the transaction have been retried.</param>
        /// <param name="maxRetries">The maximum number of retries.</param>
        /// <returns></returns>
        private static bool HandleTransactException(Exception ex, ulong handle, int retries, int maxRetries) {
            ulong verify = ThreadData.ObjectVerify;
            uint cr = sccoredb.star_transaction_free(handle, verify);
            if (cr == 0) {
                if (ex is ITransactionConflictException) {
                    if (retries <= maxRetries)
                        return true;
                    throw ErrorCode.ToException(Error.SCERRUNHANDLEDTRANSACTCONFLICT, ex);
                }
                return false;
            }
            HandleFatalErrorInTransactionScope(cr);
            return false; // Will never be reached, but needed so that the compiler is happy.
        }
        
        public static void Scope(Action action, bool isReadOnly = false) {
            TransactionHandle transactionHandle = TransactionHandle.Invalid;
            TransactionHandle old = StarcounterBase.TransactionManager.CurrentTransaction;
            bool create = (old.handle == 0 || old.IsImplicit);
            try {
                if (create)
                    transactionHandle = TransactionManager.CreateAndSetCurrent(isReadOnly);
                action();
            } finally {
                TransactionManager.SetCurrentTransaction(old);
                if (create)
                    TransactionManager.CheckForRefOrDisposeTransaction(transactionHandle);
            }
        }

        [Obsolete]
        public static void Scope<T>(Action<T> action, T arg, bool isReadOnly = false) {
            Scope(() => action(arg), isReadOnly);
        }

        [Obsolete]
        public static void Scope<T1, T2>(Action<T1, T2> action, T1 arg1, T2 arg2, bool isReadOnly = false) {
            Scope(() => action(arg1, arg2), isReadOnly);
        }

        [Obsolete]
        public static void Scope<T1, T2, T3>(Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3, bool isReadOnly = false) {
            Scope(() => action(arg1, arg2, arg3), isReadOnly);
        }

        public static TResult Scope<TResult>(Func<TResult> func, bool isReadOnly = false) {
            TResult r = default(TResult);
            Scope(() => { r = func(); }, isReadOnly);
            return r;
        }

        [Obsolete]
        public static TResult Scope<T, TResult>(Func<T, TResult> func, T arg, bool isReadOnly = false) {
            return Scope(() => func(arg), isReadOnly);
        }

        [Obsolete]
        public static TResult Scope<T1, T2, TResult>(Func<T1, T2, TResult> func, T1 arg1, T2 arg2, bool isReadOnly = false) {
            return Scope(() => func(arg1, arg2), isReadOnly);
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

            if (ThreadData.applyHooks_)
                InvokeBeforeDeleteHooks(proxy, oid, address);

            Delete(new ObjectRef { ObjectID = oid, ETI = address });
        }

        private static void InvokeBeforeDeleteHooks(IObjectProxy proxy, ulong oid, ulong address) {
            int ir = sccoredb.star_context_set_trans_flags(
                ThreadData.ContextHandle, oid, address, sccoredb.DELETE_PENDING
                );
            if (ir != 0) {
                // Positive value contains previously set flags, negative value indicates and error.

                if (ir > 0) {
                    if ((ir & sccoredb.DELETE_PENDING) != 0) {
                        // Delete already was issued. We ignore it and just return. We are
                        // processing the delete of this object so it will be deleted eventually.
                        return;
                    }
                } else
                    throw ErrorCode.ToException((uint)(-ir));
            }

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

            ThreadData.inTransactionScope_++;
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
                if (ex is System.Threading.ThreadAbortException)
                    throw;
                throw ErrorCode.ToException(Error.SCERRERRORINHOOKCALLBACK, ex);
            } finally {
                ThreadData.inTransactionScope_--;
            }

        }

        public static void Delete(ObjectRef o) {
            uint r = sccoredb.star_context_delete(ThreadData.ContextHandle, o.ObjectID, o.ETI);
            if (r == 0)
                return;
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

        private static void HandleFatalErrorInTransactionScope(uint e) {
            ExceptionManager.HandleInternalFatalError(e);
        }
    }
}
