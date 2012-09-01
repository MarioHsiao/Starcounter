
using Starcounter;
namespace Sc.Server.Binding
{
    public class PropertyBinding : IPropertyBinding
    {

        public readonly ColumnDef ColumnDef;

        private readonly int index_;

        public PropertyBinding(ColumnDef columnDef, int index)
        {
            ColumnDef = columnDef;
            index_ = index;
        }

        public int GetDataIndex()
        {
            throw new System.NotImplementedException();
        }

        public int AccessCost
        {
            get { throw new System.NotImplementedException(); }
        }

        public int Index
        {
            get { return index_; }
        }

        public IPropertyBinding InversePropertyBinding
        {
            get { throw new System.NotImplementedException(); }
        }

        public int MutateCost
        {
            get { throw new System.NotImplementedException(); }
        }

        public string Name
        {
            get { return ColumnDef.Name; }
        }

        public ITypeBinding TypeBinding
        {
            get { throw new System.NotImplementedException(); }
        }

        public DbTypeCode TypeCode
        {
            get { return DbTypeCode.Int64; } // TODO: 
        }
    }
}
