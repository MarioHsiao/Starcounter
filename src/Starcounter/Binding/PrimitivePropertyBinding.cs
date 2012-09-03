
namespace Starcounter.Binding
{

    public abstract class PrimitivePropertyBinding : PropertyBinding
    {

        public PrimitivePropertyBinding() : base() { }

        public override sealed ITypeBinding TypeBinding
        {
            get
            {
                return null;
            }
        }
    }
}
