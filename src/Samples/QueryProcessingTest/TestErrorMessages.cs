using System;
using System.Diagnostics;
using Starcounter;
using Starcounter.Query.RawParserAnalyzer;

namespace QueryProcessingTest {
    public static class TestErrorMessages {
        private static MapParserTree analyzer = null;
        private static ParserTreeWalker walker = null;
        private static int ignored = 0;
        private static int sqlexceptions = 0;
        public static void RunTestErrorMessages() {
            HelpMethods.LogEvent("Test error messages.");
            ignored = 0;
            sqlexceptions = 0;
            analyzer = new MapParserTree();
            walker = new ParserTreeWalker();
            RunErrorQuery("DELETE FROM Account");
            RunErrorQuery("select from fro fro");
            Trace.Assert(ignored == 1);
            Trace.Assert(sqlexceptions == 1);
            TestSomeExceptions();
            HelpMethods.LogEvent("Finished test of error messages");
        }

        internal static void RunErrorQuery(string query) {
            try {
                walker.ParseQueryAndWalkTree(query, analyzer);
            } catch (Exception ex) {
                if (ex is SqlException) {
                    ex.ToString();
                    sqlexceptions++;
                    //HelpMethods.LogEvent(ex.ToString());
                    return;
                }
                if (!((uint)ex.Data[ErrorCode.EC_TRANSPORT_KEY] == Error.SCERRSQLINTERNALERROR))
                    throw;
                ignored++;
            }
        }

        internal static void TestSomeExceptions() {
            Boolean wasException = false;
            try {
                var res = Db.SQL("select u from user u where firstname = ?", 1).First;
            } catch (ArgumentException ex) {
                Trace.Assert((uint)ex.Data[ErrorCode.EC_TRANSPORT_KEY] == Error.SCERRBADARGUMENTS);
                wasException = true;
            }
            Trace.Assert(wasException);
            wasException = false;
            try {
                Db.SQL("create indx asdf on account(client)");
            } catch (SqlException ex) {
                Trace.Assert((uint)ex.Data[ErrorCode.EC_TRANSPORT_KEY] == Error.SCERRSQLINCORRECTSYNTAX);
                wasException = true;
            }
            Trace.Assert(wasException);
            wasException = false;
            try {
                Db.Transaction(delegate {
                    Db.SQL("create index indx on account (accountid)");
                });
            } catch (DbException ex) {
                Trace.Assert((uint)ex.Data[ErrorCode.EC_TRANSPORT_KEY] == Error.SCERRTRANSACTIONLOCKEDONTHREAD);
                wasException = true;
            }
            Trace.Assert(wasException);
            wasException = false;
            try {
                Db.SQL("create unique index indx on account (accouintid)");
            } catch (SqlException ex) {
                Trace.Assert((uint)ex.Data[ErrorCode.EC_TRANSPORT_KEY] == Error.SCERRSQLUNKNOWNNAME);
                wasException = true;
            }
            Trace.Assert(wasException);
            wasException = false;
            try {
                Db.SQL("create unique index indx on acount (accountid)");
            } catch (SqlException ex) {
                Trace.Assert((uint)ex.Data[ErrorCode.EC_TRANSPORT_KEY] == Error.SCERRSQLUNKNOWNNAME);
                wasException = true;
            }
            Trace.Assert(wasException);
            wasException = false;
            try {
                Db.SQL("delete from account");
            } catch (SqlException) {
                wasException = true;
            }
            Trace.Assert(wasException);
            wasException = false;
            try {
                Db.SQL("drop index indx");
            } catch (SqlException ex) {
                Trace.Assert((uint)ex.Data[ErrorCode.EC_TRANSPORT_KEY] == Error.SCERRSQLINCORRECTSYNTAX);
                wasException = true;
            }
            Trace.Assert(wasException);
            wasException = false;
            try {
                Db.SQL("drop index indx on acc");
            } catch (SqlException ex) {
                Trace.Assert((uint)ex.Data[ErrorCode.EC_TRANSPORT_KEY] == Error.SCERRSQLUNKNOWNNAME);
                wasException = true;
            }
            Trace.Assert(wasException);
            wasException = false;
            try {
                Db.SQL("drop index indx on account");
            } catch (DbException ex) {
                Trace.Assert((uint)ex.Data[ErrorCode.EC_TRANSPORT_KEY] == Error.SCERRINDEXNOTFOUND);
                wasException = true;
            }
            Trace.Assert(wasException);
            wasException = false;
        }
    }
}
