using System;
using System.Collections;
using Starcounter;

namespace IndexQueryTest.InheritedIndex {
    static public class InheritedIndexTest {
        public static void RunInheritedIndexTest() {
            DropIndexes();
            CreateIndexes();
            TestInheritedIndexes();
        }

        internal static void CreateIndexes() {
            Db.SlowSQL("CREATE INDEX personName ON Person (Name)");
            Db.SlowSQL("CREATE INDEX teacherName ON IndexQueryTest.InheritedIndex.Teacher (Name)");
            Db.SlowSQL("CREATE INDEX personBirthdayGender ON Person (Birthday, Gender)");
            Db.SlowSQL("CREATE INDEX employeeCompany ON Employee (Company)");
            Db.SlowSQL("CREATE INDEX professorCompany On Professor (Company)");
            Db.SlowSQL("CREATE INDEX employeeBoss ON Employee (Boss)");
        }

        internal static void DropIndexes() {
            CallDropIndex("DROP INDEX personName ON Person");
            CallDropIndex("DROP INDEX teacherName ON IndexQueryTest.InheritedIndex.Teacher");
            CallDropIndex("DROP INDEX personBirthdayGender ON Person");
            CallDropIndex("DROP INDEX employeeCompany ON Employee");
            CallDropIndex("DROP INDEX professorCompany On Professor");
            CallDropIndex("DROP INDEX employeeBoss ON Employee");
        }

        internal static void CallDropIndex(String statement) {
            try {
                Db.SlowSQL(statement);
            } catch (DbException e) {
                if (e.ErrorCode != Starcounter.Error.SCERRINDEXNOTFOUND)
                    throw e;
            }
        }

        internal static void TestInheritedIndexes() {
            PrintQueryPlan("select p from IndexQueryTest.InheritedIndex.Person p where name = ?");
            PrintQueryPlan("select e from Employee e where company = ?");
            PrintQueryPlan("select e from teacher e where company = ?");
            PrintQueryPlan("select e from professor e where company = ?");
        }

        internal static void PrintQueryPlan(String query) {
            Console.WriteLine(((IEnumerator)Db.SQL(query,null).GetEnumerator()).ToString());
        }
    }
}
