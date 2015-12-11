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
            // When we get here we know there is something different between the old and the new 
            // definition, but we don't know exactly what. We need to find out what have changed:
            // 1. One or more columns removed -> No upgrade, since we keep old columns in table
            // 2. One or more columns have their type changed -> throw exception since we currently 
            //    don't support changes of type.
            // 3. One or more columns added -> call metadata layer to create new layout.

            // TODOS:
            // Verify that a baseclass have not been changed (or maybe that it has a compatible layout)


            bool exists;
            ColumnDef oldCol;
            ColumnDef newCol;
            ColumnDef[] oldCols = oldTableDef_.ColumnDefs;
            ColumnDef[] newCols = newTableDef_.ColumnDefs;
            List<ColumnDef> addedCols = new List<ColumnDef>();
            ulong newLayout = 0;
            sccoredb.STARI_LAYOUT_INFO layoutInfo;
            uint ec;
            bool doUpgrade = false;

            // Since we don't care about columns that are removed, we can loop through
            // the columns in the new table and verify that existing have the same type
            // as before and that new ones gets added.

            // The two first columns is alwyas __id and __setspec so those can be skipped.
            for (int i = 2; i < newCols.Length; i++) {
                newCol = newCols[i];

                exists = false;
                for (int j = 2; j < oldCols.Length; j++) {
                    oldCol = oldCols[j];

                    if (newCol.Name.Equals(oldCol.Name)) {
                        // Column already exists. Check that type is the same.
                        if (newCol.Type != oldCol.Type || newCol.IsNullable != oldCol.IsNullable) {
                            throw new Exception("TODO: Errorcode, column changed datatype. Column: "
                                                + newCol.Name
                                                + "(in type " + newTableDef_.Name + ")"
                                                + ", from "
                                                + ((DbTypeCode)oldCol.Type).ToString()
                                                + " to "
                                                + ((DbTypeCode)newCol.Type).ToString()
                            );
                        }
                        exists = true;
                        break;
                    }
                }

                if (!exists) {
                    doUpgrade = true;
                    if (!newCol.IsInherited) {
                        addedCols.Add(newCol);
                    }
                }
            }

            if (doUpgrade) {
                unsafe {
                    var added_column_defs = new SqlProcessor.SqlProcessor.STAR_COLUMN_DEFINITION_HIGH[addedCols.Count];

                    for (int i = 0; i < added_column_defs.Length; i++) {
                        added_column_defs[i].name = (char*)Marshal.StringToCoTaskMemUni(addedCols[i].Name);
                        added_column_defs[i].primitive_type = addedCols[i].Type;
                        added_column_defs[i].is_nullable = addedCols[i].IsNullable ? (byte)1 : (byte)0;
                    }
                 
                    fixed (SqlProcessor.SqlProcessor.STAR_COLUMN_DEFINITION_HIGH* fixed_column_defs = added_column_defs) {
                        ec = SqlProcessor.SqlProcessor.star_alter_table_add_columns(ThreadData.ContextHandle,
                                                                                         newTableDef_.Name,
                                                                                         fixed_column_defs,
                                                                                         out newLayout);
                        
                        if (ec != 0)
                            throw ErrorCode.ToException(ec);
                    }
                }

                Db.Transact(() => {
                    var metaTable = (Starcounter.Metadata.RawView)DbHelper.FromID(newLayout);
                    
                    ec = sccoredb.stari_context_get_layout_info(ThreadData.ContextHandle, metaTable.LayoutHandle, out layoutInfo);
                    if (ec != 0)
                        throw ErrorCode.ToException(ec);
                    newTableDef_ = TableDef.ConstructTableDef(layoutInfo, (uint)(oldTableDef_.allLayoutIds.Length + 1));
                });
            } else {
                // Only change was removed columns. No upgrade needed.
                newTableDef_ = oldTableDef_;
            }
            
            return newTableDef_;
        }
    }
}
