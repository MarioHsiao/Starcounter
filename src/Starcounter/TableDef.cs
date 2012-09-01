
using Starcounter.Internal;
using System.Text;

namespace Starcounter
{

    public sealed class TableDef
    {

        public string Name;

        public ushort TableId;

        public ulong DefinitionAddr;

        public ColumnDef[] Columns;

        public TableDef(string name, ColumnDef[] columns) : this(name, 0xFFFF, sccoredb.INVALID_DEFINITION_ADDR, columns) { }

        public TableDef(string name, ushort tableId, ulong definitionAddr, ColumnDef[] columns)
        {
            Name = name;
            TableId = tableId;
            DefinitionAddr = definitionAddr;
            Columns = columns;
        }
    }
}
