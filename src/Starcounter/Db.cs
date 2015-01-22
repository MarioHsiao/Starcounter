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
        /// Returns the "dynamic type" instance corresponding to the
        /// database class with the given name.
        /// </summary>
        /// <param name="name">The type name whose type are to
        /// be returned</param>
        /// <returns>The "dynamic type" instance of the given database
        /// type.</returns>
        public static Entity TypeOf(string name) {
            var td = Bindings.GetTypeDef(name);
            return (Entity)DbHelper.FromID(td.RuntimeDefaultTypeRef.ObjectID);
        }

        /// <summary>
        /// Returns the "dynamic type" instance corresponding to the
        /// database class T.
        /// </summary>
        /// <typeparam name="T">The database class whose type are to
        /// be returned</typeparam>
        /// <returns>The "dynamic type" instance of the given database
        /// class.</returns>
        public static Entity TypeOf<T>() {
            return TypeOf(typeof(T).FullName);
        }

        /// <summary>
        /// Returns the "dynamic type" instance corresponding to the
        /// database class T, where the type is represented by class
        /// T2.
        /// </summary>
        /// <typeparam name="T">The database class whose type are to
        /// be returned</typeparam>
        /// <typeparam name="T2">The class of the type.</typeparam>
        /// <returns>The "dynamic type" instance of the given database
        /// class.</returns>
        public static T2 TypeOf<T, T2>() {
            var t = typeof(T);
            return TypeOf<T2>(t.FullName);
        }

        /// <summary>
        /// Returns the "dynamic type" instance corresponding to the
        /// given typename, where the type is represented by class
        /// T.
        /// </summary>
        /// <typeparam name="T">The class of the type instance.</typeparam>
        /// <param name="name">The name of the type to retreive.</param>
        /// <returns>The "dynamic type" instance with the given name.
        /// </returns>
        public static T TypeOf<T>(string name) {
            var td = Bindings.GetTypeDef(name);
            return (T)DbHelper.FromID(td.RuntimeDefaultTypeRef.ObjectID);
        }
        
        /// <summary>
        /// Lookups the table.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>TableDef.</returns>
        public static TableDef LookupTable(string name)
        {
            unsafe
            {
                systables.STAR_TABLE_INFO tableInfo;
                var r = systables.star_get_table_info_by_name(name, out tableInfo);
                if (r == 0)
                {
                    return TableDef.ConstructTableDef(tableInfo);
                }
                if (r == Error.SCERRTABLENOTFOUND) return null;
                throw ErrorCode.ToException(r);
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
                systables.STAR_COLUMN_DEFINITION[] column_definitions = new systables.STAR_COLUMN_DEFINITION[columns.Length - implicitColumnCount + 1];
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
                    fixed (systables.STAR_COLUMN_DEFINITION* fixed_column_definitions = column_definitions)
                    {
                        uint e = systables.star_create_table(name, inheritedTableId, fixed_column_definitions, 0);
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
            uint e = systables.star_rename_table(tableId, newName);
            if (e == 0) return;
            throw ErrorCode.ToException(e);
        }

        /// <summary>
        /// Drops the table.
        /// </summary>
        /// <param name="name">The name.</param>
        public static void DropTable(string name)
        {
            uint e = systables.star_drop_table(name);
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
        public static void Transaction(Action action, bool forceSnapshot = false, int maxRetries = 100) {
            Transaction(action, 0, forceSnapshot, maxRetries);
        }

        /// <summary>
        /// System transactions is used to insert data with OIDs, e.g., for upgrade.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="forceSnapshot"></param>
        /// <param name="maxRetries"></param>
        internal static void SystemTransaction(Action action, bool forceSnapshot = false, int maxRetries = 100) {
            Transaction(action, sccoredb.MDB_TRANSCREATE_SUPPRESS_HOOKS, forceSnapshot, maxRetries);
        }

        public static void Scope(Action action, bool forceNew = false) {
            ITransaction t = Starcounter.Transaction.GetCurrent();
            if (forceNew || t == null)
                t = new Starcounter.Transaction(false, false);
            t.Add(action);
        }

        public static T Scope<T>(Func<T> func, bool forceNew = false) {
            ITransaction t = Starcounter.Transaction.GetCurrent();
            if (forceNew || t == null)
                t = new Starcounter.Transaction(false, false);
            return t.AddAndReturn<T>(func);
        }

        internal static void Transaction(Action action, uint flags, bool forceSnapshot = false, int maxRetries = 100)
        {
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

            for (; ; )
            {
                r = sccoredb.star_create_transaction_and_set_current(flags, 1, out handle, out verify);
                if (r == 0)
                {
                    var currentTransaction = Starcounter.Transaction.GetCurrent();
                    Starcounter.Transaction.SetManagedCurrentToNull();

                    try {
                        action();
                        Starcounter.Transaction.Commit(1, 1);
                        return;
                    }
                    catch (Exception ex) {
                        r = sccoredb.star_set_current_transaction(1, 0, 0);
                        if (r == 0) {
                            r = sccoredb.star_free_transaction(handle, verify);
                            if (r == 0) {
                                if (ex is ITransactionConflictException) {
                                    if (++retries <= maxRetries) continue;
                                    throw ErrorCode.ToException(Error.SCERRUNHANDLEDTRANSACTCONFLICT, ex);
                                }
                                throw;
                            }
                        }
                        HandleFatalErrorInTransactionScope(r);
                    }
                    finally {
                        if (currentTransaction != null) {
                            // There should be no current transaction and so
                            // there should be no reason setting the current
                            // transaction to the replaced one (other then the
                            // current transcation being bound to another
                            // thread).

                            Starcounter.Transaction.SetCurrent(currentTransaction);
                        } else {
                            ImplicitTransaction.CreateOrSetCurrent();
                        }
                    }
                }

                if (r == Error.SCERRTRANSACTIONLOCKEDONTHREAD) {
                    // We already have a transaction locked on thread so we're
                    // already in a transaction scope (possibly an implicit one if
                    // for example in the context of a trigger): Just invoke the
                    // callback and exit.

                    try {
                        action();
                    }
                    catch {
                        // Operation will fail only if transaction is already
                        // aborted (in which case we need not abort it).

                        sccoredb.star_external_abort();
                        throw;
                    }
                    return;
                }

                throw ErrorCode.ToException(r);
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

            var r = sccoredb.star_begin_delete(oid, address);
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

                sccoredb.star_abort_delete(oid, address);
                if (ex is System.Threading.ThreadAbortException) throw;
                throw ErrorCode.ToException(Error.SCERRERRORINHOOKCALLBACK, ex);
            }

            r = sccoredb.star_complete_delete(oid, address);
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

            var entityInterface = proxy as IEntity;
            if (entityInterface != null) {
                entityInterface.OnDelete();
            }

            //var typeBindingFlags = (proxy.TypeBinding as TypeBinding).Flags;
            //if ((typeBindingFlags & TypeBindingFlags.Callback_OnDelete) != 0) {
            //    ((IEntity)proxy).OnDelete();
            //}
        }

        private static void HandleFatalErrorInTransactionScope(uint e)
        {
            ExceptionManager.HandleInternalFatalError(e);
        }
    }
}
