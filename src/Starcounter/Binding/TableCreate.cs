// ***********************************************************************
// <copyright file="TableCreate.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

namespace Starcounter.Binding
{

    /// <summary>
    /// Class TableCreate
    /// </summary>
    public class TableCreate
    {

        /// <summary>
        /// The table def_
        /// </summary>
        private readonly TableDef tableDef_;

        /// <summary>
        /// Initializes a new instance of the <see cref="TableCreate" /> class.
        /// </summary>
        /// <param name="tableDef">The table def.</param>
        public TableCreate(TableDef tableDef)
        {
            tableDef_ = tableDef;
        }

        /// <summary>
        /// Evals this instance.
        /// </summary>
        /// <returns>TableDef.</returns>
        public TableDef Eval()
        {
            TableDef tableDef = tableDef_;
            TableDef inheritedTableDef = null;

            if (tableDef.BaseName != null)
            {
                Db.Transaction(() =>
                {
                    inheritedTableDef = Db.LookupTable(tableDef.BaseName);
                });

                if (inheritedTableDef == null)
                {
                    // TODO: Base table does not exist. Should not happen.
                    throw ErrorCode.ToException(Error.SCERRUNSPECIFIED);
                }
            }

            // TODO:
            // Check that the first columns of the table definition matches
            // that of the inherited table. Do this in Db.CreateTable?

            Db.CreateTable(tableDef, inheritedTableDef);

            TableDef newTableDef = null;

            Db.Transaction(() =>
            {
                newTableDef = Db.LookupTable(tableDef.Name);
            });

#if true
            short columnIndex = newTableDef.GetFirstIndexableColumnIndex();
            if (columnIndex != -1) {
                unsafe {
                    short* column_indexes = stackalloc short[2];
                    column_indexes[0] = columnIndex;
                    column_indexes[1] = -1;
                    var r = Starcounter.Internal.sccoredb.sccoredb_create_index(newTableDef.TableId, "auto", 0, column_indexes, 0);
                    if (r != 0)
                        throw ErrorCode.ToException(r);
                }
            }
#endif

            return newTableDef;
        }
    }
}
