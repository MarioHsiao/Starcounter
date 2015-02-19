using System;
using Starcounter;

namespace SQLTest.InheritedDb {
    public static class InheritedData {
        internal static int nrPersons = 0;
        internal static int nrEmployees = 0;
        internal static int nrStudents = 5;
        internal static int nrManagers = 2;
        internal static int nrTeachers = 4;
        internal static int nrProfessors = 2;

        internal static int TotalTeachers { get { return nrProfessors + nrTeachers; } }
        internal static int TotalEmployees { get { return TotalTeachers + nrManagers; } }
        internal static int TotoalPersons { get { return TotalEmployees + nrStudents; } }

        internal static void CreateIndexes() {
            Db.SlowSQL("CREATE unique INDEX personName ON SQLTest.InheritedDb.Person (Name)");
            Db.SlowSQL("CREATE INDEX teacherName ON SQLTest.InheritedDb.Teacher (Name)");
            Db.SlowSQL("CREATE INDEX personBirthdayGender ON SQLTest.InheritedDb.Person (Birthday, Gender)");
            Db.SlowSQL("CREATE INDEX companyIndx ON SQLTest.InheritedDb.Employee (Company)");
            Db.SlowSQL("CREATE INDEX companyIndx On Professor (Company)");
            Db.SlowSQL("CREATE INDEX employeeBoss ON SQLTest.InheritedDb.Employee (Boss)");
            Db.SlowSQL("create index professorBoss ON Professor (Boss)");
        }

        internal static void DropIndexes() {
            CallDropIndex("DROP INDEX personName ON SQLTest.InheritedDb.Person");
            CallDropIndex("DROP INDEX teacherName ON Teacher");
            CallDropIndex("DROP INDEX personBirthdayGender ON SQLTest.InheritedDb.Person");
            CallDropIndex("DROP INDEX companyIndx ON SQLTest.InheritedDb.Employee");
            CallDropIndex("DROP INDEX companyIndx On Professor");
            CallDropIndex("DROP INDEX employeeBoss ON SQLTest.InheritedDb.Employee");
            CallDropIndex("DROP index professorBoss ON SQLTest.InheritedDb.Professor");
        }

        internal static void CallDropIndex(String statement) {
            try {
                Db.SlowSQL(statement);
            } catch (DbException e) {
                if (e.ErrorCode != Starcounter.Error.SCERRINDEXNOTFOUND)
                    throw e;
            }
        }

        internal static void DropData() {
            Db.SlowSQL("DELETE From Student");
            Db.SlowSQL("DELETE From Teacher");
            Db.SlowSQL("DELETE From Professor");
            Db.SlowSQL("DELETE From SQLTest.InheritedDb.Employee");
            Db.SlowSQL("DELETE From Manager");
            Db.SlowSQL("DELETE From Employer");
            Db.SlowSQL("DELETE From SQLTest.InheritedDb.Person");
        }

        internal static void Populate() {
            // Create persons, employees, teachers, professors, students and managers
            Db.Transact(delegate {
                for (int i = 0; i < nrPersons; i++)
                    new Person { Name = "Person" + i, Birthday = new DateTime(1950 + i * 5, i % 12 + 1, i + 1), Gender = i % 2 };
                Employer employer = new Employer { Address = "Here" };
                Manager manager = null;
                for (int i = 0; i < nrManagers; i++)
                    manager = new Manager { Name = "Manager" + i, Birthday = new DateTime(1970 + i * 3, i + 1, i + 1), Gender = i % 2, Bonus = i, Company = employer };
                for (int i = 0; i < nrEmployees; i++)
                    new Employee { Name = "Employee" + i, Birthday = new DateTime(1970 + i * 4, i + 1, i + 1), Gender = i % 2, Company = employer,
                    Boss = manager };
                for (int i = 0; i < nrProfessors; i++)
                    new Professor { Name = "Professor" + i, Birthday = new DateTime(1950 + i * 1, i + 1, i + 1), Gender = i % 2, Company = employer, Boss = manager };
                for (int i = 0; i < nrTeachers; i++)
                    new Teacher { Name = "Teacher" + i, Birthday = new DateTime(1975 + i * 2, i + 1, i + 1), Gender = i % 2, Company = employer, Boss = manager };
                for (int i = 0; i < nrStudents; i++)
                    new Student { Name = "Student" + i, Birthday = new DateTime(1980 + i, i + 1, i + 1), Gender = i % 2 };
            });
        }
    }
}
