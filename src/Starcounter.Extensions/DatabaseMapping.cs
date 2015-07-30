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
using PolyjuiceNamespace;

namespace Starcounter.Extensions {

    [Database]
    public class DbMapInfo {
        public String ToClassFullName;
        public String MethodSpaceProcessedFromUriSpace;
    }

    [Database]
    public class DbMappingRelation {
        public UInt64 FromOid;
        public UInt64 ToOid;
        public DbMapInfo MapInfo;
    }

    public class DbMapping {

        /// <summary>
        /// Used for registration exclusive access.
        /// </summary>
        const String registrationLock_ = "LockMe";

        /// <summary>
        /// Touched classes in call hierarchy.
        /// </summary>
        [ThreadStatic]
        static Dictionary<String, Boolean> touchedClasses_;

        /// <summary>
        /// Initializes the database mapping.
        /// </summary>
        public static void Init() {

            // Creating a unique index on DbMappingRelation.FromOid.
            if (Db.SQL("SELECT i FROM MaterializedIndex i WHERE Name = ?", "DbMappingRelationFromOidIndex").First == null) {
                Db.SQL("CREATE INDEX DbMappingRelationFromOidIndex ON DbMappingRelation (FromOid ASC)");
            }
        }

        /// <summary>
        /// Returns true if there is a mapped object to a given one.
        /// </summary>
        public static Boolean HasMappedObjects(UInt64 fromOid) {
            UInt64 mappedOid = 0;

            Db.Transact(() => {

                DbMappingRelation rel = Db.SQL<DbMappingRelation>("SELECT o FROM DbMappingRelation o WHERE o.FromOid = ?", fromOid).First;

                // Checking if there is no relation found for this object.
                if (null == rel) {
                    return;
                }

                mappedOid = rel.ToOid;
            });

            return 0 != mappedOid;
        }

        /// <summary>
        /// Getting mapped object ids, if they exists. Otherwise an empty list.
        /// </summary>
        public static List<UInt64> GetMappedOids(UInt64 fromOid) {

            List<UInt64> mappedOids = new List<UInt64>();

            Db.Transact(() => {

                // Going through all connected objects.
                foreach (DbMappingRelation rel in Db.SQL("SELECT o FROM DbMappingRelation o WHERE o.FromOid = ?", fromOid)) {
                    mappedOids.Add(rel.ToOid);
                }
            });

            return mappedOids;
        }

        /// <summary>
        /// Remaps the existing mapping relation.
        /// </summary>
        public static void Remap(UInt64 fromOid, UInt64 newMappedOid) {

            Db.Transact(() => {

                DbMappingRelation rel = Db.SQL<DbMappingRelation>("SELECT o FROM DbMappingRelation o WHERE o.FromOid = ?", fromOid).First;

                if (null == rel) {
                    throw new ArgumentOutOfRangeException("Specified object has no mapping relation found: " + fromOid);
                }
            
                // Mapping to a new given object.
                rel.ToOid = newMappedOid;
            });
        }
        
        /// <summary>
        /// Maps creation of a new object.
        /// </summary>
        public static void MapCreation(String fromUri, String toUri, Func<UInt64, UInt64> converter) {
            Map("POST", fromUri, toUri, (UInt64 createdOid, UInt64 unusedOid) => { 
                return converter(createdOid); 
            });
        }

        /// <summary>
        /// Maps deletion of an object.
        /// </summary>
        public static void MapDeletion(String fromUri, String toUri, Action<UInt64, UInt64> converter) {
            Map("DELETE", fromUri, toUri, (UInt64 fromOid, UInt64 toOid) => {
                converter(fromOid, toOid);
                return 0;            
            });
        }

        /// <summary>
        /// Maps modification of an object.
        /// </summary>
        public static void MapModification(String fromUri, String toUri, Action<UInt64, UInt64> converter) {
            Map("PUT", fromUri, toUri, (UInt64 fromOid, UInt64 toOid) => {
                converter(fromOid, toOid);
                return 0;
            });
        }
        
        /// <summary>
        /// Map database classes for replication.
        /// </summary>
        internal static void Map(String httpMethod, String fromUri, String toUri, Func<UInt64, UInt64, UInt64> converter) {

            lock (registrationLock_) {

                if (!fromUri.EndsWith("/{?}")) {
                    throw new ArgumentOutOfRangeException("Handler from URI should end with parameter, e.g. /MyClassName/{?}");
                }

                if (!toUri.EndsWith("/{?}")) {
                    throw new ArgumentOutOfRangeException("Handler to URI should end with parameter, e.g. /MyClassName/{?}");
                }

                String processedFromUri = fromUri.Replace(Handle.UriParameterIndicator, "@l"),
                    processedToUri = toUri.Replace(Handle.UriParameterIndicator, "@l");

                String toClassFullName = toUri.Substring(1).Substring(0, toUri.Length - 5),
                    fromClassFullName = fromUri.Substring(1).Substring(0, fromUri.Length - 5);

                if (null == Db.SQL<Starcounter.Metadata.Table>("select t from starcounter.metadata.table t where fullname = ?", fromClassFullName).First) {
                    throw new ArgumentOutOfRangeException("From class name with given full name does not exist: " + fromClassFullName);
                }

                if (null == Db.SQL<Starcounter.Metadata.Table>("select t from starcounter.metadata.table t where fullname = ?", toClassFullName).First) {
                    throw new ArgumentOutOfRangeException("To class name with given full name does not exist: " + toClassFullName);
                }

                String methodSpaceProcessedFromUriSpace = httpMethod + " " + processedFromUri + " ";

                HandlerOptions ho = new HandlerOptions() { HandlerLevel = HandlerOptions.HandlerLevels.ApplicationExtraLevel };

                String converterUri = fromUri + toUri;

                if (Handle.IsHandlerRegistered(httpMethod + " " + processedFromUri + processedToUri + " ", ho)) {
                    throw new ArgumentOutOfRangeException("Converter URI handler is already registered: " + httpMethod + " " + converterUri);
                }

                switch (httpMethod) {

                    case Handle.POST_METHOD: {

                        // Adding converter handler.
                        Handle.POST(converterUri, (UInt64 fromOid, UInt64 unused) => {
                            UInt64 res = converter(fromOid, 0);
                            return res.ToString();
                        }, ho);

                        // Checking if the processing handler is registered.
                        if (Handle.IsHandlerRegistered(methodSpaceProcessedFromUriSpace, ho)) {
                            break;
                        }

                        // The actual processing handler that is called by database hooks.
                        Handle.POST(fromUri, (UInt64 fromOid) => {

                            Boolean isRootHierarchy = false;

                            if (null == touchedClasses_) {

                                isRootHierarchy = true;
                                touchedClasses_ = new Dictionary<String, Boolean>();

                                // Touching myself here.
                                touchedClasses_.Add(fromClassFullName, true);
                            }

                            try {

                                foreach (DbMapInfo mapInfo in Db.SQL("SELECT o FROM DbMapInfo o WHERE o.MethodSpaceProcessedFromUriSpace = ?", methodSpaceProcessedFromUriSpace)) {

                                    // Checking if we already have processed this class.
                                    if (!touchedClasses_.ContainsKey(mapInfo.ToClassFullName)) {

                                        // Adding class as touched.
                                        touchedClasses_.Add(mapInfo.ToClassFullName, true);

                                        // Calling the converter.
                                        Response resp = Self.POST("/" + fromClassFullName + "/" + fromOid.ToString() + "/" + mapInfo.ToClassFullName + "/0", null, null, null, 0, ho);

                                        // Checking if we have result.
                                        if (null == resp)
                                            continue;

                                        // Getting new created related object id.
                                        UInt64 toOid = UInt64.Parse(resp.Body);

                                        // Checking if object id is real.
                                        if ((0 != toOid) && (UInt64.MaxValue != toOid)) {

                                            Db.Transact(() => {

                                                // Creating a relation between two objects.
                                                DbMappingRelation objRel = new DbMappingRelation() {
                                                    FromOid = fromOid,
                                                    ToOid = toOid,
                                                    MapInfo = mapInfo
                                                };

                                            });
                                        }
                                    }
                                }

                            } finally {

                                if (isRootHierarchy) {
                                    touchedClasses_ = null;
                                }
                            }

                            return 200;

                        }, ho);

                        break;
                    }

                    case Handle.PUT_METHOD: {

                        // Adding converter handler.
                        Handle.PUT(converterUri, (UInt64 fromOid, UInt64 toOid) => {
                            UInt64 res = converter(fromOid, toOid);
                            return res.ToString();
                        }, ho);

                        // Checking if the processing handler is registered.
                        if (Handle.IsHandlerRegistered(methodSpaceProcessedFromUriSpace, ho)) {
                            break;
                        }

                        // The actual processing handler that is called by database hooks.
                        Handle.PUT(fromUri, (UInt64 fromOid) => {

                            Boolean isRootHierarchy = false;

                            if (null == touchedClasses_) {

                                isRootHierarchy = true;
                                touchedClasses_ = new Dictionary<String, Boolean>();
                            }

                            try {

                                // Going through all connected objects.
                                foreach (DbMappingRelation rel in Db.SQL("SELECT o FROM DbMappingRelation o WHERE o.FromOid = ?", fromOid)) {

                                    // Checking if we already have processed this class.
                                    if (!touchedClasses_.ContainsKey(rel.MapInfo.ToClassFullName)) {

                                        // Adding class as touched.
                                        touchedClasses_.Add(rel.MapInfo.ToClassFullName, true);

                                        // Calling the converter.
                                        Response resp = Self.PUT("/" + fromClassFullName + "/" + fromOid.ToString() + "/" + rel.MapInfo.ToClassFullName + "/" + rel.ToOid, null, null, null, 0, ho);

                                        // Checking if we have result.
                                        if (null == resp)
                                            continue;
                                    }
                                }

                            } finally {

                                if (isRootHierarchy) {
                                    touchedClasses_ = null;
                                }
                            }

                            return 200;

                        }, ho);

                        break;
                    }

                    case Handle.DELETE_METHOD: {

                        // Adding converter handler.
                        Handle.DELETE(converterUri, (UInt64 fromOid, UInt64 toOid) => {
                            UInt64 res = converter(fromOid, toOid);
                            return res.ToString();
                        }, ho);

                        // Checking if the processing handler is registered.
                        if (Handle.IsHandlerRegistered(methodSpaceProcessedFromUriSpace, ho)) {
                            break;
                        }

                        // The actual processing handler that is called by database hooks.
                        Handle.DELETE(fromUri, (UInt64 fromOid) => {

                            Boolean isRootHierarchy = false;

                            if (null == touchedClasses_) {

                                isRootHierarchy = true;
                                touchedClasses_ = new Dictionary<String, Boolean>();
                            }

                            try {

                                // Going through all connected objects.
                                foreach (DbMappingRelation rel in Db.SQL("SELECT o FROM DbMappingRelation o WHERE o.FromOid = ?", fromOid)) {

                                    // Checking if we already have processed this class.
                                    if (!touchedClasses_.ContainsKey(rel.MapInfo.ToClassFullName)) {

                                        // Adding class as touched.
                                        touchedClasses_.Add(rel.MapInfo.ToClassFullName, true);

                                        // Calling the converter.
                                        Response resp = Self.DELETE("/" + fromClassFullName + "/" + fromOid.ToString() + "/" + rel.MapInfo.ToClassFullName + "/" + rel.ToOid, null, null, null, 0, ho);

                                        // Checking if we have result.
                                        if (null == resp)
                                            continue;

                                        // Deleting the relation, since we are about to delete objects.
                                        rel.Delete();
                                    }
                                }

                            } finally {

                                if (isRootHierarchy) {
                                    touchedClasses_ = null;
                                }
                            }

                            return 200;

                        }, ho);
                    
                        break;
                    }

                    default: {
                        throw new ArgumentOutOfRangeException("Unsupported HTTP method supplied: " + httpMethod);
                    }
                }

                // Checking if we already have a map.
                if (null == Db.SQL("SELECT o FROM DbMapInfo o WHERE o.MethodSpaceProcessedFromUriSpace = ? AND o.ToClassFullName = ?", methodSpaceProcessedFromUriSpace, toClassFullName).First) {

                    Db.Transact(() => {

                        DbMapInfo dbi = new DbMapInfo() {
                            ToClassFullName = toClassFullName,
                            MethodSpaceProcessedFromUriSpace = methodSpaceProcessedFromUriSpace
                        };
                    });
                }
            }
        }
    }
}