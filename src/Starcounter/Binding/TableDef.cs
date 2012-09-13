
using Sc.Server.Binding;
using Starcounter.Query.Execution;
using Starcounter.Internal;
using System;
using System.Text;

namespace Starcounter.Binding
{

    /// <summary>
    /// Definition of a database table.
    /// </summary>
    public sealed class TableDef
    {

        internal unsafe static TableDef ConstructTableDef(ulong definitionAddr, sccoredb.Mdb_DefinitionInfo definitionInfo)
        {
            string name = new String(definitionInfo.table_name);
            ushort tableId = definitionInfo.table_id;
            uint columnCount = definitionInfo.column_count;
            string baseName = null;

            int b = 1;

            if (definitionInfo.inherited_definition_addr != sccoredb.INVALID_DEFINITION_ADDR)
            {
                b = sccoredb.Mdb_DefinitionToDefinitionInfo(definitionInfo.inherited_definition_addr, out definitionInfo);
                if (b != 0)
                {
                    baseName = new String(definitionInfo.table_name);
                }
            }

            if (b != 0)
            {
                ColumnDef[] columns = new ColumnDef[columnCount];
                for (ushort i = 0; i < columns.Length; i++)
                {
                    sccoredb.Mdb_AttributeInfo attributeInfo;
                    b = sccoredb.Mdb_DefinitionAttributeIndexToInfo(definitionAddr, i, out attributeInfo);
                    if (b != 0)
                    {
                        columns[i] = new ColumnDef(
                            new string(attributeInfo.PtrName),
                            BindingHelper.ConvertScTypeCodeToDbTypeCode(attributeInfo.Type),
                            (attributeInfo.Flags & sccoredb.MDB_ATTRFLAG_NULLABLE) != 0,
                            (attributeInfo.Flags & sccoredb.MDB_ATTRFLAG_DERIVED) != 0
                            );
                    }
                    else
                    {
                        throw ErrorCode.ToException(sccoredb.Mdb_GetLastError());
                    }
                }
                return new TableDef(name, baseName, columns, tableId, definitionAddr);
            }

            throw ErrorCode.ToException(sccoredb.Mdb_GetLastError());
        }

        public string Name;

        public string BaseName;

        public ColumnDef[] ColumnDefs;

        public ushort TableId;

        public ulong DefinitionAddr;

        public TableDef(string name, ColumnDef[] columnsDefs) : this(name, null, columnsDefs, 0xFFFF, sccoredb.INVALID_DEFINITION_ADDR) { }

        public TableDef(string name, string baseName, ColumnDef[] columnsDefs) : this(name, baseName, columnsDefs, 0xFFFF, sccoredb.INVALID_DEFINITION_ADDR) { }

        public TableDef(string name, string baseName, ColumnDef[] columnsDefs, ushort tableId, ulong definitionAddr)
        {
            Name = name;
            BaseName = baseName;
            ColumnDefs = columnsDefs;

            TableId = tableId;
            DefinitionAddr = definitionAddr;
        }

        public TableDef Clone()
        {
            ColumnDef[] clonedColumnDefs = new ColumnDef[ColumnDefs.Length];
            for (int i = 0; i < ColumnDefs.Length; i++)
            {
                clonedColumnDefs[i] = ColumnDefs[i].Clone();
            }
            return new TableDef(Name, BaseName, clonedColumnDefs, TableId, DefinitionAddr);
        }

        public bool Equals(TableDef tableDef)
        {
            bool b =
                Name == tableDef.Name &&
                BaseName == tableDef.BaseName &&
                ColumnDefs.Length == tableDef.ColumnDefs.Length
                ;
            if (b)
            {
                for (int i = 0; i < ColumnDefs.Length; i++)
                {
                    b = ColumnDefs[i].Equals(tableDef.ColumnDefs[i]);
                    if (!b) break;
                }
            }
            return b;
        }

        internal IndexInfo[] GetAllIndexInfos()
        {
            UInt32 ec;
            UInt32 ic;
            sccoredb.SC_INDEX_INFO[] iis;
            IndexInfo[] iil;
            Int32 i;
            String name;
            Int16 attributeCount;
            UInt16 tempSortMask;
            SortOrder[] sortOrderings;
            ColumnDef[] columnDefs;

            unsafe
            {
                ec = sccoredb.SCSchemaGetIndexes(
                    DefinitionAddr,
                    &ic,
                    null
                );

                if (ec != 0)
                {
                    throw ErrorCode.ToException(ec);
                }

                if (ic == 0)
                {
                    return new IndexInfo[0];
                }

                iis = new sccoredb.SC_INDEX_INFO[ic];

                fixed (sccoredb.SC_INDEX_INFO* p = &(iis[0]))
                {
                    ec = sccoredb.SCSchemaGetIndexes(
                        DefinitionAddr,
                        &ic,
                        p
                    );
                }

                if (ec != 0)
                {
                    throw ErrorCode.ToException(ec);
                }

                iil = new IndexInfo[ic];

                for (i = 0; i < ic; i++)
                {
                    name = new String(iis[i].name);
                    // Get the number of attributes.
                    attributeCount = iis[i].attributeCount;
                    if (attributeCount < 1 || attributeCount > 10)
                    {
                        throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect attributeCount.");
                    }
                    // Get the sort orderings.
                    sortOrderings = new SortOrder[attributeCount];
                    tempSortMask = iis[i].sortMask;
                    for (Int32 j = 0; j < attributeCount; j++)
                    {
                        if ((tempSortMask & 1) == 1)
                        {
                            sortOrderings[j] = SortOrder.Descending;
                        }
                        else
                        {
                            sortOrderings[j] = SortOrder.Ascending;
                        }
                        tempSortMask = (UInt16)(tempSortMask >> 1);
                    }
                    // Get the column definitions.
                    columnDefs = new ColumnDef[attributeCount];
                    for (Int32 j = 0; j < attributeCount; j++)
                    {
                        switch (j)
                        {
                            case 0:
                                columnDefs[j] = ColumnDefs[iis[i].attrIndexArr_0];
                                break;
                            case 1:
                                columnDefs[j] = ColumnDefs[iis[i].attrIndexArr_1];
                                break;
                            case 2:
                                columnDefs[j] = ColumnDefs[iis[i].attrIndexArr_2];
                                break;
                            case 3:
                                columnDefs[j] = ColumnDefs[iis[i].attrIndexArr_3];
                                break;
                            case 4:
                                columnDefs[j] = ColumnDefs[iis[i].attrIndexArr_4];
                                break;
                            case 5:
                                columnDefs[j] = ColumnDefs[iis[i].attrIndexArr_5];
                                break;
                            case 6:
                                columnDefs[j] = ColumnDefs[iis[i].attrIndexArr_6];
                                break;
                            case 7:
                                columnDefs[j] = ColumnDefs[iis[i].attrIndexArr_7];
                                break;
                            case 8:
                                columnDefs[j] = ColumnDefs[iis[i].attrIndexArr_8];
                                break;
                            case 9:
                                columnDefs[j] = ColumnDefs[iis[i].attrIndexArr_9];
                                break;
                            case 10:
                                columnDefs[j] = ColumnDefs[iis[i].attrIndexArr_10];
                                break;
                        }
                    }
                    iil[i] = new IndexInfo(iis[i].handle, name, columnDefs, sortOrderings);
                }

                return iil;
            }
        }
    }
}
