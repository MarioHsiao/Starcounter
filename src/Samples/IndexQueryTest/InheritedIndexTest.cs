using System;
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
        }
    }
}
