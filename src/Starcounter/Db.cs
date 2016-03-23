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

namespace Starcounter {
    public static partial class Db {
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
                sccoredb.SCCOREDB_TABLE_INFO tableInfo;
                var r = sccoredb.sccoredb_get_table_info_by_name(name, out tableInfo);
                if (r == 0)
                {
                    return TableDef.ConstructTableDef(tableInfo);
                }
                if (r == Error.SCERRTABLENOTFOUND) return null;
                throw ErrorCode.ToException(sccoredb.star_get_last_error());
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
                ushort inheritedTableId = UInt16.MaxValue;
                if (inheritedTableDef == null)
                {
                    implicitColumnCount = 1; // Implicit key column.
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
                sccoredb.SC_COLUMN_DEFINITION[] column_definitions = new sccoredb.SC_COLUMN_DEFINITION[columns.Length - implicitColumnCount + 1];
                Debug.Assert(column_definitions.Length > 0);
                char* name = null;
                try
                {
                    for (int cc = column_definitions.Length - 1, ci = implicitColumnCount, di = 0; di < cc; ci++, di++)
                    {
                        column_definitions[di].name = (char *)Marshal.StringToCoTaskMemUni(columns[ci].Name);
                        column_definitions[di].type = columns[ci].Type;
                        column_definitions[di].is_nullable = columns[ci].IsNullable ? (byte)1 : (byte)0;
                    }
                    name = (char*)Marshal.StringToCoTaskMemUni(tableDef.Name);
                    fixed (sccoredb.SC_COLUMN_DEFINITION* fixed_column_definitions = column_definitions)
                    {
                        uint e = sccoredb.star_create_table(0, name, inheritedTableId, fixed_column_definitions, 0);
                        if (e != 0) throw ErrorCode.ToException(e);
                    }
                }
                finally
                {
                    if (name != null) Marshal.FreeCoTaskMem((IntPtr)name);
                    for (int i = 0; i < column_definitions.Length; i++)
                    {
                        if (column_definitions[i].name != null)
                            Marshal.FreeCoTaskMem((IntPtr)column_definitions[i].name);
                    }
                }
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

        /// <summary>
        /// Executes the given <paramref name="action"/> within a new transaction.
        /// </summary>
        /// <typeparam name="T">The type of the parameter for the action.</typeparam>
        /// <param name="action">The action to execute.</param>
        /// <param name="arg">Parameter to use as input to the action.</param>
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
        public static void Transact<T>(Action<T> action, T arg, bool forceSnapshot = false, int maxRetries = 100) {
            int retries = 0;
            uint r;
            ulong handle;
            ulong verify;
            
            VerifyTransactOptions(forceSnapshot, maxRetries);
            for (;;) {
                r = sccoredb.sccoredb_create_transaction_and_set_current(0, 1, out handle, out verify);
                if (r == 0) {
                    var currentTransaction = TransactionManager.GetCurrentAndSetToNoneManagedOnly();

                    try {
                        action(arg);
                        TransactionManager.Commit(1, 1);
                        return;
                    } catch (Exception ex) {
                        if (!HandleTransactException(ex, handle, verify, ++retries, maxRetries))
                            throw;
                        continue;
                    } finally {
                        TransactionManager.SetCurrentTransaction(currentTransaction);
                    }
                }

                if (r == Error.SCERRTRANSACTIONLOCKEDONTHREAD) {
                    try {
                        action(arg);
                    } catch {
                        sccoredb.sccoredb_external_abort();
                        throw;
                    }
                    return;
                }
                throw ErrorCode.ToException(r);
            }
        }
      
        /// <summary>
        /// Executes the given <paramref name="func"/> within a new transaction.
        /// </summary>
        /// <typeparam name="TResult">The type of the return value of the func.</typeparam>
        /// <param name="func">The func to execute.</param>
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
        /// <returns>The return value of the func.</returns>
        public static TResult Transact<TResult>(Func<TResult> func, bool forceSnapshot = false, int maxRetries = 100) {
            int retries = 0;
            uint r;
            ulong handle;
            ulong verify;
            
            VerifyTransactOptions(forceSnapshot, maxRetries);
            for (;;) {
                r = sccoredb.sccoredb_create_transaction_and_set_current(0, 1, out handle, out verify);
                if (r == 0) {
                    var currentTransaction = TransactionManager.GetCurrentAndSetToNoneManagedOnly();

                    try {
                        TResult result = func();
                        TransactionManager.Commit(1, 1);
                        return result;
                    } catch (Exception ex) {
                        if (!HandleTransactException(ex, handle, verify, ++retries, maxRetries))
                            throw;
                        continue;
                    } finally {
                        TransactionManager.SetCurrentTransaction(currentTransaction);
                    }
                }

                if (r == Error.SCERRTRANSACTIONLOCKEDONTHREAD) {
                    try {
                        return func();
                    } catch {
                        sccoredb.sccoredb_external_abort();
                        throw;
                    }
                }
                throw ErrorCode.ToException(r);
            }
        }

        /// <summary>
        /// Executes the given <paramref name="func"/> within a new transaction.
        /// </summary>
        /// <typeparam name="TResult">The type of the return value of the func.</typeparam>
        /// <typeparam name="T">The type of the parameter of the func.</typeparam>
        /// <param name="func">The func to execute.</param>
        /// <param name="arg">Parameter to use as input to the func</param>
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
        /// <returns>The return value of the func.</returns>
        public static TResult Transact<T, TResult>(Func<T, TResult> func, T arg, bool forceSnapshot = false, int maxRetries = 100) {
            int retries = 0;
            uint r;
            ulong handle;
            ulong verify;

            VerifyTransactOptions(forceSnapshot, maxRetries);
            for (;;) {
                r = sccoredb.sccoredb_create_transaction_and_set_current(0, 1, out handle, out verify);
                if (r == 0) {
                    var currentTransaction = TransactionManager.GetCurrentAndSetToNoneManagedOnly();

                    try {
                        TResult result = func(arg);
                        TransactionManager.Commit(1, 1);
                        return result;
                    } catch (Exception ex) {
                        if (!HandleTransactException(ex, handle, verify, ++retries, maxRetries))
                            throw;
                        continue;
                    } finally {
                        TransactionManager.SetCurrentTransaction(currentTransaction);
                    }
                }

                if (r == Error.SCERRTRANSACTIONLOCKEDONTHREAD) {
                    try {
                        return func(arg);
                    } catch {
                        sccoredb.sccoredb_external_abort();
                        throw;
                    }
                }
                throw ErrorCode.ToException(r);
            }
        }
 
        internal static void Transact(Action action, uint flags, bool forceSnapshot = false, int maxRetries = 100) {
            int retries;
            uint r;
            ulong handle;
            ulong verify;

            VerifyTransactOptions(forceSnapshot, maxRetries);

            retries = 0;
            for (; ; ) {
                r = sccoredb.sccoredb_create_transaction_and_set_current(flags, 1, out handle, out verify);
                if (r == 0) {
                    // We only set the handle to none here, since Transaction.Current will follow this value.
                    var currentTransaction = TransactionManager.GetCurrentAndSetToNoneManagedOnly();

                    try {
                        action();
                        TransactionManager.Commit(1, 1);
                        return;
                    } catch (Exception ex) {
                        if (!HandleTransactException(ex, handle, verify, ++retries, maxRetries))
                            throw;
                        continue;
                    } finally {
                        TransactionManager.SetCurrentTransaction(currentTransaction);
                    }
                }

                if (r == Error.SCERRTRANSACTIONLOCKEDONTHREAD) {
                    // We already have a transaction locked on thread so we're already in a transaction scope (possibly 
                    // an implicit one if for example in the context of a trigger): Just invoke the callback and exit.
                    try {
                        action();
                    } catch {
                        // Operation will fail only if transaction is already aborted (in which case we need not abort it).
                        sccoredb.sccoredb_external_abort();
                        throw;
                    }
                    return;
                }
                throw ErrorCode.ToException(r);
            }
        }

        internal static void SystemTransact(Action action, bool forceSnapshot = false, int maxRetries = 100) {
            Transact(action, sccoredb.MDB_TRANSCREATE_SYSTEM_PRIVILEGES, forceSnapshot, maxRetries);
        }

        private static void VerifyTransactOptions(bool forceSnapshot, int maxRetries) {
            if (maxRetries < 0) {
                throw new ArgumentOutOfRangeException("maxRetries", string.Format("Valid range: 0-{0}", int.MaxValue));
            }

            if (forceSnapshot) {
                throw ErrorCode.ToException(Error.SCERRNOTIMPLEMENTED, "Forcing snapshot isolation is not yet implemented.");
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
        private static bool HandleTransactException(Exception ex, ulong handle, ulong verify, int retries, int maxRetries) {
            if (sccoredb.sccoredb_set_current_transaction(1, 0, 0) == 0 &&
                sccoredb.sccoredb_free_transaction(handle, verify) == 0) {
                if (ex is ITransactionConflictException) {
                    if (retries <= maxRetries)
                        return true;
                    throw ErrorCode.ToException(Error.SCERRUNHANDLEDTRANSACTCONFLICT, ex);
                }
                return false;
            }
            HandleFatalErrorInTransactionScope();
            return false;
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

            var r = sccoredb.sccoredb_begin_delete(oid, address);
            if (r != 0) {
                // If the error is because the delete already was issued then
                // we ignore it and just return. We are processing the delete
                // of this object so it will be deleted eventually.

                if (r == Error.SCERRDELETEPENDING) return;

                throw ErrorCode.ToException(r);
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

                sccoredb.sccoredb_abort_delete(oid, address);
                if (ex is System.Threading.ThreadAbortException) throw;
                throw ErrorCode.ToException(Error.SCERRERRORINHOOKCALLBACK, ex);
            }

            r = sccoredb.sccoredb_complete_delete(oid, address);
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

        private static void HandleFatalErrorInTransactionScope()
        {
            uint e = sccoredb.star_get_last_error();
            ExceptionManager.HandleInternalFatalError(e);
        }
    }
}
