
using Starcounter.Internal;
using System.Text;

// TODO:
using Sc.Server.Binding;
using System;
using Starcounter.Query.Execution;


namespace Starcounter.Binding
{

    /// <summary>
    /// Definition of a database table.
    /// </summary>
    public sealed class TableDef
    {

        public string Name;

        public ushort TableId;

        public ulong DefinitionAddr;

        public ColumnDef[] ColumnDefs;

        public TableDef(string name, ColumnDef[] columnsDefs) : this(name, 0xFFFF, sccoredb.INVALID_DEFINITION_ADDR, columnsDefs) { }

        public TableDef(string name, ushort tableId, ulong definitionAddr, ColumnDef[] columnsDefs)
        {
            Name = name;
            TableId = tableId;
            DefinitionAddr = definitionAddr;
            ColumnDefs = columnsDefs;
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
