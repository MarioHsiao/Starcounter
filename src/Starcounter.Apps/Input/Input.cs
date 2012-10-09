
using System;
using Starcounter.Templates;
namespace Starcounter {

    public class Input<TValue> : Input {
        public TValue Value { get; set; }

        public TValue OldValue {
            get {
                throw new NotImplementedException();
                //                App.GetValue<TTemplate>(Template);
            }
        }
    }

    public class Input<TApp, TTemplate> : Input
        where TApp : App
        where TTemplate : Template {

        private TApp _app = null;
        private TTemplate _template = null;
        public TApp App { get { return _app; } set { _app = value; } }
        public TTemplate Template { get { return _template; } set { _template = value; }  }
    }

    public class Input<TApp, TTemplate, TValue> : Input<TValue> where TApp : App where TTemplate : Template {

        private TApp _app = null;
        private TTemplate _template = null;
        public TApp App { get { return _app; } set { _app = value; } }
        public TTemplate Template { get { return _template; } set { _template = value; } }

        public TApp Parent {
            get {
                return null;
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

        private bool _cancelled = false;

        public void Cancel() {
            Cancelled = true;
        }

        public void CallOtherHandlers() {
            throw new NotImplementedException();
        }

        public bool Cancelled {
            get {
                return _cancelled;
            }
            set {
                _cancelled = value;
            }
        }

    }   

    public class SchemaAttribute : System.Attribute {
    }
}
