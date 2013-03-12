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
            //InheritedIndexTest.PrintQueryPlan("select p from professor p  where p IS IndexQueryTest.InheritedIndex.Professor and company = ?"); // use index
            foreach (Professor e in Db.SQL("select p from professor p  where p IS IndexQueryTest.InheritedIndex.Professor and company = ?", company))
                nrObjects++;
            Trace.Assert(nrObjects == InheritedIndexTest.nrProfessors);
            // Object expression and type identifier to false
            nrObjects = 0;
            //InheritedIndexTest.PrintQueryPlan("select p from professor p  where p IS Student"); // use index
            foreach (Professor e in Db.SQL("select p from professor p  where p IS Student"))
                nrObjects++;
            Trace.Assert(nrObjects == 0);
            // Object expression is subtype to type identifier
            nrObjects = 0;
            //InheritedIndexTest.PrintQueryPlan("select p from Teacher p  where p IS Employee"); // use index
            foreach (Teacher e in Db.SQL("select p from Teacher p  where p IS Employee"))
                nrObjects++;
            Trace.Assert(nrObjects == InheritedIndexTest.TotalTeachers);
            // Object expression is equal to type identifier
            nrObjects = 0;
            //InheritedIndexTest.PrintQueryPlan("select p from Employee p  where p IS Employee"); // use index
            foreach (Employee e in Db.SQL("select p from Employee p  where p IS Employee"))
                nrObjects++;
            Trace.Assert(nrObjects == InheritedIndexTest.TotalEmployees);
            // Object expression is supertype to type identifier
            nrObjects = 0;
            //InheritedIndexTest.PrintQueryPlan("select p from Employee p  where p IS Manager"); // use index
            foreach (Employee e in Db.SQL("select p from Employee p  where p IS Manager"))
                nrObjects++;
            Trace.Assert(nrObjects == InheritedIndexTest.nrManagers);
            // Object expression and type variable
            nrObjects = 0;
            //InheritedIndexTest.PrintQueryPlan("select p from Employee p  where p is ?");
            foreach (Employee e in Db.SQL("select p from Employee p  where p is ?", typeof(Manager)))
                nrObjects++;
            Trace.Assert(nrObjects == InheritedIndexTest.nrManagers);
            nrObjects = 0;
            //InheritedIndexTest.PrintQueryPlan("select p from Manager p  where p is ?");
            foreach (Employee e in Db.SQL("select p from Manager p  where p is ?", typeof(Manager)))
                nrObjects++;
            Trace.Assert(nrObjects == InheritedIndexTest.nrManagers);
            //InheritedIndexTest.PrintQueryPlan("select p from Professor p  where p is ?");
            foreach (Employee e in Db.SQL("select p from Professor p  where p is ?", typeof(Manager)))
                nrObjects++;
            Trace.Assert(nrObjects == InheritedIndexTest.nrProfessors);
            // Several IS type expressions
            nrObjects = 0;
            //InheritedIndexTest.PrintQueryPlan("select p from Employee p  where p is ? and p is  ?");
            foreach (Employee e in Db.SQL("select p from Employee p  where p is ? and p is  ?", typeof(Professor), typeof(Teacher)))
                nrObjects++;
            Trace.Assert(nrObjects == InheritedIndexTest.nrProfessors);
            nrObjects = 0;
            //InheritedIndexTest.PrintQueryPlan("select p from Employee p  where p is ? and p is  ?");
            foreach (Employee e in Db.SQL("select p from Employee p  where p is ? and p is  ?", typeof(Manager), typeof(Teacher)))
                nrObjects++;
            Trace.Assert(nrObjects == 0);
            // Join
            nrObjects = 0;
            //InheritedIndexTest.PrintQueryPlan("select p from Employee p, Manager e  where p is ? and e is  ? and p.Boss = e");
            foreach (Employee e in Db.SQL("select p from Employee p, Manager e  where p is ? and e is  ? and p.Boss = e", typeof(Teacher), typeof(Manager)))
                nrObjects++;
            Trace.Assert(nrObjects == InheritedIndexTest.TotalTeachers);
            nrObjects = 0;
            //InheritedIndexTest.PrintQueryPlan("select p from Employee p, Manager e  where p is ? and e is  ? and p.Boss = e and p.Birthday > e.Birthday");
            foreach (Employee e in Db.SQL("select p from Employee p, Manager e  where p is ? and e is  ? and p.Boss = e and p.Birthday > e.Birthday", 
                typeof(Teacher), typeof(Manager)))
                nrObjects++;
            Trace.Assert(nrObjects == InheritedIndexTest.nrTeachers);
            // Literal test
#if false
            nrObjects = 0;
            InheritedIndexTest.PrintQueryPlan("select p from Student p where p.Name IS ?");
            foreach (Employee e in Db.SQL("select p from Student p where p.Name IS ", typeof(String)))
                nrObjects++;
            Trace.Assert(nrObjects == 0);
            nrObjects = 0;
            InheritedIndexTest.PrintQueryPlan("select p from Student p where p.Name IS string");
            foreach (Employee e in Db.SQL("select p from Student p where p.Name IS string"))
                nrObjects++;
            Trace.Assert(nrObjects == 0);
#endif
        }
    }
}
