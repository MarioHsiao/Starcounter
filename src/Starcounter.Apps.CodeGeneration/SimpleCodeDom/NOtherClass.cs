
namespace Starcounter.Internal.Application.CodeGeneration {

    /// <summary>
    /// Used for classes where a simple class name and inherits name
    /// is sufficient for code generation
    /// </summary>
    public class NOtherClass : NClass {

        public string _ClassName;
        public string _Inherits;

        public override string ClassName {
            get { return _ClassName; }
        }
        public override string Inherits {
            get { return _Inherits; }
        }
    }

}