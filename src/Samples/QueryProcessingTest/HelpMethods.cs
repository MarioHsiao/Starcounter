using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter;
using Starcounter.TestFramework;

namespace QueryProcessingTest {
    public static class HelpMethods {
        internal static TestLogger logger = null;

        internal static void PrintQueryPlan(String query) {
            Console.WriteLine(((IEnumerator)Db.SQL(query, null).GetEnumerator()).ToString());
        }

        internal static void PrintSlowQueryPlan(String query) {
            Console.WriteLine(((IEnumerator)Db.SlowSQL(query, null).GetEnumerator()).ToString());
        }

        internal static void LogEvent(String eventString) {
            logger.Log(eventString);
        }
    }
}
