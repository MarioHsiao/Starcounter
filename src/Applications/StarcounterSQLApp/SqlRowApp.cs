using System.Text;
using Starcounter;
using Starcounter.Binding;

namespace StarcounterSQLApp {
    public class SqlRowApp : App {
        // TODO:
        // Need someplace to save temporary data (during the request) for the current request to send to the client.
        private string json;

        public override string ToJson(bool includeView = false, bool includeSchema = false) {
            return json;
        }

        public SqlRowApp(IObjectView row, IPropertyBinding[] props) {
            AddOne(row, props);
        }

        public SqlRowApp(object value, IPropertyBinding property) {
            AddOne(value, property);
        }

        //internal void Begin(StringBuilder buffer) {
        //    buffer.Append('[');
        //}

        private void AddOne(IObjectView row, IPropertyBinding[] props) {
            StringBuilder buffer = new StringBuilder();
            buffer.Append('[');
            for (int i = 0; i < (props.Length - 1); i++) {
                buffer.Append(SQLToJsonHelper.PropertyToJsonString(props[i], row));
                buffer.Append(',');
            }
            buffer.Append(SQLToJsonHelper.PropertyToJsonString(props[props.Length - 1], row));
            buffer.Append("]");
            json = buffer.ToString();
        }

        private void AddOne(object value, IPropertyBinding property) {
            StringBuilder buffer = new StringBuilder();
            buffer.Append('[');
            switch (property.TypeCode) {
                case DbTypeCode.String:
                    buffer.Append('\"');
                    if (value != null)
                        buffer.Append(value);
                    buffer.Append('\"');
                    break;
                case DbTypeCode.Object:
                    buffer.Append(DbHelper.GetObjectID(value as Entity));
                    break;
                case DbTypeCode.Binary:
                case DbTypeCode.LargeBinary:
                    // TODO:
                    buffer.Append("\"TODO:\"");
                    break;
                default:
                    buffer.Append(value);
                    break;
            }
            buffer.Append("]");
            json = buffer.ToString();
        }

        //internal void End(StringBuilder buffer) {
        //    buffer[buffer.Length - 1] = ']';
        //    json = buffer;
        //}
    }

}
