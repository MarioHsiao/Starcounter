using System;
using Starcounter;
using Starcounter.Extensions;
using Starcounter.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DbMappingTest;
using System.Diagnostics;

namespace ABCTest {

    [Database]
    public class A {
        public String FirstName;
        public String LastName;
        public Int32 Age;
    }

    [Database]
    public class B {
        public String Name;
        public Int32 YoungerAge;
    }

    [Database]
    public class C {
        public String LastName;
        public Int32 OlderAge;
    }

    public class ABCTestClass {

        public static void Map() {

            // Mapping is the following: A <-> B <-> C.
            // When A is created - B should be created, which in turns creates C.

            DbMapping.MapCreation("/ABCTest.A/{?}", "/ABCTest.B/{?}", (UInt64 fromOid) => {
                ABCTest.B dst = new ABCTest.B();
                return dst.GetObjectNo();
            });

            DbMapping.MapCreation("/ABCTest.B/{?}", "/ABCTest.A/{?}", (UInt64 fromOid) => {
                ABCTest.A dst = new ABCTest.A();
                return dst.GetObjectNo();
            });

            DbMapping.MapCreation("/ABCTest.C/{?}", "/ABCTest.B/{?}", (UInt64 fromOid) => {
                ABCTest.B dst = new ABCTest.B();
                return dst.GetObjectNo();
            });

            DbMapping.MapCreation("/ABCTest.B/{?}", "/ABCTest.C/{?}", (UInt64 fromOid) => {
                ABCTest.C dst = new ABCTest.C();
                return dst.GetObjectNo();
            });

            DbMapping.MapDeletion("/ABCTest.A/{?}", "/ABCTest.B/{?}", (UInt64 fromOid, UInt64 toOid) => {
                ABCTest.B dst = (ABCTest.B)DbHelper.FromID(toOid);
                dst.Delete();
            });

            DbMapping.MapDeletion("/ABCTest.B/{?}", "/ABCTest.A/{?}", (UInt64 fromOid, UInt64 toOid) => {
                ABCTest.A dst = (ABCTest.A)DbHelper.FromID(toOid);
                dst.Delete();
            });

            DbMapping.MapDeletion("/ABCTest.C/{?}", "/ABCTest.B/{?}", (UInt64 fromOid, UInt64 toOid) => {
                ABCTest.B dst = (ABCTest.B)DbHelper.FromID(toOid);
                dst.Delete();
            });

            DbMapping.MapDeletion("/ABCTest.B/{?}", "/ABCTest.C/{?}", (UInt64 fromOid, UInt64 toOid) => {
                ABCTest.C dst = (ABCTest.C)DbHelper.FromID(toOid);
                dst.Delete();
            });

            DbMapping.MapModification("/ABCTest.A/{?}", "/ABCTest.B/{?}", (UInt64 fromOid, UInt64 toOid) => {
                ABCTest.A a = (ABCTest.A)DbHelper.FromID(fromOid);
                ABCTest.B b = (ABCTest.B)DbHelper.FromID(toOid);
                b.Name = a.FirstName + " " + a.LastName;
                b.YoungerAge = a.Age - 5;
            });

            DbMapping.MapModification("/ABCTest.B/{?}", "/ABCTest.A/{?}", (UInt64 fromOid, UInt64 toOid) => {
                ABCTest.B b = (ABCTest.B)DbHelper.FromID(fromOid);
                ABCTest.A a = (ABCTest.A)DbHelper.FromID(toOid);

                if (!String.IsNullOrEmpty(b.Name)) {
                    String[] str = b.Name.Split();
                    a.FirstName = str[0];
                    if (str.Length > 1) {
                        a.LastName = str[1];
                    }
                }

                a.Age = b.YoungerAge + 5;
            });

            DbMapping.MapModification("/ABCTest.C/{?}", "/ABCTest.B/{?}", (UInt64 fromOid, UInt64 toOid) => {
                ABCTest.C c = (ABCTest.C)DbHelper.FromID(fromOid);
                ABCTest.B b = (ABCTest.B)DbHelper.FromID(toOid);
                b.YoungerAge = c.OlderAge - 10;
            });

            DbMapping.MapModification("/ABCTest.B/{?}", "/ABCTest.C/{?}", (UInt64 fromOid, UInt64 toOid) => {
                ABCTest.B b = (ABCTest.B)DbHelper.FromID(fromOid);
                ABCTest.C c = (ABCTest.C)DbHelper.FromID(toOid);

                if (!String.IsNullOrEmpty(b.Name)) {
                    String[] str = b.Name.Split();
                    if (str.Length > 1) {
                        c.LastName = str[1];
                    }
                }

                c.OlderAge = b.YoungerAge + 10;
            });
        }

        public static void TestCreateA() {

            ABCTest.A a;
            ABCTest.B b;
            ABCTest.C c;

            Db.Transact(() => {
                a = new ABCTest.A();
                a.FirstName = "John";
                a.LastName = "Doe";
                a.Age = 55;
            });

            a = Db.SQL<ABCTest.A>("SELECT o FROM ABCTest.A o WHERE o.FirstName = ?", "John").First;
            Assert.IsTrue(a != null);
            Assert.IsTrue(1 == Utils.NumObjects<ABCTest.A>());
            Assert.IsTrue(a.Age == 55);

            b = Db.SQL<ABCTest.B>("SELECT o FROM ABCTest.B o WHERE o.Name = ?", "John Doe").First;
            Assert.IsTrue(b != null);
            Assert.IsTrue(1 == Utils.NumObjects<ABCTest.B>());
            Assert.IsTrue(b.YoungerAge == 50);

            c = Db.SQL<ABCTest.C>("SELECT o FROM ABCTest.C o WHERE o.LastName = ?", "Doe").First;
            Assert.IsTrue(c != null);
            Assert.IsTrue(1 == Utils.NumObjects<ABCTest.C>());
            Assert.IsTrue(c.OlderAge == 60);

            Db.Transact(() => {
                a.Delete();
            });

            Assert.IsTrue(0 == Utils.NumObjects<DbMappingRelation>());
            Assert.IsTrue(0 == Utils.NumObjects<ABCTest.A>());
            Assert.IsTrue(0 == Utils.NumObjects<ABCTest.B>());
            Assert.IsTrue(0 == Utils.NumObjects<ABCTest.C>());
        }

        public static void TestCreateADeleteTwoAndMapExistingObjects() {

            // Disabling mapping so only one object is created.
            MapConfig.Enabled = false;

            ABCTest.A a;
            ABCTest.B b;
            ABCTest.C c;

            Db.Transact(() => {
                a = new ABCTest.A();
                a.FirstName = "John";
                a.LastName = "Doe";
                a.Age = 55;
            });

            Assert.IsTrue(0 == Utils.NumObjects<DbMappingRelation>());
            Assert.IsTrue(0 == Utils.NumObjects<ABCTest.B>());
            Assert.IsTrue(0 == Utils.NumObjects<ABCTest.C>());

            // Now enabling the mapping and mapping existing objects, so other objects will be created.
            MapConfig.Enabled = true;
            DbMapping.MapExistingObjects();

            a = Db.SQL<ABCTest.A>("SELECT o FROM ABCTest.A o WHERE o.FirstName = ?", "John").First;
            Assert.IsTrue(a != null);
            Assert.IsTrue(1 == Utils.NumObjects<ABCTest.A>());
            Assert.IsTrue(a.Age == 55);

            b = Db.SQL<ABCTest.B>("SELECT o FROM ABCTest.B o WHERE o.Name = ?", "John Doe").First;
            Assert.IsTrue(b != null);
            Assert.IsTrue(1 == Utils.NumObjects<ABCTest.B>());
            Assert.IsTrue(b.YoungerAge == 50);

            c = Db.SQL<ABCTest.C>("SELECT o FROM ABCTest.C o WHERE o.LastName = ?", "Doe").First;
            Assert.IsTrue(c != null);
            Assert.IsTrue(1 == Utils.NumObjects<ABCTest.C>());
            Assert.IsTrue(c.OlderAge == 60);

            // Disabling mapping so only one object is deleted.
            MapConfig.Enabled = false;

            // Now deleting both objects, so that orphaned relations are left.
            Db.Transact(() => {
                a.Delete();
                b.Delete();
            });

            Assert.IsTrue(0 != Utils.NumObjects<DbMappingRelation>());

            c = Db.SQL<ABCTest.C>("SELECT o FROM ABCTest.C o WHERE o.LastName = ?", "Doe").First;
            Assert.IsTrue(c != null);
            Assert.IsTrue(1 == Utils.NumObjects<ABCTest.C>());
            Assert.IsTrue(c.OlderAge == 60);

            // Now enabling the mapping and mapping existing objects, so everything should be deleted.
            MapConfig.Enabled = true;
            DbMapping.MapExistingObjects();

            Assert.IsTrue(0 == Utils.NumObjects<DbMappingRelation>());
            Assert.IsTrue(0 == Utils.NumObjects<ABCTest.A>());
            Assert.IsTrue(0 == Utils.NumObjects<ABCTest.B>());
            Assert.IsTrue(0 == Utils.NumObjects<ABCTest.C>());
        }

        public static void TestCreateAAndMapExistingObjects() {

            // Disabling mapping so only one object is created.
            MapConfig.Enabled = false;

            ABCTest.A a;
            ABCTest.B b;
            ABCTest.C c;

            Db.Transact(() => {
                a = new ABCTest.A();
                a.FirstName = "John";
                a.LastName = "Doe";
                a.Age = 55;
            });

            Assert.IsTrue(0 == Utils.NumObjects<DbMappingRelation>());
            Assert.IsTrue(0 == Utils.NumObjects<ABCTest.B>());
            Assert.IsTrue(0 == Utils.NumObjects<ABCTest.C>());

            // Now enabling the mapping and mapping existing objects, so other objects will be created.
            MapConfig.Enabled = true;
            DbMapping.MapExistingObjects();

            a = Db.SQL<ABCTest.A>("SELECT o FROM ABCTest.A o WHERE o.FirstName = ?", "John").First;
            Assert.IsTrue(a != null);
            Assert.IsTrue(1 == Utils.NumObjects<ABCTest.A>());
            Assert.IsTrue(a.Age == 55);

            b = Db.SQL<ABCTest.B>("SELECT o FROM ABCTest.B o WHERE o.Name = ?", "John Doe").First;
            Assert.IsTrue(b != null);
            Assert.IsTrue(1 == Utils.NumObjects<ABCTest.B>());
            Assert.IsTrue(b.YoungerAge == 50);

            c = Db.SQL<ABCTest.C>("SELECT o FROM ABCTest.C o WHERE o.LastName = ?", "Doe").First;
            Assert.IsTrue(c != null);
            Assert.IsTrue(1 == Utils.NumObjects<ABCTest.C>());
            Assert.IsTrue(c.OlderAge == 60);

            // Disabling mapping so only one object is deleted.
            MapConfig.Enabled = false;

            Db.Transact(() => {
                a.Delete();
            });

            Assert.IsTrue(0 != Utils.NumObjects<DbMappingRelation>());

            b = Db.SQL<ABCTest.B>("SELECT o FROM ABCTest.B o WHERE o.Name = ?", "John Doe").First;
            Assert.IsTrue(b != null);
            Assert.IsTrue(1 == Utils.NumObjects<ABCTest.B>());
            Assert.IsTrue(b.YoungerAge == 50);

            c = Db.SQL<ABCTest.C>("SELECT o FROM ABCTest.C o WHERE o.LastName = ?", "Doe").First;
            Assert.IsTrue(c != null);
            Assert.IsTrue(1 == Utils.NumObjects<ABCTest.C>());
            Assert.IsTrue(c.OlderAge == 60);

            // Now enabling the mapping and mapping existing objects, so everything should be deleted.
            MapConfig.Enabled = true;
            DbMapping.MapExistingObjects();

            Assert.IsTrue(0 == Utils.NumObjects<DbMappingRelation>());
            Assert.IsTrue(0 == Utils.NumObjects<ABCTest.A>());
            Assert.IsTrue(0 == Utils.NumObjects<ABCTest.B>());
            Assert.IsTrue(0 == Utils.NumObjects<ABCTest.C>());
        }

        public static void TestCreateB() {

            ABCTest.A a;
            ABCTest.B b;
            ABCTest.C c;

            Db.Transact(() => {
                b = new ABCTest.B();
                b.Name = "John Doe";
                b.YoungerAge = 50;
            });

            a = Db.SQL<ABCTest.A>("SELECT o FROM ABCTest.A o WHERE o.FirstName = ?", "John").First;
            Assert.IsTrue(a != null);
            Assert.IsTrue(1 == Utils.NumObjects<ABCTest.A>());
            Assert.IsTrue(a.Age == 55);

            b = Db.SQL<ABCTest.B>("SELECT o FROM ABCTest.B o WHERE o.Name = ?", "John Doe").First;
            Assert.IsTrue(b != null);
            Assert.IsTrue(1 == Utils.NumObjects<ABCTest.B>());
            Assert.IsTrue(b.YoungerAge == 50);

            c = Db.SQL<ABCTest.C>("SELECT o FROM ABCTest.C o WHERE o.LastName = ?", "Doe").First;
            Assert.IsTrue(c != null);
            Assert.IsTrue(1 == Utils.NumObjects<ABCTest.C>());
            Assert.IsTrue(c.OlderAge == 60);

            Db.Transact(() => {
                b.Delete();
            });

            Assert.IsTrue(0 == Utils.NumObjects<DbMappingRelation>());
            Assert.IsTrue(0 == Utils.NumObjects<ABCTest.A>());
            Assert.IsTrue(0 == Utils.NumObjects<ABCTest.B>());
            Assert.IsTrue(0 == Utils.NumObjects<ABCTest.C>());
        }

        public static void TestCreateBAndMapExistingObjects() {

            // Disabling mapping so only one object is created.
            MapConfig.Enabled = false;

            ABCTest.A a;
            ABCTest.B b;
            ABCTest.C c;

            Db.Transact(() => {
                b = new ABCTest.B();
                b.Name = "John Doe";
                b.YoungerAge = 50;
            });

            Assert.IsTrue(0 == Utils.NumObjects<DbMappingRelation>());
            Assert.IsTrue(0 == Utils.NumObjects<ABCTest.A>());
            Assert.IsTrue(0 == Utils.NumObjects<ABCTest.C>());

            // Now enabling the mapping and mapping existing objects, so other objects will be created.
            MapConfig.Enabled = true;
            DbMapping.MapExistingObjects();

            a = Db.SQL<ABCTest.A>("SELECT o FROM ABCTest.A o WHERE o.FirstName = ?", "John").First;
            Assert.IsTrue(a != null);
            Assert.IsTrue(1 == Utils.NumObjects<ABCTest.A>());
            Assert.IsTrue(a.Age == 55);

            b = Db.SQL<ABCTest.B>("SELECT o FROM ABCTest.B o WHERE o.Name = ?", "John Doe").First;
            Assert.IsTrue(b != null);
            Assert.IsTrue(1 == Utils.NumObjects<ABCTest.B>());
            Assert.IsTrue(b.YoungerAge == 50);

            c = Db.SQL<ABCTest.C>("SELECT o FROM ABCTest.C o WHERE o.LastName = ?", "Doe").First;
            Assert.IsTrue(c != null);
            Assert.IsTrue(1 == Utils.NumObjects<ABCTest.C>());
            Assert.IsTrue(c.OlderAge == 60);

            // Disabling mapping so only one object is deleted.
            MapConfig.Enabled = false;

            Db.Transact(() => {
                b.Delete();
            });

            Assert.IsTrue(0 != Utils.NumObjects<DbMappingRelation>());

            a = Db.SQL<ABCTest.A>("SELECT o FROM ABCTest.A o WHERE o.FirstName = ?", "John").First;
            Assert.IsTrue(a != null);
            Assert.IsTrue(1 == Utils.NumObjects<ABCTest.A>());
            Assert.IsTrue(a.Age == 55);

            c = Db.SQL<ABCTest.C>("SELECT o FROM ABCTest.C o WHERE o.LastName = ?", "Doe").First;
            Assert.IsTrue(c != null);
            Assert.IsTrue(1 == Utils.NumObjects<ABCTest.C>());
            Assert.IsTrue(c.OlderAge == 60);

            // Now enabling the mapping and mapping existing objects, so everything should be deleted.
            MapConfig.Enabled = true;
            DbMapping.MapExistingObjects();

            Assert.IsTrue(0 == Utils.NumObjects<DbMappingRelation>());
            Assert.IsTrue(0 == Utils.NumObjects<ABCTest.A>());
            Assert.IsTrue(0 == Utils.NumObjects<ABCTest.B>());
            Assert.IsTrue(0 == Utils.NumObjects<ABCTest.C>());
        }

        public static void TestCreateC() {

            ABCTest.A a;
            ABCTest.B b;
            ABCTest.C c;

            Db.Transact(() => {
                c = new ABCTest.C();
                c.LastName = "Doe";
                c.OlderAge = 60;
            });

            a = Db.SQL<ABCTest.A>("SELECT o FROM ABCTest.A o").First;
            Assert.IsTrue(a != null);
            Assert.IsTrue(1 == Utils.NumObjects<ABCTest.A>());
            Assert.IsTrue(a.Age == 55);

            b = Db.SQL<ABCTest.B>("SELECT o FROM ABCTest.B o").First;
            Assert.IsTrue(b != null);
            Assert.IsTrue(1 == Utils.NumObjects<ABCTest.B>());
            Assert.IsTrue(b.YoungerAge == 50);

            c = Db.SQL<ABCTest.C>("SELECT o FROM ABCTest.C o WHERE o.LastName = ?", "Doe").First;
            Assert.IsTrue(c != null);
            Assert.IsTrue(1 == Utils.NumObjects<ABCTest.C>());
            Assert.IsTrue(c.OlderAge == 60);

            Db.Transact(() => {
                c.Delete();
            });

            Assert.IsTrue(0 == Utils.NumObjects<DbMappingRelation>());
            Assert.IsTrue(0 == Utils.NumObjects<ABCTest.A>());
            Assert.IsTrue(0 == Utils.NumObjects<ABCTest.B>());
            Assert.IsTrue(0 == Utils.NumObjects<ABCTest.C>());
        }

        public static void TestCreateCAndMapExistingObjects() {

            // Disabling mapping so only one object is created.
            MapConfig.Enabled = false;

            ABCTest.A a;
            ABCTest.B b;
            ABCTest.C c;

            Db.Transact(() => {
                c = new ABCTest.C();
                c.LastName = "Doe";
                c.OlderAge = 60;
            });

            Assert.IsTrue(0 == Utils.NumObjects<DbMappingRelation>());
            Assert.IsTrue(0 == Utils.NumObjects<ABCTest.B>());
            Assert.IsTrue(0 == Utils.NumObjects<ABCTest.A>());

            // Now enabling the mapping and mapping existing objects, so other objects will be created.
            MapConfig.Enabled = true;
            DbMapping.MapExistingObjects();

            a = Db.SQL<ABCTest.A>("SELECT o FROM ABCTest.A o").First;
            Assert.IsTrue(a != null);
            Assert.IsTrue(1 == Utils.NumObjects<ABCTest.A>());
            Assert.IsTrue(a.Age == 55);

            b = Db.SQL<ABCTest.B>("SELECT o FROM ABCTest.B o").First;
            Assert.IsTrue(b != null);
            Assert.IsTrue(1 == Utils.NumObjects<ABCTest.B>());
            Assert.IsTrue(b.YoungerAge == 50);

            c = Db.SQL<ABCTest.C>("SELECT o FROM ABCTest.C o WHERE o.LastName = ?", "Doe").First;
            Assert.IsTrue(c != null);
            Assert.IsTrue(1 == Utils.NumObjects<ABCTest.C>());
            Assert.IsTrue(c.OlderAge == 60);

            // Disabling mapping so only one object is deleted.
            MapConfig.Enabled = false;

            Db.Transact(() => {
                c.Delete();
            });

            Assert.IsTrue(0 != Utils.NumObjects<DbMappingRelation>());

            a = Db.SQL<ABCTest.A>("SELECT o FROM ABCTest.A o").First;
            Assert.IsTrue(a != null);
            Assert.IsTrue(1 == Utils.NumObjects<ABCTest.A>());
            Assert.IsTrue(a.Age == 55);

            b = Db.SQL<ABCTest.B>("SELECT o FROM ABCTest.B o").First;
            Assert.IsTrue(b != null);
            Assert.IsTrue(1 == Utils.NumObjects<ABCTest.B>());
            Assert.IsTrue(b.YoungerAge == 50);

            // Now enabling the mapping and mapping existing objects, so everything should be deleted.
            MapConfig.Enabled = true;
            DbMapping.MapExistingObjects();

            Assert.IsTrue(0 == Utils.NumObjects<DbMappingRelation>());
            Assert.IsTrue(0 == Utils.NumObjects<ABCTest.A>());
            Assert.IsTrue(0 == Utils.NumObjects<ABCTest.B>());
            Assert.IsTrue(0 == Utils.NumObjects<ABCTest.C>());
        }

        public static void RunTest() {

            DbMapping.Init();

            // Creating mapping handlers.
            Map();

            // Checking that there are no existing relations.
            Assert.IsTrue(0 == Utils.NumObjects<DbMappingRelation>());

            TestCreateA();
            TestCreateAAndMapExistingObjects();

            TestCreateB();
            TestCreateBAndMapExistingObjects();

            TestCreateC();
            TestCreateCAndMapExistingObjects();

            TestCreateADeleteTwoAndMapExistingObjects();

            Assert.IsTrue(0 == Utils.NumObjects<DbMappingRelation>());
        }
    }
}
