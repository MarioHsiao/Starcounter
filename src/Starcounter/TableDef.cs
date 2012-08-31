
using Starcounter.Internal;
using System.Text;

namespace Starcounter
{

    public sealed class TableDef
    {

        public string Name;

        public ushort TableId;

        public ColumnDef[] Columns;

        public TableDef(string name, ushort tableId, ColumnDef[] columns)
        {
            Name = name;
            TableId = tableId;
            Columns = columns;
        }

    }
}
