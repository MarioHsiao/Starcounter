
using Starcounter.Internal;
using System.Text;

namespace Starcounter
{

    public sealed class TableDef
    {

        public TableDef(string name, ushort tableId)
        {
            Name = name;
            TableId = tableId;
        }

        public string Name { get; private set; }

        public ushort TableId { get; private set; }
    }
}
