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
            // Object expression is subtype to type identifier
            nrObjects = 0;
            InheritedIndexTest.PrintQueryPlan("select p from Teacher p  where p IS Employee"); // use index
            foreach (Teacher e in Db.SQL("select p from Teacher p  where p IS Employee"))
                nrObjects++;
            Trace.Assert(nrObjects == InheritedIndexTest.TotalTeachers);
            // Object expression is equal to type identifier
            nrObjects = 0;
            InheritedIndexTest.PrintQueryPlan("select p from Employee p  where p IS Employee"); // use index
            foreach (Employee e in Db.SQL("select p from Employee p  where p IS Employee"))
                nrObjects++;
            Trace.Assert(nrObjects == InheritedIndexTest.TotalEmployees);
            // Object expression is supertype to type identifier
            nrObjects = 0;
            InheritedIndexTest.PrintQueryPlan("select p from Employee p  where p IS Manager"); // use index
            foreach (Employee e in Db.SQL("select p from Employee p  where p IS Manager"))
                nrObjects++;
            Trace.Assert(nrObjects == InheritedIndexTest.nrManagers);
            // Object variable and type identifier
            nrObjects = 0;
            InheritedIndexTest.PrintQueryPlan("select p from Employee p  where ? is Employer"); // use index
            foreach (Employee e in Db.SQL("select p from Employee p  where ? is Employer", company))
                nrObjects++;
            Trace.Assert(nrObjects == InheritedIndexTest.nrManagers);
            // Object expression and type variable
            // Object variable and type variable
            // Several IS type expressions
            nrObjects = 0;
            InheritedIndexTest.PrintQueryPlan("select p from Employee p  where p IS Manager and ? is Employer"); // use index
            foreach (Employee e in Db.SQL("select p from Employee p  where p IS Manager and ? is Employer", company))
                nrObjects++;
            Trace.Assert(nrObjects == InheritedIndexTest.nrManagers);
        }
    }
}
