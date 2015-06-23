using System;
using System.IO;
using System.Text;
using System.Threading;
using Starcounter;
using System.Diagnostics;
using System.Collections.Generic;
using Starcounter.Internal;
using Starcounter.Internal.Web;
using Starcounter.Advanced;
using Codeplex.Data;
using System.Net;
using PolyjuiceNamespace;

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
    public class DbMapInfo {
        public String ToClassFullName;
        public String ProcessedFromUri;
        public Int32 ConverterId;
        public Int32 DeleteConverterId;
        public Int32 PutConverterId;
    }

    [Database]
    public class DbMappingRelation {
        public UInt64 FromOid;
        public UInt64 ToOid;
        public DbMapInfo MapInfo;
    }

    public class DbMappingTest {

        /// <summary>
        /// List of converter functions.
        /// </summary>
        static Func<UInt64, UInt64, UInt64>[] dbMapconverters_ = new Func<UInt64, UInt64, UInt64>[1024];

        /// <summary>
        /// Number of converters.
        /// </summary>
        static Int32 numDbMapconverters_;

        /// <summary>
        /// Touched classes in call hierarchy.
        /// </summary>
        [ThreadStatic]
        static Dictionary<String, Boolean> touchedClasses_;

        /// <summary>
        /// Adding database mapping.
        /// </summary>
        public static void DbMap(String httpMethod, String fromUri, String toUri, Func<UInt64, UInt64, UInt64> converter) {

            lock (dbMapconverters_) {

                if (!fromUri.EndsWith("/{?}")) {
                    throw new ArgumentOutOfRangeException("Handler from URI should end with parameter, e.g. /MyClassName/{?}");
                }

                if (!toUri.EndsWith("/{?}")) {
                    throw new ArgumentOutOfRangeException("Handler to URI should end with parameter, e.g. /MyClassName/{?}");
                }

                String toClassFullName = toUri.Substring(1).Substring(0, toUri.Length - 5),
                    fromClassFullName = fromUri.Substring(1).Substring(0, fromUri.Length - 5);

                if (null == Db.SQL<Starcounter.Metadata.Table>("select t from starcounter.metadata.table t where fullname = ?", fromClassFullName).First) {
                    throw new ArgumentOutOfRangeException("From class name with given full name does not exist: " + fromClassFullName);
                }

                if (null == Db.SQL<Starcounter.Metadata.Table>("select t from starcounter.metadata.table t where fullname = ?", toClassFullName).First) {
                    throw new ArgumentOutOfRangeException("To class name with given full name does not exist: " + toClassFullName);
                }

                String processedFromUriWithSpace = fromUri.Replace(Handle.UriParameterIndicator, "@l") + " ";
                String processedFromUri = httpMethod + " " + processedFromUriWithSpace;

                if (null != Db.SQL("SELECT o FROM DbMapInfo o WHERE o.ProcessedFromUri = ? AND o.ToClassFullName = ?", processedFromUri, toClassFullName).First) {
                    throw new ArgumentOutOfRangeException("Map for the selected URIs is already registered: handler " + processedFromUri + " and class " + toClassFullName);
                }

                Int32 deleteConverterId = -1;
                Int32 putConverterId = -1;

                if (httpMethod == Handle.POST_METHOD) {

                    DbMapInfo tmp = Db.SQL<DbMapInfo>("SELECT o FROM DbMapInfo o WHERE o.ProcessedFromUri = ? AND o.ToClassFullName = ?", "DELETE " + processedFromUriWithSpace, toClassFullName).First;

                    if (null != tmp) {
                        deleteConverterId = tmp.ConverterId;
                    }

                    tmp = Db.SQL<DbMapInfo>("SELECT o FROM DbMapInfo o WHERE o.ProcessedFromUri = ? AND o.ToClassFullName = ?", "PUT " + processedFromUriWithSpace, toClassFullName).First;

                    if (null != tmp) {
                        putConverterId = tmp.ConverterId;
                    }
                }

                HandlerOptions ho = new HandlerOptions() { HandlerLevel = HandlerOptions.HandlerLevels.ApplicationExtraLevel };

                switch (httpMethod) {

                    case Handle.POST_METHOD:
                    case Handle.PUT_METHOD:
                    case Handle.DELETE_METHOD: {

                        if (!Handle.IsHandlerRegistered(processedFromUri, ho)) {

                            Handle.CUSTOM(httpMethod + " " + fromUri, (UInt64 fromOid) => {

                                Boolean isRootHierarchy = false;

                                if (null == touchedClasses_) {

                                    isRootHierarchy = true;
                                    touchedClasses_ = new Dictionary<String, Boolean>();
                                }

                                try {

                                    // Checking if we create objects.
                                    switch (httpMethod) {

                                        // Creating a new object.
                                        case Handle.POST_METHOD: {

                                            foreach (DbMapInfo mapInfo in Db.SQL("SELECT o FROM DbMapInfo o WHERE o.ProcessedFromUri = ?", processedFromUri)) {

                                                // Checking if we already have processed this class.
                                                if (!touchedClasses_.ContainsKey(mapInfo.ToClassFullName)) {

                                                    // Adding class as touched.
                                                    touchedClasses_.Add(mapInfo.ToClassFullName, true);

                                                    // Getting new created related object id.
                                                    UInt64 toOid = dbMapconverters_[mapInfo.ConverterId](fromOid, 0);

                                                    // Checking if object id is real.
                                                    if (toOid != 0) {

                                                        // Creating a relation between two objects.
                                                        DbMappingRelation objRel = new DbMappingRelation() {
                                                            FromOid = fromOid,
                                                            ToOid = toOid,
                                                            MapInfo = mapInfo
                                                        };
                                                    }
                                                }
                                            }

                                            break;
                                        }

                                        // Deleting object.
                                        case Handle.DELETE_METHOD: {

                                            // Going through all connected objects.
                                            foreach (DbMappingRelation rel in Db.SQL("SELECT o FROM DbMappingRelation o WHERE o.FromOid = ?", fromOid)) {

                                                // Checking if DELETE converter is defined.
                                                if (rel.MapInfo.DeleteConverterId == -1)
                                                    continue;

                                                // Checking if we already have processed this class.
                                                if (!touchedClasses_.ContainsKey(rel.MapInfo.ToClassFullName)) {

                                                    // Adding class as touched.
                                                    touchedClasses_.Add(rel.MapInfo.ToClassFullName, true);

                                                    // Calling the deletion.
                                                    dbMapconverters_[rel.MapInfo.DeleteConverterId](fromOid, rel.ToOid);

                                                    // Deleting the relation, since we are about to delete objects.
                                                    rel.Delete();
                                                }
                                            }

                                            break;
                                        }

                                        // Modifying object.
                                        case Handle.PUT_METHOD: {

                                            // Going through all connected objects.
                                            foreach (DbMappingRelation rel in Db.SQL("SELECT o FROM DbMappingRelation o WHERE o.FromOid = ?", fromOid)) {

                                                // Checking if PUT converter is defined.
                                                if (rel.MapInfo.PutConverterId == -1)
                                                    continue;

                                                // Checking if we already have processed this class.
                                                if (!touchedClasses_.ContainsKey(rel.MapInfo.ToClassFullName)) {

                                                    // Adding class as touched.
                                                    touchedClasses_.Add(rel.MapInfo.ToClassFullName, true);

                                                    // Calling the deletion.
                                                    dbMapconverters_[rel.MapInfo.PutConverterId](fromOid, rel.ToOid);
                                                }
                                            }

                                            break;
                                        }
                                    }

                                } finally {

                                    if (isRootHierarchy) {
                                        touchedClasses_ = null;
                                    }
                                }

                                return 200;

                            }, ho);
                        }

                        break;
                    }

                    default: {
                        throw new ArgumentOutOfRangeException("Unsupported HTTP method supplied: " + httpMethod);
                    }
                }

                Db.Transact(() => {

                    DbMapInfo dbi = new DbMapInfo() {
                        ToClassFullName = toClassFullName,
                        ProcessedFromUri = processedFromUri,
                        ConverterId = numDbMapconverters_,
                        PutConverterId = putConverterId,
                        DeleteConverterId = deleteConverterId
                    };
                });

                dbMapconverters_[numDbMapconverters_] = converter;
                numDbMapconverters_++;
            }
        }

        public static void Main() {

            Db.Transact(() => {
                Db.SlowSQL("DELETE FROM DbMapInfo");
                Db.SlowSQL("DELETE FROM DbMappingRelation");
                Db.SlowSQL("DELETE FROM NameClass1");
                Db.SlowSQL("DELETE FROM NameClass2");
                Db.SlowSQL("DELETE FROM NameClass3");
            });

            //Debugger.Launch();

            DbMap("PUT", "/DbMappingTest.NameClass1/{?}", "/DbMappingTest.NameClass2/{?}", (UInt64 fromOid, UInt64 toOid) => {
                NameClass1 src = (NameClass1)DbHelper.FromID(fromOid);
                NameClass2 dst = (NameClass2)DbHelper.FromID(toOid);
                dst.FullName = src.FirstName + " " + src.LastName;
                return 0;
            });

            DbMap("PUT", "/DbMappingTest.NameClass1/{?}", "/DbMappingTest.NameClass3/{?}", (UInt64 fromOid, UInt64 toOid) => {
                NameClass1 src = (NameClass1)DbHelper.FromID(fromOid);
                NameClass3 dst = (NameClass3)DbHelper.FromID(toOid);
                dst.FirstName = src.FirstName;
                return 0;
            });

            DbMap("DELETE", "/DbMappingTest.NameClass1/{?}", "/DbMappingTest.NameClass2/{?}", (UInt64 fromOid, UInt64 toOid) => {
                NameClass2 dst = (NameClass2)DbHelper.FromID(toOid);
                dst.Delete();
                return 0;
            });

            DbMap("DELETE", "/DbMappingTest.NameClass1/{?}", "/DbMappingTest.NameClass3/{?}", (UInt64 fromOid, UInt64 toOid) => {
                NameClass3 dst = (NameClass3)DbHelper.FromID(toOid);
                dst.Delete();
                return 0;
            });

            DbMap("POST", "/DbMappingTest.NameClass1/{?}", "/DbMappingTest.NameClass2/{?}", (UInt64 fromOid, UInt64 toOid) => {
                NameClass1 src = (NameClass1)DbHelper.FromID(fromOid);
                NameClass2 dst = new NameClass2();
                return dst.GetObjectNo(); // Newly created object ID.
            });

            DbMap("POST", "/DbMappingTest.NameClass1/{?}", "/DbMappingTest.NameClass3/{?}", (UInt64 fromOid, UInt64 toOid) => {
                NameClass1 src = (NameClass1)DbHelper.FromID(fromOid);
                NameClass3 dst = new NameClass3();
                return dst.GetObjectNo(); // Newly created object ID.
            });
            
            Db.Transact(() => {
                NameClass1 nc1 = new NameClass1();
                NameClass2 nc2;
                NameClass3 nc3;

                Debug.Assert(Db.SQL<NameClass2>("SELECT o FROM NameClass2 o").First != null);
                Debug.Assert(Db.SQL<NameClass3>("SELECT o FROM NameClass3 o").First != null);

                nc1.FirstName = "John";

                nc2 = Db.SQL<NameClass2>("SELECT o FROM NameClass2 o").First;
                Debug.Assert(nc2.FullName == "John ");
                nc3 = Db.SQL<NameClass3>("SELECT o FROM NameClass3 o").First;
                Debug.Assert(nc3.FirstName == "John");

                nc1.LastName = "Doe";
                nc2 = Db.SQL<NameClass2>("SELECT o FROM NameClass2 o").First;
                Debug.Assert(nc2.FullName == "John Doe");
                nc3 = Db.SQL<NameClass3>("SELECT o FROM NameClass3 o").First;
                Debug.Assert(nc3.FirstName == "John");

                nc1 = new NameClass1();
                nc1.FirstName = "Ivan";

                nc2 = Db.SQL<NameClass2>("SELECT o FROM NameClass2 o WHERE o.FullName = ?", "Ivan ").First;
                Debug.Assert(nc2.FullName == "Ivan ");
                nc3 = Db.SQL<NameClass3>("SELECT o FROM NameClass3 o WHERE o.FirstName = ?", "Ivan").First;
                Debug.Assert(nc3.FirstName == "Ivan");

                // Checking that old instances are untouched.
                nc2 = Db.SQL<NameClass2>("SELECT o FROM NameClass2 o WHERE o.FullName = ?", "John Doe").First;
                Debug.Assert(nc2.FullName == "John Doe");
                nc3 = Db.SQL<NameClass3>("SELECT o FROM NameClass3 o WHERE o.FirstName = ?", "John").First;
                Debug.Assert(nc3.FirstName == "John");

                nc1.LastName = "Petrov";

                nc2 = Db.SQL<NameClass2>("SELECT o FROM NameClass2 o WHERE o.FullName = ?", "Ivan Petrov").First;
                Debug.Assert(nc2.FullName == "Ivan Petrov");
                nc3 = Db.SQL<NameClass3>("SELECT o FROM NameClass3 o WHERE o.FirstName = ?", "Ivan").First;
                Debug.Assert(nc3.FirstName == "Ivan");

                // Deleting the object, and all related objects.
                nc1.Delete();

                // Checking that old instances are untouched.
                nc2 = Db.SQL<NameClass2>("SELECT o FROM NameClass2 o WHERE o.FullName = ?", "John Doe").First;
                Debug.Assert(nc2.FullName == "John Doe");
                nc3 = Db.SQL<NameClass3>("SELECT o FROM NameClass3 o WHERE o.FirstName = ?", "John").First;
                Debug.Assert(nc3.FirstName == "John");

                // Checking that the original and all mapped objects are deleted.
                nc1 = Db.SQL<NameClass1>("SELECT o FROM NameClass1 o WHERE o.FirstName = ?", "Ivan").First;
                Debug.Assert(nc1 == null);
                nc2 = Db.SQL<NameClass2>("SELECT o FROM NameClass2 o WHERE o.FullName = ?", "Ivan Petrov").First;
                Debug.Assert(nc2 == null);
                nc3 = Db.SQL<NameClass3>("SELECT o FROM NameClass3 o WHERE o.FirstName = ?", "Ivan").First;
                Debug.Assert(nc3 == null);

                nc1 = Db.SQL<NameClass1>("SELECT o FROM NameClass1 o WHERE o.FirstName = ?", "John").First;
                Debug.Assert(nc1.FirstName == "John");

                // Deleting the object, and all related objects.
                nc1.Delete();

                // Checking that the original and all mapped objects are deleted.
                nc1 = Db.SQL<NameClass1>("SELECT o FROM NameClass1 o WHERE o.FirstName = ?", "John").First;
                Debug.Assert(nc1 == null);
                nc2 = Db.SQL<NameClass2>("SELECT o FROM NameClass2 o WHERE o.FullName = ?", "John Doe").First;
                Debug.Assert(nc2 == null);
                nc3 = Db.SQL<NameClass3>("SELECT o FROM NameClass3 o WHERE o.FirstName = ?", "John").First;
                Debug.Assert(nc3 == null);

                // Checking that no objects are left.
                nc1 = Db.SQL<NameClass1>("SELECT o FROM NameClass1 o").First;
                Debug.Assert(nc1 == null);
                nc2 = Db.SQL<NameClass2>("SELECT o FROM NameClass2 o").First;
                Debug.Assert(nc2 == null);
                nc3 = Db.SQL<NameClass3>("SELECT o FROM NameClass3 o").First;
                Debug.Assert(nc3 == null);

                // Checking that all relations are also deleted.
                DbMappingRelation rel = Db.SQL<DbMappingRelation>("SELECT o FROM DbMappingRelation o").First;
                Debug.Assert(rel == null);
            });
        }
    }
}
