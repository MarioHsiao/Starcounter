

namespace Starcounter.Templates {
    
    /// <summary>
    /// Usefull for sugaring a template in code-generators by
    /// adding fixed properties that maps to template properties
    /// </summary>
    public class TJsonWrapper : ITJson {

        private Schema<Json<object>> _Template;

        public TJsonWrapper(Schema<Json<object>> template) {
            _Template = template;
        }

        public TJsonWrapper() {
            _Template = new Schema<Json<object>>();
        }

        public Schema<Json<object>> Template {
            get {
                return _Template;
            }
        }

        /*

        public string ClassName {
            get {
                throw new System.NotImplementedException();
            }
            set {
                throw new System.NotImplementedException();
            }
        }

        public string Namespace {
            get {
                throw new System.NotImplementedException();
            }
            set {
                throw new System.NotImplementedException();
            }
        }

        public T Add<T>(string name) where T : Template, new() {
            throw new System.NotImplementedException();
        }

        public T Add<T>(string name, TObj type) where T : TObjArr, new() {
            throw new System.NotImplementedException();
        }

        public Template Add(System.Type type, string name) {
            throw new System.NotImplementedException();
        }

        public T Add<T>(string name, string bind) where T : TValue, new() {
            throw new System.NotImplementedException();
        }

        public T Add<T>(string name, TObj type, string bind) where T : TObjArr, new() {
            throw new System.NotImplementedException();
        }

        public PropertyList Properties {
            get { throw new System.NotImplementedException(); }
        }
         */
    }
}
