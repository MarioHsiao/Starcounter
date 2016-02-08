// ***********************************************************************
// <copyright file="TableDef.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Query.Execution;
using Starcounter.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Starcounter.Binding {
    /// <summary>
    /// Definition of a database table.
    /// </summary>
    public sealed class TableDef {
        private const string COLUMN_NAME_QUERY = @"SELECT c.Name FROM Starcounter.Metadata.Column c WHERE c.""Table"".FullName=? AND c.Name=?";

        /// <summary>
        /// </summary>
        internal unsafe static TableDef ConstructTableDef(sccoredb.STARI_LAYOUT_INFO tableInfo, uint layoutInfoCount, bool resolveColumnNames) {
            string name = SqlProcessor.SqlProcessor.GetNameFromToken(tableInfo.token);
            ushort tableId = tableInfo.layout_handle;
            uint columnCount = tableInfo.column_count;
            string baseName = null;
            uint inheritedColumnCount = 0;
            uint ec;
            sccoredb.STARI_LAYOUT_INFO[] layoutInfos = null;
            ushort[] allLayoutIds = null;
            
            if (layoutInfoCount == 1) {
                allLayoutIds = new ushort[1];
                allLayoutIds[0] = tableInfo.layout_handle;
            } else if (layoutInfoCount > 1) {
                // TODO:
                // Additional layouts exists, due to schema upgrades. 
                // Not sure how to handle this. Is it enough to get and keep all layouts, or
                // do we need to create additional TableDefs for each layout.
                
                ulong token = SqlProcessor.SqlProcessor.GetTokenFromName(name);
                if (token != 0) {
                    layoutInfos = new sccoredb.STARI_LAYOUT_INFO[layoutInfoCount];
                    fixed (sccoredb.STARI_LAYOUT_INFO* pLayoutInfos = layoutInfos)
                    {
                        ec = sccoredb.stari_context_get_layout_infos_by_token(
                                ThreadData.ContextHandle, token, &layoutInfoCount, pLayoutInfos
                             );
                    }
                    if (ec != 0)
                        throw ErrorCode.ToException(ec);

                    allLayoutIds = new ushort[layoutInfoCount];
                    for (int i = 0; i < layoutInfoCount; i++) {
                        allLayoutIds[i] = layoutInfos[i].layout_handle;
                    }
                }
            }

            // All layouts is inherits from the base layout created by the metadata layer. This
            // layout is however not represented by a class or a table so we treat this as no base
            // class.

            Debug.Assert(tableInfo.inherited_layout_handle != 0);
            if (tableInfo.inherited_layout_handle != 0) {
                var r = sccoredb.stari_context_get_layout_info(ThreadData.ContextHandle, tableInfo.inherited_layout_handle, out tableInfo);
                if (r == 0) {
                    if (tableInfo.token != SqlProcessor.SqlProcessor.STAR_MOM_OF_ALL_LAYOUTS_NAME_TOKEN) {
                        baseName = SqlProcessor.SqlProcessor.GetNameFromToken(tableInfo.token);
                        inheritedColumnCount = tableInfo.column_count;
                    }
                } else {
                    throw ErrorCode.ToException(r);
                }
            }

            ColumnDef[] columns = new ColumnDef[columnCount];
            for (ushort i = 0; i < columns.Length; i++) {
                sccoredb.STARI_COLUMN_INFO columnInfo;
                var r = sccoredb.stari_context_get_column_info(ThreadData.ContextHandle, tableId, i, out columnInfo);
                if (r == 0) {
                    // The string retrieved from token does not case about case, so we need to 
                    // retrieve the correct name from Column metadata.
                    string colName = SqlProcessor.SqlProcessor.GetNameFromToken(columnInfo.token);
                    if (resolveColumnNames)
                        colName = Db.SQL<string>(COLUMN_NAME_QUERY, name, colName).First;
                    
                    columns[i] = new ColumnDef(
                        colName,
                        columnInfo.type,
                        columnInfo.nullable != 0,
                        i < inheritedColumnCount
                        );
                } else {
                    throw ErrorCode.ToException(r);
                }
            }

            var tableDef = new TableDef(name, baseName, columns, tableId);
            tableDef.allLayoutIds = allLayoutIds;

            return tableDef;
        }
        
        /// <summary>
        /// The name
        /// </summary>
        public string Name;

        /// <summary>
        /// The base name
        /// </summary>
        public string BaseName;

        /// <summary>
        /// The column defs
        /// </summary>
        private ColumnDef[] _ColumnDefs;
        public ColumnDef[] ColumnDefs {
            get {
                Debug.Assert(_ColumnDefs != null);
                return _ColumnDefs;
            }
            internal set { _ColumnDefs = value; }
        }

        /// <summary>
        /// The table id
        /// </summary>
        public ushort TableId;

        /// <summary>
        /// Contains all different layout used for this table. 
        /// </summary>
        internal ushort[] allLayoutIds;

        /// <summary>
        /// Initializes a new instance of the <see cref="TableDef" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="columnsDefs">The columns defs.</param>
        public TableDef(string name, ColumnDef[] columnsDefs) : this(name, null, columnsDefs, 0xFFFF) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TableDef" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="baseName">Name of the base.</param>
        /// <param name="columnsDefs">The columns defs.</param>
        public TableDef(string name, string baseName, ColumnDef[] columnsDefs) : this(name, baseName, columnsDefs, 0xFFFF) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TableDef" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="baseName">Name of the base.</param>
        /// <param name="columnsDefs">The columns defs.</param>
        /// <param name="tableId">The table id.</param>
        public TableDef(string name, string baseName, ColumnDef[] columnsDefs, ushort tableId) {
            Name = name;
            BaseName = baseName;
            ColumnDefs = columnsDefs;

            TableId = tableId;
        }

        /// <summary>
        /// Gets the short name.
        /// </summary>
        /// <value>The short name.</value>
        public string ShortName {
            get {
                var i = Name.LastIndexOf('.');
                if (i >= 0) {
                    return Name.Substring(i + 1);
                }
                return Name;
            }
        }

        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns>TableDef.</returns>
        public TableDef Clone() {
            ColumnDef[] clonedColumnDefs = new ColumnDef[ColumnDefs.Length];
            for (int i = 0; i < ColumnDefs.Length; i++) {
                clonedColumnDefs[i] = ColumnDefs[i].Clone();
            }
            return new TableDef(Name, BaseName, clonedColumnDefs, TableId);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tableDef"></param>
        /// <returns></returns>
        public bool Equals(TableDef tableDef) {
            bool b =
                Name.Equals(tableDef.Name, StringComparison.InvariantCultureIgnoreCase)
                  && (BaseName == tableDef.BaseName 
                       || ((tableDef.BaseName != null) 
                             && tableDef.BaseName.Equals("Starcounter.Metadata.MetadataEntity", StringComparison.InvariantCultureIgnoreCase)
                          )
                     )
                  && ColumnDefs.Length == tableDef.ColumnDefs.Length;
            if (b) {
                for (int i = 0; i < ColumnDefs.Length; i++) {
                    b = ColumnDefs[i].Equals(tableDef.ColumnDefs[i]);
                    if (!b)
                        break;
                }
            }
            return b;
        }

        /// <summary>
        /// Gets all index infos.
        /// </summary>
        /// <returns>IndexInfo[][].</returns>
        internal IndexInfo[] GetAllIndexInfos() {
            uint ec;
            uint ic;
            sccoredb.STARI_INDEX_INFO[] iis;
            IndexInfo[] iil;

            unsafe
            {
                string setspec = Starcounter.SqlProcessor.SqlProcessor.GetSetSpecifier(TableId);
                ec = sccoredb.stari_context_get_index_infos_by_setspec(
                    ThreadData.ContextHandle, setspec, sccoredb.STAR_EXCLUDE_INHERITED, &ic, null
                    );
                if (ec != 0) {
                    throw ErrorCode.ToException(ec);
                }
                if (ic == 0) {
                    return new IndexInfo[0];
                }

                iis = new sccoredb.STARI_INDEX_INFO[ic];
                fixed (sccoredb.STARI_INDEX_INFO* pii = &(iis[0]))
                {
                    ec = sccoredb.stari_context_get_index_infos_by_setspec(
                        ThreadData.ContextHandle, setspec, sccoredb.STAR_EXCLUDE_INHERITED, &ic, pii
                        );
                    if (ec != 0) {
                        throw ErrorCode.ToException(ec);
                    }

                    iil = new IndexInfo[ic];
                    for (int i = 0; i < ic; i++) {
                        iil[i] = CreateIndexInfo(pii + i);
                    }
                    return iil;
                }
            }
        }

        /// <summary>
        /// </summary>
        internal IndexInfo GetIndexInfo(string name) {
            unsafe
            {
                ulong token = SqlProcessor.SqlProcessor.GetTokenFromName(name);
                if (token != 0) {
                    var indexInfos = GetAllIndexInfos();
                    for (var i = 0; i < indexInfos.Length; i++) {
                        if (indexInfos[i].Token == token) {
                            return indexInfos[i];
                        }
                    }
                }
                return null;
            }
        }

        internal unsafe IndexInfo CreateIndexInfo(sccoredb.STARI_INDEX_INFO* pii) {
            string name;
            short attributeCount;
            ushort tempSortMask;
            SortOrder[] sortOrderings;
            int[] columnIndexes;
            ColumnDef[] columnDefs;
            ulong token;

            token = pii->token;
            name = Starcounter.SqlProcessor.SqlProcessor.GetNameFromToken(token);
            // Get the number of attributes.
            attributeCount = pii->attributeCount;
            if (attributeCount < 1 || attributeCount > 10) {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect attributeCount.");
            }
            // Get the sort orderings.
            sortOrderings = new SortOrder[attributeCount];
            tempSortMask = pii->sortMask;
            for (Int32 j = 0; j < attributeCount; j++) {
                if ((tempSortMask & 1) == 1) {
                    sortOrderings[j] = SortOrder.Descending;
                } else {
                    sortOrderings[j] = SortOrder.Ascending;
                }
                tempSortMask = (UInt16)(tempSortMask >> 1);
            }
            // Get the column definitions.
            columnIndexes = new int[attributeCount];
            columnDefs = new ColumnDef[attributeCount];
            for (Int32 j = 0; j < attributeCount; j++) {
                switch (j) {
                    case 0:
                        columnIndexes[j] = pii->attrIndexArr_0;
                        columnDefs[j] = ColumnDefs[pii->attrIndexArr_0];
                        break;
                    case 1:
                        columnIndexes[j] = pii->attrIndexArr_1;
                        columnDefs[j] = ColumnDefs[pii->attrIndexArr_1];
                        break;
                    case 2:
                        columnIndexes[j] = pii->attrIndexArr_2;
                        columnDefs[j] = ColumnDefs[pii->attrIndexArr_2];
                        break;
                    case 3:
                        columnIndexes[j] = pii->attrIndexArr_3;
                        columnDefs[j] = ColumnDefs[pii->attrIndexArr_3];
                        break;
                    case 4:
                        columnIndexes[j] = pii->attrIndexArr_4;
                        columnDefs[j] = ColumnDefs[pii->attrIndexArr_4];
                        break;
                    case 5:
                        columnIndexes[j] = pii->attrIndexArr_5;
                        columnDefs[j] = ColumnDefs[pii->attrIndexArr_5];
                        break;
                    case 6:
                        columnIndexes[j] = pii->attrIndexArr_6;
                        columnDefs[j] = ColumnDefs[pii->attrIndexArr_6];
                        break;
                    case 7:
                        columnIndexes[j] = pii->attrIndexArr_7;
                        columnDefs[j] = ColumnDefs[pii->attrIndexArr_7];
                        break;
                    case 8:
                        columnIndexes[j] = pii->attrIndexArr_8;
                        columnDefs[j] = ColumnDefs[pii->attrIndexArr_8];
                        break;
                    case 9:
                        columnIndexes[j] = pii->attrIndexArr_9;
                        columnDefs[j] = ColumnDefs[pii->attrIndexArr_9];
                        break;
                    case 10:
                        columnIndexes[j] = pii->attrIndexArr_10;
                        columnDefs[j] = ColumnDefs[pii->attrIndexArr_10];
                        break;
                }
            }
            return new IndexInfo(
                pii->handle, TableId, token, name, columnIndexes, columnDefs, sortOrderings
                );
        }
    }
}
