using System;
using System.IO;
using System.Text;
using System.Threading;
using Starcounter;
using System.Diagnostics;
using System.Collections.Generic;
using Starcounter.Internal;
using Starcounter.Advanced;
using System.Net;
using Starcounter.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ABCTest;

namespace DbMappingTest {

    public static class Utils {
        public static Int32 NumObjects<T>() {
            Int32 num = 0;
            foreach (var o in Db.SQL("SELECT o FROM " + typeof(T).FullName + " o")) {
                num++;
            }
            return num;
        }
    }

    [Database]
    public class NameClass1 {
        public String FirstName;
        public String LastName;
        public Int32 Age;
    }

    [Database]
    public class NameClass2 {
        public String FullName;
        public Int32 YoungerAge;
    }

    [Database]
    public class NameClass3 {
        public String FirstName;
        public Int32 OlderAge;
    }

    [Database]
    public class NameClass4 {
        public String FirstName;
        public Int32 Age;
    }

    [Database]
    public class PrivateClassA {
        public string Name;
        public PrivateClassB RefClass;
    }

    [Database]
    public class PrivateClassB {
        public string Name;
    }

    [Database]
    public class OtherClassA : IEntity {
        public string Name;
        public OtherClassB RefClass;

        public void OnDelete() {
            if (RefClass != null)
                RefClass.Delete();
        }
    }

    [Database]
    public class OtherClassB {
        public string Name;
    }

    [Database]
    public class Class1 {
        public string Name;
    }

    [Database]
    public class Class2 {
        public string Name;
    }

    public class TestTemplatedMapping {

        public static void Map() {
            DbMapping.MapDefault<Class1, Class2>();
        }
    }

    public class TestCrossDeletion {

        public static void Map() {

            DbMapping.MapCreation("DbMappingTest.PrivateClassA", "DbMappingTest.OtherClassA", (UInt64 fromOid) => {
                OtherClassA dst = new OtherClassA();
                return dst.GetObjectNo(); // Newly created object ID.
            });

            DbMapping.MapCreation("DbMappingTest.OtherClassA", "DbMappingTest.PrivateClassA", (UInt64 fromOid) => {
                PrivateClassA dst = new PrivateClassA();
                return dst.GetObjectNo(); // Newly created object ID.
            });

            DbMapping.MapCreation("DbMappingTest.PrivateClassB", "DbMappingTest.OtherClassB", (UInt64 fromOid) => {
                OtherClassB dst = new OtherClassB();
                return dst.GetObjectNo(); // Newly created object ID.
            });

            DbMapping.MapCreation("DbMappingTest.OtherClassB", "DbMappingTest.PrivateClassB", (UInt64 fromOid) => {
                PrivateClassB dst = new PrivateClassB();
                return dst.GetObjectNo(); // Newly created object ID.
            });

            DbMapping.MapDeletion("DbMappingTest.PrivateClassA", "DbMappingTest.OtherClassA", (UInt64 fromOid, UInt64 toOid) => {
                OtherClassA dst = (OtherClassA)DbHelper.FromID(toOid);
                if (dst != null)
                    dst.Delete();
            });

            DbMapping.MapDeletion("DbMappingTest.OtherClassA", "DbMappingTest.PrivateClassA", (UInt64 fromOid, UInt64 toOid) => {
                PrivateClassA dst = (PrivateClassA)DbHelper.FromID(toOid);
                if (dst != null)
                    dst.Delete();
            });

            DbMapping.MapDeletion("DbMappingTest.PrivateClassB", "DbMappingTest.OtherClassB", (UInt64 fromOid, UInt64 toOid) => {
                OtherClassB dst = (OtherClassB)DbHelper.FromID(toOid);
                if (dst != null)
                    dst.Delete();
            });

            DbMapping.MapDeletion("DbMappingTest.OtherClassB", "DbMappingTest.PrivateClassB", (UInt64 fromOid, UInt64 toOid) => {
                PrivateClassB dst = (PrivateClassB)DbHelper.FromID(toOid);
                if (dst != null)
                    dst.Delete();
            });

            DbMapping.MapModification("DbMappingTest.PrivateClassA", "DbMappingTest.OtherClassA", (UInt64 fromOid, UInt64 toOid) => {
                PrivateClassA src = (PrivateClassA)DbHelper.FromID(fromOid);
                OtherClassA dst = (OtherClassA)DbHelper.FromID(toOid);
                dst.Name = src.Name;

                // Referencing back.
                if (src.RefClass != null) {
                    List<UInt64> mappedOids = DbMapping.GetMappedOids(src.RefClass.GetObjectNo());
                    dst.RefClass = (OtherClassB)DbHelper.FromID(mappedOids[0]);
                }
            });

            DbMapping.MapModification("DbMappingTest.PrivateClassB", "DbMappingTest.OtherClassB", (UInt64 fromOid, UInt64 toOid) => {
                PrivateClassB src = (PrivateClassB)DbHelper.FromID(fromOid);
                OtherClassB dst = (OtherClassB)DbHelper.FromID(toOid);
                dst.Name = src.Name;
            });


            DbMapping.MapModification("DbMappingTest.OtherClassB", "DbMappingTest.PrivateClassB", (UInt64 fromOid, UInt64 toOid) => {
                OtherClassB src = (OtherClassB)DbHelper.FromID(fromOid);
                PrivateClassB dst = (PrivateClassB)DbHelper.FromID(toOid);
                dst.Name = src.Name;
            });
        }

        public static void RunTest() {

            Db.Transact(() => {
                Db.SlowSQL("DELETE FROM PrivateClassA");
                Db.SlowSQL("DELETE FROM PrivateClassB");
                Db.SlowSQL("DELETE FROM OtherClassA");
                Db.SlowSQL("DELETE FROM OtherClassB");
            });

            Db.Transact(() => {

                // Checking that there are no existing relations.
                DbMappingRelation rel = Db.SQL<DbMappingRelation>("SELECT o FROM DbMappingRelation o").First;
                Assert.IsTrue(rel == null);

                // Testing cascading deletes where the other object deletes additional
                // objects, which will lead to an InvalidObjectAccess in DbMapping.DELETE
                PrivateClassA privateA = new PrivateClassA();

                // Checking creation of mapped class.
                OtherClassA otherA = Db.SQL<OtherClassA>("SELECT o FROM OtherClassA o").First;
                Assert.IsTrue(otherA != null);
                Assert.IsTrue(otherA.Name == null);
                Assert.IsTrue(otherA.RefClass == null);

                privateA.Name = "Some";
                Assert.IsTrue(otherA.Name == "Some");
                Assert.IsTrue(otherA.RefClass == null);

                privateA.RefClass = new PrivateClassB();

                OtherClassB otherB = Db.SQL<OtherClassB>("SELECT o FROM OtherClassB o").First;
                Assert.IsTrue(otherB != null);
                Assert.IsTrue(otherB.Name == null);

                Assert.IsTrue(otherA.Name == "Some");
                Assert.IsTrue(otherA.RefClass != null);

                otherB.Name = "Other";

                Assert.IsTrue(privateA.RefClass.Name == "Other");
                Assert.IsTrue(otherA.RefClass.Name == "Other");

                // Checking deletion.
                privateA.Delete();

                otherA = Db.SQL<OtherClassA>("SELECT o FROM OtherClassA o").First;
                Assert.IsTrue(otherA == null);

                otherB = Db.SQL<OtherClassB>("SELECT o FROM OtherClassB o").First;
                Assert.IsTrue(otherB == null);

                PrivateClassB privateB = Db.SQL<PrivateClassB>("SELECT o FROM PrivateClassB o").First;
                Assert.IsTrue(privateB == null);

                // Checking that all relations are also deleted.
                rel = Db.SQL<DbMappingRelation>("SELECT o FROM DbMappingRelation o").First;
                Assert.IsTrue(rel == null);
            });
        }
    }

    public class TestSeveralClassesUsage {

        public static void Map() {

            DbMapping.MapCreation("DbMappingTest.NameClass1", "DbMappingTest.NameClass2", (UInt64 fromOid) => {
                NameClass1 src = (NameClass1)DbHelper.FromID(fromOid);
                NameClass2 dst = new NameClass2();
                return dst.GetObjectNo(); // Newly created object ID.
            });

            DbMapping.MapCreation("DbMappingTest.NameClass1", "DbMappingTest.NameClass3", (UInt64 fromOid) => {
                NameClass1 src = (NameClass1)DbHelper.FromID(fromOid);
                NameClass3 dst = new NameClass3();
                return dst.GetObjectNo(); // Newly created object ID.
            });

            DbMapping.MapCreation("DbMappingTest.NameClass3", "DbMappingTest.NameClass4", (UInt64 fromOid) => {
                NameClass3 src = (NameClass3)DbHelper.FromID(fromOid);
                NameClass4 dst = new NameClass4();
                return dst.GetObjectNo(); // Newly created object ID.
            });

            DbMapping.MapModification("DbMappingTest.NameClass1", "DbMappingTest.NameClass2", (UInt64 fromOid, UInt64 toOid) => {
                NameClass1 src = (NameClass1)DbHelper.FromID(fromOid);
                NameClass2 dst = (NameClass2)DbHelper.FromID(toOid);
                dst.FullName = src.FirstName + " " + src.LastName;
                dst.YoungerAge = src.Age - 5;
            });

            DbMapping.MapModification("DbMappingTest.NameClass1", "DbMappingTest.NameClass3", (UInt64 fromOid, UInt64 toOid) => {
                NameClass1 src = (NameClass1)DbHelper.FromID(fromOid);
                NameClass3 dst = (NameClass3)DbHelper.FromID(toOid);
                dst.FirstName = src.FirstName;
                dst.OlderAge = src.Age + 5;
            });

            DbMapping.MapModification("DbMappingTest.NameClass3", "DbMappingTest.NameClass4", (UInt64 fromOid, UInt64 toOid) => {
                NameClass3 src = (NameClass3)DbHelper.FromID(fromOid);
                NameClass4 dst = (NameClass4)DbHelper.FromID(toOid);
                dst.FirstName = "Haha" + src.FirstName;
                dst.Age = src.OlderAge - 5;
            });

            DbMapping.MapDeletion("DbMappingTest.NameClass1", "DbMappingTest.NameClass2", (UInt64 fromOid, UInt64 toOid) => {
                NameClass2 dst = (NameClass2)DbHelper.FromID(toOid);
                dst.Delete();
            });

            DbMapping.MapDeletion("DbMappingTest.NameClass1", "DbMappingTest.NameClass3", (UInt64 fromOid, UInt64 toOid) => {
                NameClass3 dst = (NameClass3)DbHelper.FromID(toOid);
                dst.Delete();
            });

            DbMapping.MapDeletion("DbMappingTest.NameClass3", "DbMappingTest.NameClass4", (UInt64 fromOid, UInt64 toOid) => {
                NameClass4 dst = (NameClass4)DbHelper.FromID(toOid);
                dst.Delete();
            });
        }

        public static void RunTest() {

            Db.Transact(() => {
                Db.SlowSQL("DELETE FROM NameClass1");
                Db.SlowSQL("DELETE FROM NameClass2");
                Db.SlowSQL("DELETE FROM NameClass3");
                Db.SlowSQL("DELETE FROM NameClass4");
            });

            Db.Transact(() => {

                // Checking that there are no existing relations.
                DbMappingRelation rel = Db.SQL<DbMappingRelation>("SELECT o FROM DbMappingRelation o").First;
                Assert.IsTrue(rel == null);

                NameClass1 nc1 = new NameClass1();
                NameClass2 nc2;
                NameClass3 nc3;
                NameClass4 nc4;

                Assert.IsTrue(Db.SQL<NameClass2>("SELECT o FROM NameClass2 o").First != null);
                Assert.IsTrue(Db.SQL<NameClass3>("SELECT o FROM NameClass3 o").First != null);

                nc1.FirstName = "John";
                nc1.Age = 30;

                nc2 = Db.SQL<NameClass2>("SELECT o FROM NameClass2 o").First;
                Assert.IsTrue(nc2.FullName == "John ");
                Assert.IsTrue(nc2.YoungerAge == 25);

                nc3 = Db.SQL<NameClass3>("SELECT o FROM NameClass3 o").First;
                Assert.IsTrue(nc3.FirstName == "John");
                Assert.IsTrue(nc3.OlderAge == 35);

                Assert.IsTrue(true == DbMapping.HasMappedObjects(nc1.GetObjectNo()));
                Assert.IsTrue(true == DbMapping.HasMappedObjects(nc2.GetObjectNo()));
                Assert.IsTrue(true == DbMapping.HasMappedObjects(nc3.GetObjectNo()));

                Assert.IsTrue(DbMapping.GetMappedObject<NameClass2>(nc1).GetObjectNo() == nc2.GetObjectNo());
                Assert.IsTrue(DbMapping.GetMappedObject<NameClass3>(nc1).GetObjectNo() == nc3.GetObjectNo());

                nc4 = Db.SQL<NameClass4>("SELECT o FROM NameClass4 o").First;
                Assert.IsTrue(nc4.FirstName == "HahaJohn");
                Assert.IsTrue(nc4.Age == 30);

                Assert.IsTrue(true == DbMapping.HasMappedObjects(nc4.GetObjectNo()));
                Assert.IsTrue(DbMapping.GetMappedObject<NameClass4>(nc3).GetObjectNo() == nc4.GetObjectNo());

                nc1.LastName = "Doe";

                nc2 = Db.SQL<NameClass2>("SELECT o FROM NameClass2 o").First;
                Assert.IsTrue(nc2.FullName == "John Doe");
                Assert.IsTrue(nc2.YoungerAge == 25);

                nc3 = Db.SQL<NameClass3>("SELECT o FROM NameClass3 o").First;
                Assert.IsTrue(nc3.FirstName == "John");
                Assert.IsTrue(nc3.OlderAge == 35);

                nc4 = Db.SQL<NameClass4>("SELECT o FROM NameClass4 o").First;
                Assert.IsTrue(nc4.FirstName == "HahaJohn");
                Assert.IsTrue(nc4.Age == 30);

                Assert.IsTrue(nc1.Age == 30);

                nc1 = new NameClass1();
                nc1.FirstName = "Ivan";
                nc1.Age = 70;

                nc2 = Db.SQL<NameClass2>("SELECT o FROM NameClass2 o WHERE o.FullName = ?", "Ivan ").First;
                Assert.IsTrue(nc2.FullName == "Ivan ");
                Assert.IsTrue(nc2.YoungerAge == 65);

                nc3 = Db.SQL<NameClass3>("SELECT o FROM NameClass3 o WHERE o.FirstName = ?", "Ivan").First;
                Assert.IsTrue(nc3.FirstName == "Ivan");
                Assert.IsTrue(nc3.OlderAge == 75);

                nc4 = Db.SQL<NameClass4>("SELECT o FROM NameClass4 o WHERE o.FirstName = ?", "HahaIvan").First;
                Assert.IsTrue(nc4.FirstName == "HahaIvan");
                Assert.IsTrue(nc4.Age == 70);

                // Checking that old instances are untouched.
                NameClass1 nc11 = Db.SQL<NameClass1>("SELECT o FROM NameClass1 o WHERE o.FirstName = ?", "John").First;
                Assert.IsTrue(nc11.FirstName == "John");
                Assert.IsTrue(nc11.Age == 30);

                nc2 = Db.SQL<NameClass2>("SELECT o FROM NameClass2 o WHERE o.FullName = ?", "John Doe").First;
                Assert.IsTrue(nc2.FullName == "John Doe");
                Assert.IsTrue(nc2.YoungerAge == 25);

                nc3 = Db.SQL<NameClass3>("SELECT o FROM NameClass3 o WHERE o.FirstName = ?", "John").First;
                Assert.IsTrue(nc3.FirstName == "John");
                Assert.IsTrue(nc3.OlderAge == 35);

                nc1.LastName = "Petrov";

                nc2 = Db.SQL<NameClass2>("SELECT o FROM NameClass2 o WHERE o.FullName = ?", "Ivan Petrov").First;
                Assert.IsTrue(nc2.FullName == "Ivan Petrov");
                Assert.IsTrue(nc2.YoungerAge == 65);

                nc3 = Db.SQL<NameClass3>("SELECT o FROM NameClass3 o WHERE o.FirstName = ?", "Ivan").First;
                Assert.IsTrue(nc3.FirstName == "Ivan");
                Assert.IsTrue(nc3.OlderAge == 75);

                nc4 = Db.SQL<NameClass4>("SELECT o FROM NameClass4 o WHERE o.FirstName = ?", "HahaIvan").First;
                Assert.IsTrue(nc4.FirstName == "HahaIvan");
                Assert.IsTrue(nc4.Age == 70);

                Assert.IsTrue(nc1.Age == 70);

                // Checking that old instances are untouched.
                nc11 = Db.SQL<NameClass1>("SELECT o FROM NameClass1 o WHERE o.FirstName = ?", "John").First;
                Assert.IsTrue(nc11.FirstName == "John");
                Assert.IsTrue(nc11.Age == 30);

                nc2 = Db.SQL<NameClass2>("SELECT o FROM NameClass2 o WHERE o.FullName = ?", "John Doe").First;
                Assert.IsTrue(nc2.FullName == "John Doe");
                Assert.IsTrue(nc2.YoungerAge == 25);

                nc3 = Db.SQL<NameClass3>("SELECT o FROM NameClass3 o WHERE o.FirstName = ?", "John").First;
                Assert.IsTrue(nc3.FirstName == "John");
                Assert.IsTrue(nc3.OlderAge == 35);

                // Deleting the object, and all related objects.
                nc1.Delete();

                // Checking that old instances are untouched.
                nc11 = Db.SQL<NameClass1>("SELECT o FROM NameClass1 o WHERE o.FirstName = ?", "John").First;
                Assert.IsTrue(nc11.FirstName == "John");
                nc2 = Db.SQL<NameClass2>("SELECT o FROM NameClass2 o WHERE o.FullName = ?", "John Doe").First;
                Assert.IsTrue(nc2.FullName == "John Doe");
                nc3 = Db.SQL<NameClass3>("SELECT o FROM NameClass3 o WHERE o.FirstName = ?", "John").First;
                Assert.IsTrue(nc3.FirstName == "John");

                // Checking that the original and all mapped objects are deleted.
                nc1 = Db.SQL<NameClass1>("SELECT o FROM NameClass1 o WHERE o.FirstName = ?", "Ivan").First;
                Assert.IsTrue(nc1 == null);
                nc2 = Db.SQL<NameClass2>("SELECT o FROM NameClass2 o WHERE o.FullName = ?", "Ivan Petrov").First;
                Assert.IsTrue(nc2 == null);
                nc3 = Db.SQL<NameClass3>("SELECT o FROM NameClass3 o WHERE o.FirstName = ?", "Ivan").First;
                Assert.IsTrue(nc3 == null);
                nc4 = Db.SQL<NameClass4>("SELECT o FROM NameClass4 o WHERE o.FirstName = ?", "HahaIvan").First;
                Assert.IsTrue(nc4 == null);

                nc1 = Db.SQL<NameClass1>("SELECT o FROM NameClass1 o WHERE o.FirstName = ?", "John").First;
                Assert.IsTrue(nc1.FirstName == "John");
                Assert.IsTrue(nc1.Age == 30);

                // Deleting the object, and all related objects.
                nc1.Delete();

                // Checking that the original and all mapped objects are deleted.
                nc1 = Db.SQL<NameClass1>("SELECT o FROM NameClass1 o WHERE o.FirstName = ?", "John").First;
                Assert.IsTrue(nc1 == null);
                nc2 = Db.SQL<NameClass2>("SELECT o FROM NameClass2 o WHERE o.FullName = ?", "John Doe").First;
                Assert.IsTrue(nc2 == null);
                nc3 = Db.SQL<NameClass3>("SELECT o FROM NameClass3 o WHERE o.FirstName = ?", "John").First;
                Assert.IsTrue(nc3 == null);

                // Checking that no objects are left.
                nc1 = Db.SQL<NameClass1>("SELECT o FROM NameClass1 o").First;
                Assert.IsTrue(nc1 == null);
                nc2 = Db.SQL<NameClass2>("SELECT o FROM NameClass2 o").First;
                Assert.IsTrue(nc2 == null);
                nc3 = Db.SQL<NameClass3>("SELECT o FROM NameClass3 o").First;
                Assert.IsTrue(nc3 == null);

                // Checking that all relations are also deleted.
                rel = Db.SQL<DbMappingRelation>("SELECT o FROM DbMappingRelation o").First;
                Assert.IsTrue(rel == null);
            });
        }
    }

    public class TestSeparateAppsEmulation {

        public static void RunTest() {

            App1.TestApp1.Init();
            App2.TestApp2.Init();

            App1.TestApp1.RunTest();

            App1.App1Person app1Person;
            App2.App2Person app2Person;

            app1Person = Db.SQL<App1.App1Person>("SELECT o FROM App1.App1Person o WHERE o.FirstName = ?", "John").First;
            App1.App1Person app1Person1Saved = app1Person;

            Assert.IsTrue(app1Person != null);
            Assert.IsTrue(app1Person.Age == 555);

            app2Person = Db.SQL<App2.App2Person>("SELECT o FROM App2.App2Person o WHERE o.Name = ?", "John Doe").First;
            Assert.IsTrue(app2Person != null);

            SharedClasses.MySomebody smb = Db.SQL<SharedClasses.MySomebody>("SELECT o FROM SharedClasses.MySomebody o WHERE o.Name = ?", "John Doe").First;
            Assert.IsTrue(smb != null);

            App2.TestApp2.RunTest();

            app1Person = Db.SQL<App1.App1Person>("SELECT o FROM App1.App1Person o WHERE o.FirstName = ?", "John").First;
            Assert.IsTrue(app1Person != null);
            Assert.IsTrue(app1Person.Age == 555);

            app2Person = Db.SQL<App2.App2Person>("SELECT o FROM App2.App2Person o WHERE o.Name = ?", "John Doe").First;
            Assert.IsTrue(app2Person != null);

            app1Person = Db.SQL<App1.App1Person>("SELECT o FROM App1.App1Person o WHERE o.FirstName = ?", "Hoho").First;
            Assert.IsTrue(app1Person != null);

            smb = Db.SQL<SharedClasses.MySomebody>("SELECT o FROM SharedClasses.MySomebody o WHERE o.Name = ?", "Hoho").First;
            Assert.IsTrue(smb != null);

            app2Person = Db.SQL<App2.App2Person>("SELECT o FROM App2.App2Person o WHERE o.Name = ?", "Hoho").First;
            Assert.IsTrue(app2Person != null);

            Db.Transact(() => {
                app1Person1Saved.Delete();
                app2Person.Delete();
            });

            app2Person = Db.SQL<App2.App2Person>("SELECT o FROM App2.App2Person o").First;
            Assert.IsTrue(app2Person == null);

            app1Person = Db.SQL<App1.App1Person>("SELECT o FROM App1.App1Person o").First;
            Assert.IsTrue(app1Person == null);

            DbMappingRelation rel = Db.SQL<DbMappingRelation>("SELECT o FROM DbMappingRelation o").First;
            Assert.IsTrue(rel == null);
        }
    }

    public class TestMapExisting {

        public static void RunTest() {

            // Checking that there are no existing relations.
            DbMappingRelation rel = Db.SQL<DbMappingRelation>("SELECT o FROM DbMappingRelation o").First;
            Assert.IsTrue(rel == null);

            App1.TestApp1.Init();
            App2.TestApp2.Init();

            App1.App1Person app1Person = null;
            App2.App2Person app2Person = null;
            SharedClasses.MySomebody smb;

            // Turning off mapping.
            MapConfig.Enabled = false;

            Db.Transact(() => {
                app1Person = new App1.App1Person();
                Assert.IsTrue(null != app1Person);

                app1Person.FirstName = "John";
                app1Person.LastName = "Doe";
                app1Person.Age = 555;
            });

            app1Person = Db.SQL<App1.App1Person>("SELECT o FROM App1.App1Person o WHERE o.FirstName = ?", "John").First;
            Assert.IsTrue(app1Person != null);

            app2Person = Db.SQL<App2.App2Person>("SELECT o FROM App2.App2Person o").First;
            Assert.IsTrue(app2Person == null);

            smb = Db.SQL<SharedClasses.MySomebody>("SELECT o FROM SharedClasses.MySomebody o").First;
            Assert.IsTrue(smb == null);

            // Turning on mapping.
            MapConfig.Enabled = true;

            // Triggering mapping of existing objects.
            DbMapping.MapExistingObjects();

            app1Person = Db.SQL<App1.App1Person>("SELECT o FROM App1.App1Person o WHERE o.FirstName = ?", "John").First;
            Assert.IsTrue(app1Person != null);
            Assert.IsTrue(1 == Utils.NumObjects<App1.App1Person>());

            app2Person = Db.SQL<App2.App2Person>("SELECT o FROM App2.App2Person o WHERE o.Name = ?", "John Doe").First;
            Assert.IsTrue(app2Person != null);
            Assert.IsTrue(1 == Utils.NumObjects<App2.App2Person>());

            smb = Db.SQL<SharedClasses.MySomebody>("SELECT o FROM SharedClasses.MySomebody o WHERE o.Name = ?", "John Doe").First;
            Assert.IsTrue(smb != null);
            Assert.IsTrue(1 == Utils.NumObjects<SharedClasses.MySomebody>());

            // Again triggering mapping of existing objects.
            // NOTE: Nothing should change.
            DbMapping.MapExistingObjects();

            app1Person = Db.SQL<App1.App1Person>("SELECT o FROM App1.App1Person o WHERE o.FirstName = ?", "John").First;
            Assert.IsTrue(app1Person != null);
            Assert.IsTrue(1 == Utils.NumObjects<App1.App1Person>());

            app2Person = Db.SQL<App2.App2Person>("SELECT o FROM App2.App2Person o WHERE o.Name = ?", "John Doe").First;
            Assert.IsTrue(app2Person != null);
            Assert.IsTrue(1 == Utils.NumObjects<App2.App2Person>());

            smb = Db.SQL<SharedClasses.MySomebody>("SELECT o FROM SharedClasses.MySomebody o WHERE o.Name = ?", "John Doe").First;
            Assert.IsTrue(smb != null);
            Assert.IsTrue(1 == Utils.NumObjects<SharedClasses.MySomebody>());

            // Now disabling mapping and deleting one object, relations and corresponding object should remain untoched.
            MapConfig.Enabled = false;

            Db.Transact(() => {
                app2Person.Delete();
            });

            app2Person = Db.SQL<App2.App2Person>("SELECT o FROM App2.App2Person o").First;
            Assert.IsTrue(app2Person == null);

            app1Person = Db.SQL<App1.App1Person>("SELECT o FROM App1.App1Person o WHERE o.FirstName = ?", "John").First;
            Assert.IsTrue(app1Person != null);
            Assert.IsTrue(1 == Utils.NumObjects<App1.App1Person>());

            smb = Db.SQL<SharedClasses.MySomebody>("SELECT o FROM SharedClasses.MySomebody o WHERE o.Name = ?", "John Doe").First;
            Assert.IsTrue(smb != null);
            Assert.IsTrue(1 == Utils.NumObjects<SharedClasses.MySomebody>());

            // Reenabling mapping flag.
            MapConfig.Enabled = true;

            // NOTE: Now we trigger mapping again and the orphaned classes should be deleted.
            DbMapping.MapExistingObjects();

            // Now everything should be deleted.
            app2Person = Db.SQL<App2.App2Person>("SELECT o FROM App2.App2Person o").First;
            Assert.IsTrue(app2Person == null);

            app1Person = Db.SQL<App1.App1Person>("SELECT o FROM App1.App1Person o").First;
            Assert.IsTrue(app1Person == null);

            smb = Db.SQL<SharedClasses.MySomebody>("SELECT o FROM SharedClasses.MySomebody o").First;
            Assert.IsTrue(smb == null);

            rel = Db.SQL<DbMappingRelation>("SELECT o FROM DbMappingRelation o").First;
            Assert.IsTrue(rel == null);
        }
    }

    public class DbMappingTest {

        public static void Main() {

            // Database mapping initialization.
            DbMapping.Init();

            TestCrossDeletion.Map();
            TestSeveralClassesUsage.Map();

            TestCrossDeletion.RunTest();
            TestSeveralClassesUsage.RunTest();

            App1.TestApp1.Map();
            App2.TestApp2.Map();

            TestSeparateAppsEmulation.RunTest();
            TestMapExisting.RunTest();

            ABCTestClass.RunTest();

            TestTemplatedMapping.Map();
        }
    }
}
