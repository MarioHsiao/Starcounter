

using Starcounter.Templates;
namespace Starcounter.Internal.Application.CodeGeneration {

    /// <summary>
    /// Represents a class or type that is provided by a library
    /// (such as String, bool or AppTemplate).
    /// </summary>
    public class NPredefinedType : NClass {

        private string _FixedClassName;

        /// <summary>
        /// Sets the fixed name of the type
        /// </summary>
        public string FixedClassName {
            set {
                _FixedClassName = value;
            }
        }

        /// <summary>
        /// As no declaring code is generated from these nodes,
        /// there is no need to track the inherited types
        /// </summary>
        public override string Inherits {
            get { return null; }
        }

        /// <summary>
        /// The class name (type name) is provided as a fixed
        /// string
        /// </summary>
        public override string ClassName {
            get {
                return _FixedClassName;
            }
        }

    }
}
