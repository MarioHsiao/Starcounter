using System;
using System.Text;
using Starcounter;
using Starcounter.Binding;
using Starcounter.Internal;
using Starcounter.Query.Execution;

namespace StarcounterSQLApp {
    // TODO:
    // DisplayName in PropertyMapping (PropertyBinding) needs to be public.

    // TODO:
    // DisplayName when projection is a simple value.

    partial class SqlApp : App {
        static void Main(string[] args) {
            AppsBootstrapper.Bootstrap(8080);
            AssureMuchoData();

            GET("/query", () => new SqlApp() { View = "sql.html" });
        }

        private static void AssureMuchoData() {
            Db.Transaction(() => {
                Mucho m;
                if (Db.SQL("SELECT m FROM Mucho m").First == null) {
                    for (Int32 i = 1; i < 101; i++) {
                        m = new Mucho() { Name = "Mucho" + i, Value = i };
                    }
                }
            });
        }

        // TODO:
        // State that should not be exposed to client and need to be saved
        // to use the recreationkey (offsetkey).
        // Cannot save this here!
        private byte[] offsetKey;
        private string queryWithoutParamerers;
        private object[] parameters;

        void Handle(Input.Execute execute) {
            string modifiedQuery = Query;

            ResetError();
            ResetResult();

            // TODO:
            // Separate values from query.

            queryWithoutParamerers = modifiedQuery;
            parameters = null;

            offsetKey = ExecuteQuery(queryWithoutParamerers, FetchCount, parameters);
            if (offsetKey != null) {
                queryWithoutParamerers += " OFFSETKEY ?";
                More = true;
            } else {
                More = false;
            }
        }

        void Handle(Input.GetMore getMore) {
            ResetError();

            if (offsetKey == null) {
                getMore.Cancel();
                return;
            }

            if (parameters == null)
                offsetKey = ExecuteQuery(queryWithoutParamerers, FetchCount, offsetKey);
            else
                offsetKey = ExecuteQuery(queryWithoutParamerers, FetchCount, parameters, offsetKey);

            More = (offsetKey != null);
        }

        void Handle(Input.Query query) {
            offsetKey = null;
            More = false;
        }

        private void ResetError() {
            Error.Code = 0;
            Error.Message = null;
            Error.Helplink = null;
        }

        private void ResetResult() {
            offsetKey = null;
            QueryResult.Columns.Clear();
            QueryResult.Result.Clear();
        }

        /// <summary>
        /// Executes the specified query fetches at most the specified number of results
        /// and returns an offsetKey or null if all results are returned.
        /// </summary>
        /// <param name="query">The query to execute</param>
        /// <param name="offsetKey">An existing offsetKey or null if this is a new query</param>
        /// <param name="fetchCount">The number of rows to fetch</param>
        /// <returns>An offsetKey or null if the end of the result are reached.</returns>
        private byte[] ExecuteQuery(string query, int fetchCount, params object[] parameters) {
            bool addColumns;
            IPropertyBinding[] props;
            SqlEnumerator sqle;
            
            sqle = null;
            try {
                try {

                    // TODO:
                    // When parameters are separated from the query change this to SQL.
                    sqle = Db.SlowSQL(query, parameters).GetEnumerator();
                } catch (Exception sex) {
                    ResetResult();
                    Error.Code = 0;
                    Error.Message = sex.Message;
                    Error.Helplink = sex.HelpLink;

                    return null;
                }
               
                addColumns = (offsetKey == null);
                props = GetPropertiesFromResult(sqle, addColumns);
                offsetKey = FillResult(sqle, props, fetchCount);
            } finally {
                if (sqle != null)
                    sqle.Dispose();
            }

            return offsetKey;
        }

        private IPropertyBinding[] GetPropertiesFromResult(SqlEnumerator sqle, bool addColumns) {
            IPropertyBinding propertyBinding;
            IPropertyBinding[] props;
            ITypeBinding resultBinding;
            PropertyMapping propertyMapping;
            string displayName;

            if (addColumns)
                QueryResult.Columns.Clear();

            if (sqle.ProjectionTypeCode != null){
                props = new IPropertyBinding[1];
                props[0] = new SingleProjectionBinding() { TypeCode = (DbTypeCode)sqle.ProjectionTypeCode };

                if (addColumns){
                    QueryResult.Columns.Add(new QueryResultApp.ColumnsApp() {
                        Title = props[0].Name,
                        Type = SQLToJsonHelper.DbTypeCodeToString(props[0].TypeCode)
                    });
                }
            } else {
                resultBinding = sqle.TypeBinding;
                props = new IPropertyBinding[resultBinding.PropertyCount];
                for (int i = 0; i < resultBinding.PropertyCount; i++) {
                    propertyBinding = resultBinding.GetPropertyBinding(i);
                    props[i] = propertyBinding;

                    if (addColumns) {
                        propertyMapping = propertyBinding as PropertyMapping;
                        displayName = (propertyMapping != null) ? propertyMapping.DisplayName : propertyBinding.Name;

                        QueryResult.Columns.Add(new QueryResultApp.ColumnsApp() {
                            Title = displayName,
                            Type = SQLToJsonHelper.DbTypeCodeToString(props[0].TypeCode)
                        });
                    }
                }
            }

            return props;
        }

        private byte[] FillResult(SqlEnumerator sqle, IPropertyBinding[] props, int fetchCount) {
            bool moreToFetch;
            byte[] offsetKey;
            StringBuilder sb;
            
            moreToFetch = true;
            offsetKey = null;
            sb = new StringBuilder();

            if (fetchCount != 0) {
                for (int i = 0; i < fetchCount; i++) {
                    if (!sqle.MoveNext()) {
                        moreToFetch = false;
                        break;
                    }

                    if (sqle.ProjectionTypeCode != null)
                        this.QueryResult.Result.Add(new SqlRowApp(sqle.Current, props[0]));
                    else
                        this.QueryResult.Result.Add(new SqlRowApp(sqle.Current, props));
                }

                // Since the offsetkey is for the CURRENT item we step one step forward
                // before retrieveing it.
                if (moreToFetch && sqle.MoveNext()) {
                    offsetKey = sqle.GetOffsetKey();
                } else {
                    moreToFetch = false;
                }
            } else {
                while (sqle.MoveNext()) {
                    if (sqle.ProjectionTypeCode != null)
                        this.QueryResult.Result.Add(new SqlRowApp(sqle.Current, props[0]));
                    else
                        this.QueryResult.Result.Add(new SqlRowApp(sqle.Current, props));
                }
            }
            return offsetKey;
        }
    }
}
