using System;
using Starcounter;
using Starcounter.Extensions;
using Starcounter.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace App1 {

    [Database]
    public class App1Person {
        public String FirstName;
        public String LastName;
        public Int32 Age;
    }

    public class TestApp1 {

        public static void Map() {

            StarcounterEnvironment.RunWithinApplication("App1", () => {

                DbMapping.MapCreation("App1.App1Person", "SharedClasses.MySomebody", (UInt64 fromOid) => {
                    SharedClasses.MySomebody dst = new SharedClasses.MySomebody();
                    return dst.GetObjectNo(); // Newly created object ID.
                });

                DbMapping.MapCreation("SharedClasses.MySomebody", "App1.App1Person", (UInt64 fromOid) => {
                    App1.App1Person dst = new App1.App1Person();
                    return dst.GetObjectNo(); // Newly created object ID.
                });

                DbMapping.MapDeletion("App1.App1Person", "SharedClasses.MySomebody", (UInt64 fromOid, UInt64 toOid) => {
                    SharedClasses.MySomebody dst = (SharedClasses.MySomebody)DbHelper.FromID(toOid);
                    if (dst != null)
                        dst.Delete();
                });

                DbMapping.MapDeletion("SharedClasses.MySomebody", "App1.App1Person", (UInt64 fromOid, UInt64 toOid) => {
                    App1.App1Person dst = (App1.App1Person)DbHelper.FromID(toOid);
                    if (dst != null)
                        dst.Delete();
                });

                DbMapping.MapModification("App1.App1Person", "SharedClasses.MySomebody", (UInt64 fromOid, UInt64 toOid) => {
                    App1.App1Person src = (App1.App1Person)DbHelper.FromID(fromOid);
                    SharedClasses.MySomebody dst = (SharedClasses.MySomebody)DbHelper.FromID(toOid);
                    dst.Name = src.FirstName + " " + src.LastName;
                });

                DbMapping.MapModification("SharedClasses.MySomebody", "App1.App1Person", (UInt64 fromOid, UInt64 toOid) => {
                    SharedClasses.MySomebody src = (SharedClasses.MySomebody)DbHelper.FromID(fromOid);
                    App1.App1Person dst = (App1.App1Person)DbHelper.FromID(toOid);

                    if (!String.IsNullOrEmpty(src.Name)) {
                        String[] str = src.Name.Split();
                        dst.FirstName = str[0];
                        if (str.Length > 1) {
                            dst.LastName = str[1];
                        }
                    }
                });

            });
        }

        public static void Init() {

            Db.Transact(() => {
                Db.SlowSQL("DELETE FROM App1Person");
            });
        }

        public static void RunTest() {

            StarcounterEnvironment.RunWithinApplication("App1", () => {

                Db.Transact(() => {
                    App1Person app1Person = new App1Person();
                    Assert.IsTrue(null != app1Person);

                    app1Person.FirstName = "John";
                    app1Person.LastName = "Doe";
                    app1Person.Age = 555;
                });
            });
        }
    }
}
