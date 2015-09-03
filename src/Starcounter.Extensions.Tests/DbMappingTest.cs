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

namespace DbMappingTest {

    [Database]
    public class NameClass1 {
        public String FirstName;
        public String LastName;
    }

    [Database]
    public class NameClass2 {
        public String FullName;
    }

    [Database]
    public class NameClass3 {
        public String FirstName;
    }

    [Database]
    public class NameClass4 {
        public String FirstName;
    }

    public class DbMappingTest {

        public static void Main() {

            Db.Transact(() => {
                Db.SlowSQL("DELETE FROM NameClass1");
                Db.SlowSQL("DELETE FROM NameClass2");
                Db.SlowSQL("DELETE FROM NameClass3");
                Db.SlowSQL("DELETE FROM NameClass4");
            });

            // Database mapping initialization.
            DbMapping.Init();

            //Debugger.Launch();

            DbMapping.MapModification("/DbMappingTest.NameClass1/{?}", "/DbMappingTest.NameClass2/{?}", (UInt64 fromOid, UInt64 toOid) => {
                NameClass1 src = (NameClass1)DbHelper.FromID(fromOid);
                NameClass2 dst = (NameClass2)DbHelper.FromID(toOid);
                dst.FullName = src.FirstName + " " + src.LastName;
            });

            DbMapping.MapModification("/DbMappingTest.NameClass1/{?}", "/DbMappingTest.NameClass3/{?}", (UInt64 fromOid, UInt64 toOid) => {
                NameClass1 src = (NameClass1)DbHelper.FromID(fromOid);
                NameClass3 dst = (NameClass3)DbHelper.FromID(toOid);
                dst.FirstName = src.FirstName;
            });

            DbMapping.MapModification("/DbMappingTest.NameClass3/{?}", "/DbMappingTest.NameClass4/{?}", (UInt64 fromOid, UInt64 toOid) => {
                NameClass3 src = (NameClass3)DbHelper.FromID(fromOid);
                NameClass4 dst = (NameClass4)DbHelper.FromID(toOid);
                dst.FirstName = "Haha" + src.FirstName;
            });

            DbMapping.MapDeletion("/DbMappingTest.NameClass1/{?}", "/DbMappingTest.NameClass2/{?}", (UInt64 fromOid, UInt64 toOid) => {
                NameClass2 dst = (NameClass2)DbHelper.FromID(toOid);
                dst.Delete();
            });

            DbMapping.MapDeletion("/DbMappingTest.NameClass1/{?}", "/DbMappingTest.NameClass3/{?}", (UInt64 fromOid, UInt64 toOid) => {
                NameClass3 dst = (NameClass3)DbHelper.FromID(toOid);
                dst.Delete();
            });

            DbMapping.MapDeletion("/DbMappingTest.NameClass3/{?}", "/DbMappingTest.NameClass4/{?}", (UInt64 fromOid, UInt64 toOid) => {
                NameClass4 dst = (NameClass4)DbHelper.FromID(toOid);
                dst.Delete();
            });

            DbMapping.MapCreation("/DbMappingTest.NameClass1/{?}", "/DbMappingTest.NameClass2/{?}", (UInt64 fromOid) => {
                NameClass1 src = (NameClass1)DbHelper.FromID(fromOid);
                NameClass2 dst = new NameClass2();
                return dst.GetObjectNo(); // Newly created object ID.
            });

            DbMapping.MapCreation("/DbMappingTest.NameClass1/{?}", "/DbMappingTest.NameClass3/{?}", (UInt64 fromOid) => {
                NameClass1 src = (NameClass1)DbHelper.FromID(fromOid);
                NameClass3 dst = new NameClass3();
                return dst.GetObjectNo(); // Newly created object ID.
            });

            DbMapping.MapCreation("/DbMappingTest.NameClass3/{?}", "/DbMappingTest.NameClass4/{?}", (UInt64 fromOid) => {
                NameClass3 src = (NameClass3)DbHelper.FromID(fromOid);
                NameClass4 dst = new NameClass4();
                return dst.GetObjectNo(); // Newly created object ID.
            });
            
            Db.Transact(() => {
                NameClass1 nc1 = new NameClass1();
                NameClass2 nc2;
                NameClass3 nc3;
                NameClass4 nc4;
                List<UInt64> mappedOids;

                Assert.IsTrue(Db.SQL<NameClass2>("SELECT o FROM NameClass2 o").First != null);
                Assert.IsTrue(Db.SQL<NameClass3>("SELECT o FROM NameClass3 o").First != null);

                nc1.FirstName = "John";

                nc2 = Db.SQL<NameClass2>("SELECT o FROM NameClass2 o").First;
                Assert.IsTrue(nc2.FullName == "John ");

                nc3 = Db.SQL<NameClass3>("SELECT o FROM NameClass3 o").First;
                Assert.IsTrue(nc3.FirstName == "John");

                Assert.IsTrue(true == DbMapping.HasMappedObjects(nc1.GetObjectNo()));
                Assert.IsTrue(true == DbMapping.HasMappedObjects(nc2.GetObjectNo()));
                Assert.IsTrue(true == DbMapping.HasMappedObjects(nc3.GetObjectNo()));

                mappedOids = DbMapping.GetMappedOids(nc1.GetObjectNo());
                Assert.IsTrue(mappedOids[0] == nc2.GetObjectNo());
                Assert.IsTrue(mappedOids[1] == nc3.GetObjectNo());

                nc4 = Db.SQL<NameClass4>("SELECT o FROM NameClass4 o").First;
                Assert.IsTrue(nc4.FirstName == "HahaJohn");

                Assert.IsTrue(true == DbMapping.HasMappedObjects(nc4.GetObjectNo()));
                mappedOids = DbMapping.GetMappedOids(nc3.GetObjectNo());
                Assert.IsTrue(mappedOids[0] == nc4.GetObjectNo());

                nc1.LastName = "Doe";

                nc2 = Db.SQL<NameClass2>("SELECT o FROM NameClass2 o").First;
                Assert.IsTrue(nc2.FullName == "John Doe");
                nc3 = Db.SQL<NameClass3>("SELECT o FROM NameClass3 o").First;
                Assert.IsTrue(nc3.FirstName == "John");
                nc4 = Db.SQL<NameClass4>("SELECT o FROM NameClass4 o").First;
                Assert.IsTrue(nc4.FirstName == "HahaJohn");

                nc1 = new NameClass1();
                nc1.FirstName = "Ivan";

                nc2 = Db.SQL<NameClass2>("SELECT o FROM NameClass2 o WHERE o.FullName = ?", "Ivan ").First;
                Assert.IsTrue(nc2.FullName == "Ivan ");
                nc3 = Db.SQL<NameClass3>("SELECT o FROM NameClass3 o WHERE o.FirstName = ?", "Ivan").First;
                Assert.IsTrue(nc3.FirstName == "Ivan");
                nc4 = Db.SQL<NameClass4>("SELECT o FROM NameClass4 o WHERE o.FirstName = ?", "HahaIvan").First;
                Assert.IsTrue(nc4.FirstName == "HahaIvan");

                // Checking that old instances are untouched.
                NameClass1 nc11 = Db.SQL<NameClass1>("SELECT o FROM NameClass1 o WHERE o.FirstName = ?", "John").First;
                Assert.IsTrue(nc11.FirstName == "John");
                nc2 = Db.SQL<NameClass2>("SELECT o FROM NameClass2 o WHERE o.FullName = ?", "John Doe").First;
                Assert.IsTrue(nc2.FullName == "John Doe");
                nc3 = Db.SQL<NameClass3>("SELECT o FROM NameClass3 o WHERE o.FirstName = ?", "John").First;
                Assert.IsTrue(nc3.FirstName == "John");

                nc1.LastName = "Petrov";

                nc2 = Db.SQL<NameClass2>("SELECT o FROM NameClass2 o WHERE o.FullName = ?", "Ivan Petrov").First;
                Assert.IsTrue(nc2.FullName == "Ivan Petrov");
                nc3 = Db.SQL<NameClass3>("SELECT o FROM NameClass3 o WHERE o.FirstName = ?", "Ivan").First;
                Assert.IsTrue(nc3.FirstName == "Ivan");
                nc4 = Db.SQL<NameClass4>("SELECT o FROM NameClass4 o WHERE o.FirstName = ?", "HahaIvan").First;
                Assert.IsTrue(nc4.FirstName == "HahaIvan");

                // Checking that old instances are untouched.
                nc11 = Db.SQL<NameClass1>("SELECT o FROM NameClass1 o WHERE o.FirstName = ?", "John").First;
                Assert.IsTrue(nc11.FirstName == "John");
                nc2 = Db.SQL<NameClass2>("SELECT o FROM NameClass2 o WHERE o.FullName = ?", "John Doe").First;
                Assert.IsTrue(nc2.FullName == "John Doe");
                nc3 = Db.SQL<NameClass3>("SELECT o FROM NameClass3 o WHERE o.FirstName = ?", "John").First;
                Assert.IsTrue(nc3.FirstName == "John");

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
                DbMappingRelation rel = Db.SQL<DbMappingRelation>("SELECT o FROM DbMappingRelation o").First;
                Assert.IsTrue(rel == null);
            });
        }
    }
}
