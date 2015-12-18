// ***********************************************************************
// <copyright file="TableUpgrade.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Internal;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Starcounter.Binding {
    /// <summary>
    ///
    /// </summary>
    public class TableUpgrade {
        private delegate void RecordHandler(ObjectRef obj);
        private readonly string tableName_;
        private readonly TableDef oldTableDef_;
        private TableDef newTableDef_;

        /// <summary>
        /// Initializes a new instance of the <see cref="TableUpgrade" /> class.
        /// </summary>
        /// <param name="tableName">Name of the table to upgrade.</param>
        /// <param name="oldTableDef">The existing definition of the table.</param>
        /// <param name="newTableDef">The new definition of the table.</param>
        public TableUpgrade(string tableName, TableDef oldTableDef, TableDef newTableDef) {
            tableName_ = tableName;
            oldTableDef_ = oldTableDef;
            newTableDef_ = newTableDef;
        }

        /// <summary>
        /// Evaluates and upgrades the table.
        /// </summary>
        /// <returns>The new table definition.</returns>
        public TableDef Eval() {
            bool exists;
            ColumnDef newCol;
            ColumnDef oldCol;
            ColumnDef[] newCols;
            ColumnDef[] oldCols;
            List<ColumnDef> addedCols = null;
            uint ec;
            ulong newLayoutHandle = 0;

//            System.Diagnostics.Debugger.Launch();

            // The following things are checked when evaluating::
            // 1. One or more columns removed -> No upgrade, since we keep old columns in table.
            // 2. One or more columns have their type changed -> throw exception since we currently 
            //    don't support type conversion.
            // 3. One or more columns added -> call metadata layer to create new layout.
            // 4. Inheritance have changed -> Not supported, throw exception.
            
            VerifyTableInheritance(oldTableDef_, newTableDef_);
            
            // Since we don't care about columns that are removed, we can loop through
            // the columns in the new table and verify that existing have the same type
            // as before and that new ones gets added.            
            newCols = newTableDef_.ColumnDefs;
            oldCols = oldTableDef_.ColumnDefs;

            // The two first columns is alwyas __id and __setspec so those can be skipped.
            for (int i = 2; i < newCols.Length; i++) {
                newCol = newCols[i];

                if (newCol.IsInherited)
                    continue;

                exists = false;
                for (int j = 2; j < oldCols.Length; j++) {
                    oldCol = oldCols[j];

                    if (newCol.Name.Equals(oldCol.Name)) {
                        VerifyColumnType(oldCol, newCol);
                        exists = true;
                        break;
                    }
                }

                if (!exists) {
                    if (addedCols == null)
                         addedCols = new List<ColumnDef>();
                    addedCols.Add(newCol);
                }
            }

            if (addedCols != null) {
                unsafe {
                    var added_column_defs = new SqlProcessor.SqlProcessor.STAR_COLUMN_DEFINITION_HIGH[addedCols.Count + 1];

                    for (int i = 0; i < addedCols.Count; i++) {
                        added_column_defs[i].name = (char*)Marshal.StringToCoTaskMemUni(addedCols[i].Name);
                        added_column_defs[i].primitive_type = addedCols[i].Type;
                        added_column_defs[i].is_nullable = addedCols[i].IsNullable ? (byte)1 : (byte)0;
                    }
                    added_column_defs[added_column_defs.Length - 1].primitive_type = 0; // terminate the list.

                    fixed (SqlProcessor.SqlProcessor.STAR_COLUMN_DEFINITION_HIGH* fixed_column_defs = added_column_defs) {
                        ec = SqlProcessor.SqlProcessor.star_alter_table_add_columns(
                                                                ThreadData.ContextHandle,
                                                                newTableDef_.Name,
                                                                fixed_column_defs,
                                                                out newLayoutHandle
                                                        );
                        if (ec != 0) 
                            throw ErrorCode.ToException(ec);
                    }
                }

                Db.Transact(() => {
                    sccoredb.STARI_LAYOUT_INFO layoutInfo;
                    ec = sccoredb.stari_context_get_layout_info(ThreadData.ContextHandle, (ushort)newLayoutHandle, out layoutInfo);
                    if (ec != 0)
                        throw ErrorCode.ToException(ec);

                    newTableDef_ = TableDef.ConstructTableDef(layoutInfo, (uint)(oldTableDef_.allLayoutIds.Length + 1), true);
                });
            } else {
                // Only change was removed columns or updates in a basetable. No upgrade needed.
                Db.Transact(() => {
                    newTableDef_ = Db.LookupTable(newTableDef_.Name);
                });
            }

            return newTableDef_;
        }

        private void VerifyColumnType(ColumnDef oldCol, ColumnDef newCol) {
            if (newCol.Type != oldCol.Type || newCol.IsNullable != oldCol.IsNullable) {
                throw ErrorCode.ToException(Error.SCERRFIELDSIGNATUREDEVIATION,
                                            string.Format("Property/field '{0}' in class '{1}' changed type from '{2}' to '{3}'.",
                                                newCol.Name, 
                                                newTableDef_.Name,
                                                BindingHelper.ConvertScTypeCodeToDbTypeCode(oldCol.Type),
                                                BindingHelper.ConvertScTypeCodeToDbTypeCode(newCol.Type)
                                            ));
            }
        }

        private void VerifyTableInheritance(TableDef oldTableDef, TableDef newTableDef) {
            bool throwEx = false;

            if (newTableDef.BaseName != null) {
                if (oldTableDef.BaseName == null) {
                    throwEx = true;
                } else if (!newTableDef.BaseName.Equals(oldTableDef.BaseName)) {
                    throwEx = true;
                }
            } else if (oldTableDef.BaseName != null) {
                throwEx = true;
            }
            
            if (throwEx) {
                throw ErrorCode.ToException(Error.SCERRTYPEBASEDEVIATION,
                                            string.Format("Class '{0}' changed inheritance from '{1}' to '{2}'.",
                                                newTableDef.Name, oldTableDef.BaseName, newTableDef.BaseName
                                            ));
            }
        }
    }
}
