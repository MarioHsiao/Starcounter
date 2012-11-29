using System;
using Starcounter;
using Starcounter.Internal;

namespace StarcounterApps3 {
    partial class SqlApp : App {
        void Handle(Input.Execute execute) {
            var app = new TestSqlResult();
            app.ConvertResult(null);
            QueryResult = app;
        }
    }
}
