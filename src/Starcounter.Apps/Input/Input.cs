
using System;
using Starcounter.Templates;
namespace Starcounter {

    public class Input<TApp, TTemplate> : Input
        where TApp : App
        where TTemplate : Template {
        private readonly TApp _app = null;
        private readonly TTemplate _template = null;

        public TApp App { get { return _app; } }
        public TTemplate Template { get { return _template; } }
    }

    public class Input<TApp, TTemplate, TValue> : Input<TApp,TTemplate> where TApp : App where TTemplate : Template {
        public TApp Parent {
            get {
                return null;
            }
        }

        public void Cancel() {
            throw new NotImplementedException();
        }

        public void CallOtherHandlers() {
            throw new NotImplementedException();
        }

        public bool Cancelled { get; set; }

        public TValue NewValue { get; set; }

        public TValue OldValue {
            get {
                throw new NotImplementedException();
//                App.GetValue<TTemplate>(Template);
            }
        }

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
