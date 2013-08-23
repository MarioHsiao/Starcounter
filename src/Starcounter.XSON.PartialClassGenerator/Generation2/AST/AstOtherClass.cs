
namespace Starcounter.Internal.MsBuild.Codegen {

    /// <summary>
    /// Used for classes where a simple class name and inherits name
    /// is sufficient for code generation
    /// </summary>
    public class AstOtherClass : AstClass {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gen"></param>
        public AstOtherClass(Gen2DomGenerator gen)
            : base(gen) {
        }

        /// <summary>
        /// The _ class name
        /// </summary>
        public string _ClassName;


        /// <summary>
        /// Gets the name of the class.
        /// </summary>
        /// <value>The name of the class.</value>
        public override string ClassStemIdentifier {
            //            get { return "?" + _ClassName; }
            
            get { return 
                _ClassName;
            }
        }
    }
}