using System.Text;
using Starcounter;

namespace StarcounterApps3 {
    public class TestSqlResult : App {
        private string json;

        public override string ToJson(bool includeView = false, bool includeSchema = false, bool includeSessionId = false) {
            return json;
        }

        internal void ConvertResult(SqlResult result) {
            StringBuilder sb = new StringBuilder();

            sb.Append("{ \"Columns\": [ {\"Title\":\"FirstName\", \"Type\":\"string\"}, ");
			sb.Append("{\"Title\":\"LastName\", \"Type\":\"string\"}, ");
            sb.Append("{\"Title\":\"Age\", \"Type\":\"number\"} ], ");
			sb.Append("\"Result\": [ [ \"John\", \"Doe\", 19 ], [ \"Jane\", \"Roe\", 27 ] ] }");
            json = sb.ToString();
        }
    }
}
