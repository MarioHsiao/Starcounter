// ***********************************************************************
// <copyright file="TableCreate.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

namespace Starcounter.Binding {
    /// <summary>
    /// 
    /// </summary>
    public class TableCreate {
        private readonly TableDef tableDef_;

        /// <summary>
        /// Initializes a new instance of the <see cref="TableCreate" /> class.
        /// </summary>
        /// <param name="tableDef">The definition for the table to create</param>
        public TableCreate(TableDef tableDef) {
            tableDef_ = tableDef;
        }

        /// <summary>
        /// Evaluates and creates the table.
        /// </summary>
        /// <returns>The created table</returns>
        public TableDef Eval() {
            TableDef tableDef = tableDef_;
            TableDef inheritedTableDef = null;

            if (tableDef.BaseName != null) {
                Db.Transact(() => {
                    inheritedTableDef = Db.LookupTable(tableDef.BaseName);
                });

                if (inheritedTableDef == null) {
                    // TODO: Base table does not exist. Should not happen.
                    throw ErrorCode.ToException(Error.SCERRUNSPECIFIED);
                }
            }

            // TODO:
            // Check that the first columns of the table definition matches
            // that of the inherited table. Do this in Db.CreateTable?

            Db.CreateTable(tableDef, inheritedTableDef);

            TableDef newTableDef = null;

            Db.Transact(() => {
                newTableDef = Db.LookupTable(tableDef.Name);
            });

            return newTableDef;
        }
    }
}
