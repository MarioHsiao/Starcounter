using System;
using Starcounter;
using Starcounter.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Starcounter.Internal;

namespace SharedClasses {
    [Database]
    public class MySomebody {
        public String Name;
    }
}

namespace App2 {

    [Database]
    public class App2Person {
        public String Name;
        public Int32 Age;
    }
    public class TestApp2 {

        public static void Map() {

            StarcounterEnvironment.RunWithinApplication("App2", () => {

                DbMapping.MapCreation("App2.App2Person", "SharedClasses.MySomebody", (UInt64 fromOid) => {
                    SharedClasses.MySomebody dst = new SharedClasses.MySomebody();
                    return dst.GetObjectNo(); // Newly created object ID.
                });

                DbMapping.MapCreation("SharedClasses.MySomebody", "App2.App2Person", (UInt64 fromOid) => {
                    App2.App2Person dst = new App2.App2Person();
                    return dst.GetObjectNo(); // Newly created object ID.
                });

                DbMapping.MapDeletion("App2.App2Person", "SharedClasses.MySomebody", (UInt64 fromOid, UInt64 toOid) => {
                    SharedClasses.MySomebody dst = (SharedClasses.MySomebody)DbHelper.FromID(toOid);
                    if (dst != null)
                        dst.Delete();
                });

                DbMapping.MapDeletion("SharedClasses.MySomebody", "App2.App2Person", (UInt64 fromOid, UInt64 toOid) => {
                    App2.App2Person dst = (App2.App2Person)DbHelper.FromID(toOid);
                    if (dst != null)
                        dst.Delete();
                });

                DbMapping.MapModification("App2.App2Person", "SharedClasses.MySomebody", (UInt64 fromOid, UInt64 toOid) => {
                    App2.App2Person src = (App2.App2Person)DbHelper.FromID(fromOid);
                    SharedClasses.MySomebody dst = (SharedClasses.MySomebody)DbHelper.FromID(toOid);
                    dst.Name = src.Name;
                });

                DbMapping.MapModification("SharedClasses.MySomebody", "App2.App2Person", (UInt64 fromOid, UInt64 toOid) => {
                    SharedClasses.MySomebody src = (SharedClasses.MySomebody)DbHelper.FromID(fromOid);
                    App2.App2Person dst = (App2.App2Person)DbHelper.FromID(toOid);
                    dst.Name = src.Name;
                });
            });
        }

        public static void Init() {

            Db.Transact(() => {
                Db.SlowSQL("DELETE FROM App2Person");
            });
        }

        public static void RunTest() {

            StarcounterEnvironment.RunWithinApplication("App2", () => {

                Db.Transact(() => {
                    App2Person app2Person = new App2Person();
                    Assert.IsTrue(null != app2Person);

                    app2Person.Name = "Hoho";
                    app2Person.Age = 123;
                });
            });
        }
    }
}
