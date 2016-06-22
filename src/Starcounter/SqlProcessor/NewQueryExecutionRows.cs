using System;

namespace Starcounter.SqlProcessor {
    public class NewQueryResultRows<T> : Rows<T> {
        protected string query;
        protected Object[] sqlParams;
        protected ulong iterator;

        internal static NewQueryResultRows<T> ProcessSqlQuery(String query, params Object[] values) {
            ulong sqlIterator;
            Exception ex = SqlProcessor.CallSelectPrepare(query, out sqlIterator);
            if (ex != null) throw ex;
#if false
            if (queryType == SqlProcessor.SQL_QUERY_TYPE_NONSELECT)
                return null;
#endif
            NewQueryResultRows<T> result = new NewQueryResultRows<T>(query, values);
            result.iterator = sqlIterator;
            return result;
        }

        internal NewQueryResultRows(String query, params Object[] values) {
            this.query = query;
            this.sqlParams = values;
        }

        public override IRowEnumerator<T> GetEnumerator() {
            return null;
        }

        public override T First {
            get {
                return default(T);
            }
        }
    }
}
