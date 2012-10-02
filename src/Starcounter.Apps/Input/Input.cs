
using Starcounter.Templates;
namespace Starcounter {

    public class Input<T1> : Input where T1 : App {
        private T1 _Doc = null;
        public T1 Doc { get { return _Doc; } }
    }

    public class Input<AppType, ValueType> : Input<AppType> where AppType : App {
        public AppType Parent {
            get {
                return null;
            }
        }

        public ValueType Value { get; set; }

        public App FindParent(ParentTemplate parentProperty) {
            return null;
        }

        public T FindParent<T>() where T:App {
            return null;
        }

    }

    public class Input {
    }   

    public class SchemaAttribute : System.Attribute {
    }
}
