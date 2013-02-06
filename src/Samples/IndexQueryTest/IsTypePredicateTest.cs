using System;
using System.Diagnostics;
using IndexQueryTest.InheritedIndex;
using Starcounter;

namespace IndexQueryTest {
    public static class IsTypePredicateTest {
        public static void RunIsTypePredicateTest() {
            Employer company = Db.SQL<Employer>("select e from employer e where address =  ?", "Here").First;
            // Object expression and type identifier to true
            int nrObjects = 0;
            InheritedIndexTest.PrintQueryPlan("select p from professor p  where p IS IndexQueryTest.InheritedIndex.Professor and company = ?"); // use index
            foreach (Professor e in Db.SQL("select p from professor p  where p IS IndexQueryTest.InheritedIndex.Professor and company = ?", company))
                nrObjects++;
            Trace.Assert(nrObjects == InheritedIndexTest.nrProfessors);
            // Object expression and type identifier to false
            nrObjects = 0;
            InheritedIndexTest.PrintQueryPlan("select p from professor p  where p IS Student"); // use index
            foreach (Professor e in Db.SQL("select p from professor p  where p IS Student"))
                nrObjects++;
            Trace.Assert(nrObjects == 0);
            // Type short and full names
            // Object expression is subtype to type identifier
            // Object expression is equal to type identifier
            // Object expression is supertype to type identifier
            // Object variable and type identifier
            // Object expression and type variable
            // Object variable and type variable
        }
    }
}
