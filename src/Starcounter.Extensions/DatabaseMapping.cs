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

namespace Starcounter.Extensions {

    [Database]
    public class DbMapInfo {
        public String FromClassFullName;
        public String ToClassFullName;
    }

    [Database]
    public class DbMappingRelation {
        public UInt64 FromOid;
        public UInt64 ToOid;
        public String ToClassFullName;
        public DbMappingRelation MirrorRelationRef;
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

            // Enabling mapping globally.
            MapConfig.IsGlobalMappingEnabled = true;
            MapConfig.Enabled = true;

            if (Db.SQL("SELECT i FROM Starcounter.Internal.Metadata.MaterializedIndex i WHERE Name = ?", "DbMappingRelationFromOidIndex").First == null) {
                Db.SQL("CREATE INDEX DbMappingRelationFromOidIndex ON Starcounter.Extensions.DbMappingRelation (FromOid ASC)");
            }

            if (Db.SQL("SELECT i FROM Starcounter.Internal.Metadata.MaterializedIndex i WHERE Name = ?", "DbMappingRelationFromOidAndNameIndex").First == null) {
                Db.SQL("CREATE INDEX DbMappingRelationFromOidAndNameIndex ON Starcounter.Extensions.DbMappingRelation (FromOid ASC, ToClassFullName ASC)");
            }

            if (Db.SQL("SELECT i FROM Starcounter.Internal.Metadata.MaterializedIndex i WHERE Name = ?", "DbMapInfoFromClassFullNameIndex").First == null) {
                Db.SQL("CREATE INDEX DbMapInfoFromClassFullNameIndex ON Starcounter.Extensions.DbMapInfo (FromClassFullName ASC)");
            }

            // Adding handler for mapping existing objects.
            if (!Handle.IsHandlerRegistered("GET /sc/map ", null)) {
                Handle.GET("/sc/map", () => {
                    MapExistingObjects();
                    return 200;
                });
            }
        }

        /// <summary>
        /// Returns true if there is a mapped object to a given one.
        /// </summary>
        public static Boolean HasMappedObjects(UInt64 fromOid) {
            UInt64 mappedOid = 0;

            Db.Transact(() => {

                DbMappingRelation rel = Db.SQL<DbMappingRelation>("SELECT o FROM Starcounter.Extensions.DbMappingRelation o WHERE o.FromOid = ?", fromOid).First;

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
                foreach (DbMappingRelation rel in Db.SQL("SELECT o FROM Starcounter.Extensions.DbMappingRelation o WHERE o.FromOid = ?", fromOid)) {
                    mappedOids.Add(rel.ToOid);
                }
            });

            return mappedOids;
        }

        /// <summary>
        /// Getting one specific mapped object. Returns null of no mapped objects exists and
        /// throws exception in case of the source object is mapped to several destination objects.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="from"></param>
        /// <returns></returns>
        public static T GetMappedObject<T>(object from) {
            if (from == null)
                return default(T);

            ulong fromOid = from.GetObjectNo();
            ulong toOid = 0;

            Db.Transact(() => {
                foreach (DbMappingRelation rel in Db.SQL("SELECT o FROM Starcounter.Extensions.DbMappingRelation o WHERE o.FromOid=? AND o.ToClassFullName=?", fromOid, typeof(T).FullName)) {
                    // TODO:
                    // Maybe we can never have more than 0 or 1 relations for a specific type? In that case this check is unnecessary
                    if (toOid > 0) { // toOid already set. We only support 0 or 1 objects from this method.
                        // TODO:
                        // Throw proper error with errorcode.
                        throw new Exception("Invalid mapping. Should be mapped to one object but found several maps.");
                    }
                    toOid = rel.ToOid;
                }
            });

            if (toOid == 0)
                return default(T);

            return (T)DbHelper.FromID(toOid);
        }

        /// <summary>
        /// Remaps the existing mapping relation.
        /// </summary>
        public static void Remap(UInt64 fromOid, UInt64 toOid, UInt64 newToOid) {

            Db.Transact(() => {

                DbMappingRelation rel = Db.SQL<DbMappingRelation>("SELECT o FROM Starcounter.Extensions.DbMappingRelation o WHERE o.FromOid = ? AND o.ToOid = ?", fromOid, toOid).First;

                if (null == rel) {
                    throw new ArgumentOutOfRangeException("Specified object has no mapping relation found: " + fromOid + " -> " + toOid);
                }
                
                // Mapping to a new given object.
                rel.ToOid = newToOid;
            });
        }

        /// <summary>
        /// Gets the full name of the given template class.
        /// </summary>
        private static String GetClassFullName<T>() {
            return typeof(T).FullName;
        }

        /// <summary>
        /// Maps creation of a new object.
        /// </summary>
        public static void MapCreation(String fromClassFullName, String toClassFullName, Func<UInt64, UInt64> converter) {
            Map("POST", fromClassFullName, toClassFullName, (UInt64 createdOid, UInt64 unusedOid) => { 
                return converter(createdOid); 
            });
        }

        /// <summary>
        /// Maps creation of a new object using the fullname of each type as uris.
        /// </summary>
        /// <typeparam name="TFrom"></typeparam>
        /// <typeparam name="TTo"></typeparam>
        public static void MapCreation<TFrom, TTo>(Func<UInt64, UInt64> converter) {
            var fromClassFullName = GetClassFullName<TFrom>();
            var toClassFullName = GetClassFullName<TTo>();

            DbMapping.MapCreation(fromClassFullName, toClassFullName, converter);
        }

        /// <summary>
        /// Maps creation of a new object using the fullname of each type as uris and 
        /// creates a new instance of the type specified in <typeparamref name="TTo"/> 
        /// using the default constructor.
        /// </summary>
        /// <typeparam name="TFrom"></typeparam>
        /// <typeparam name="TTo"></typeparam>
        public static void MapCreation<TFrom, TTo>() where TTo : new() {
            var fromClassFullName = GetClassFullName<TFrom>();
            var toClassFullName = GetClassFullName<TTo>();

            DbMapping.MapCreation(fromClassFullName, toClassFullName, (UInt64 fromOid) => {
                TTo newObj = new TTo();
                return newObj.GetObjectNo();
            });
        }

        /// <summary>
        /// Maps deletion of an object.
        /// </summary>
        public static void MapDeletion(String fromClassFullName, String toClassFullName, Action<UInt64, UInt64> converter) {
            Map("DELETE", fromClassFullName, toClassFullName, (UInt64 fromOid, UInt64 toOid) => {
                converter(fromOid, toOid);
                return 0;            
            });
        }

        /// <summary>
        /// Maps deletion of an object using the fullname of each type as uris.
        /// </summary>
        /// <typeparam name="TFrom"></typeparam>
        /// <typeparam name="TTo"></typeparam>
        public static void MapDeletion<TFrom, TTo>(Action<UInt64, UInt64> converter) {
            var fromClassFullName = GetClassFullName<TFrom>();
            var toClassFullName = GetClassFullName<TTo>();

            DbMapping.MapDeletion(fromClassFullName, toClassFullName, converter);
        }

        /// <summary>
        /// Maps deletion of an object using the fullname of each type as uris and calls 
        /// Delete method on the object from <typeparamref name="TTo" />
        /// </summary>
        /// <typeparam name="TFrom"></typeparam>
        /// <typeparam name="TTo"></typeparam>
        public static void MapDeletion<TFrom, TTo>() {
            var fromClassFullName = GetClassFullName<TFrom>();
            var toClassFullName = GetClassFullName<TTo>();

            DbMapping.MapDeletion(fromClassFullName, toClassFullName, (UInt64 fromOid, UInt64 toOid) => {
                TTo obj = (TTo)DbHelper.FromID(toOid);
                if (obj != null)
                    obj.Delete();
            });
        }

        /// <summary>
        /// Maps modification of an object.
        /// </summary>
        public static void MapModification(String fromClassFullName, String toClassFullName, Action<UInt64, UInt64> converter) {
            Map("PUT", fromClassFullName, toClassFullName, (UInt64 fromOid, UInt64 toOid) => {
                converter(fromOid, toOid);
                return 0;
            });
        }

        /// <summary>
        /// Maps modification of an object using the fullname of each type as uris.
        /// </summary>
        /// <typeparam name="TFrom"></typeparam>
        /// <typeparam name="TTo"></typeparam>
        /// <param name="converter"></param>
        public static void MapModification<TFrom, TTo>(Action<UInt64, UInt64> converter) {
            var fromClassFullName = GetClassFullName<TFrom>();
            var toClassFullName = GetClassFullName<TTo>();

            DbMapping.MapModification(fromClassFullName, toClassFullName, converter);
        }

        /// <summary>
        /// Maps modification of an object using the fullname of each type as uris and maps all 
        /// properties (using database metadata) that match.
        /// </summary>
        /// <typeparam name="TFrom"></typeparam>
        /// <typeparam name="TTo"></typeparam>
        /// <param name="converter"></param>
        public static void MapModification<TFrom, TTo>() {
            Action<ulong, ulong> modificationConverter;

            modificationConverter = ModificationMapGenerator.Create<TFrom, TTo>();
            DbMapping.MapModification<TFrom, TTo>(modificationConverter);
        }

        /// <summary>
        /// Adds default mappings for creation, deletion and modification. For modifications
        /// all properties that have the same name and type in both specified generic types are
        /// mapped, including single object relations.
        /// </summary>
        /// <typeparam name="TFrom"></typeparam>
        /// <typeparam name="TTo"></typeparam>
        /// <param name="twoWay">If true default maps in both directions are created.</param>
        public static void MapDefault<TFrom, TTo>(bool twoWay = true)
            where TFrom : new()
            where TTo : new() {
            Action<ulong, ulong> modificationConverterFrom;
            Action<ulong, ulong> modificationConverterTo;
            
            // We start with generating the delegates for the modifications since they might fail
            // if we have some unsupported values.
            modificationConverterTo = ModificationMapGenerator.Create<TFrom, TTo>();
            modificationConverterFrom = null;

            if (twoWay) {
                modificationConverterFrom = ModificationMapGenerator.Create<TTo, TFrom>();
            }

            DbMapping.MapCreation<TFrom, TTo>();
            DbMapping.MapDeletion<TFrom, TTo>();
            DbMapping.MapModification<TFrom, TTo>(modificationConverterTo);

            if (twoWay) {
                DbMapping.MapCreation<TTo, TFrom>();
                DbMapping.MapDeletion<TTo, TFrom>();
                DbMapping.MapModification<TTo, TFrom>(modificationConverterFrom);
            }
        }

        /// <summary>
        /// States when checking mapping object.
        /// </summary>
        public enum ObjectCheckState {
            OnlySourceObjectExists,
            MappedObjectClassIsNotLoaded,
            OnlyObjectAndRelationExisted,
            ObjectRelationAndMappedObjectExists
        };

        /// <summary>
        /// Checks if there is a mapping relation and mapped object to the given object.
        /// </summary>
        internal static ObjectCheckState CheckMappedObjectExistence(String fromClassFullName, UInt64 fromOid, String toClassFullName, Boolean deleteOrphaned) {

            // Getting the relation for the source object.
            DbMappingRelation rel = Db.SQL<DbMappingRelation>("SELECT o FROM Starcounter.Extensions.DbMappingRelation o WHERE o.FromOid = ? AND o.ToClassFullName = ?",
                fromOid, toClassFullName).First;

            // First checking if there is a relation object.
            if (rel == null) {
                return ObjectCheckState.OnlySourceObjectExists;
            }
            
            // Checking if class exists.
            try {

                // Now checking if mapped object actually exists.
                if (null != DbHelper.FromID(rel.ToOid)) {
                    return ObjectCheckState.ObjectRelationAndMappedObjectExists;
                }

            } catch {
                // When this exception occurs it means that database object is in database, but the code didn't load the .NET class.
                return ObjectCheckState.MappedObjectClassIsNotLoaded;
            }

            // Now we need to delete the orphaned relation that has not mapped object and the source object.
            if (deleteOrphaned) {

                // Deleting the source object.
                var fromObj = DbHelper.FromID(fromOid);
                fromObj.Delete();
            }

            return ObjectCheckState.OnlyObjectAndRelationExisted;
        }

        /// <summary>
        /// Mapping existing objects.
        /// </summary>
        public static void MapExisting() {
            MapExistingObjects();
        }

        /// <summary>
        /// Mapping existing objects.
        /// </summary>
        public static void MapExistingObjects() {

            StarcounterEnvironment.RunWithinApplication(null, () => {

                Db.Transact(() => {

                    // Going through registered mappers.
                    foreach (DbMapInfo mi in Db.SQL("SELECT o FROM Starcounter.Extensions.DbMapInfo o")) {

                        // Checking if class exists.
                        try {
                            var v = Db.SQL("SELECT o FROM " + mi.FromClassFullName + " o").First;
                        } catch {
                            continue;
                            // Class does not exist in database so we proceed to next one.
                        }

                        // Selecting all existing objects of class FROM.
                        foreach (var o in Db.SQL("SELECT o FROM " + mi.FromClassFullName + " o")) {

                            UInt64 objNo = o.GetObjectNo();

                            // Checking if we already having a mapped object.
                            ObjectCheckState state = CheckMappedObjectExistence(mi.FromClassFullName, objNo, mi.ToClassFullName, true);
                            switch (state) {

                                case ObjectCheckState.OnlySourceObjectExists: {

                                    // Running mapped objects creation.
                                    Self.POST(string.Format("/{0}/{1}", mi.FromClassFullName, objNo), null, null, null, 0, new HandlerOptions() {
                                        HandlerLevel = HandlerOptions.HandlerLevels.ApplicationExtraLevel
                                    });

                                    // Now updating the objects.
                                    Self.PUT(string.Format("/{0}/{1}", mi.FromClassFullName, objNo), null, null, null, 0, new HandlerOptions() {
                                        HandlerLevel = HandlerOptions.HandlerLevels.ApplicationExtraLevel
                                    });

                                    break;
                                }

                                case ObjectCheckState.OnlyObjectAndRelationExisted: {

                                    break;
                                }

                                case ObjectCheckState.ObjectRelationAndMappedObjectExists: {

                                    // Just updating the object now.
                                    Self.PUT(string.Format("/{0}/{1}", mi.FromClassFullName, objNo), null, null, null, 0, new HandlerOptions() {
                                        HandlerLevel = HandlerOptions.HandlerLevels.ApplicationExtraLevel
                                    });

                                    break;
                                }

                                case ObjectCheckState.MappedObjectClassIsNotLoaded: {

                                    break;
                                }
                            }
                        }
                    }
                });

                Db.Transact(() => {

                    // Searching for orhaned relations and deleting them.
                    foreach (DbMappingRelation rel in Db.SQL<DbMappingRelation>("SELECT o FROM Starcounter.Extensions.DbMappingRelation o")) {

                        // Checking that there are no objects in each direction.
                        if (null == DbHelper.FromID(rel.FromOid) &&
                            null == DbHelper.FromID(rel.ToOid)) {

                            // First deleting other direction map.
                            rel.MirrorRelationRef.Delete();

                            // Deleting the relation, since we are about to delete objects.
                            rel.Delete();
                        }
                    }
                });
            });
        }

        /// <summary>
        /// Map database classes for replication.
        /// </summary>
        internal static void Map(String httpMethod, String fromClassFullName, String toClassFullName, Func<UInt64, UInt64, UInt64> converter) {

            lock (registrationLock_) {

                StarcounterEnvironment.RunWithinApplication(null, () => {

                    if (null == Db.SQL<Starcounter.Metadata.Table>("select t from starcounter.metadata.table t where fullname = ?", fromClassFullName).First) {
                        throw new ArgumentOutOfRangeException("From class name with given full name does not exist: " + fromClassFullName);
                    }

                    if (null == Db.SQL<Starcounter.Metadata.Table>("select t from starcounter.metadata.table t where fullname = ?", toClassFullName).First) {
                        throw new ArgumentOutOfRangeException("To class name with given full name does not exist: " + toClassFullName);
                    }

                    String processedFromUri = "/" + fromClassFullName + "/@l", 
                        processedToUri = "/" + toClassFullName + "/@l";

                    String fromUri = "/" + fromClassFullName + "/{?}",
                        toUri = "/" + toClassFullName + "/{?}";

                    HandlerOptions ho = new HandlerOptions() { HandlerLevel = HandlerOptions.HandlerLevels.ApplicationExtraLevel };

                    String converterUri = fromUri + toUri;

                    if (Handle.IsHandlerRegistered(httpMethod + " " + processedFromUri + processedToUri + " ", ho)) {
                        throw new ArgumentOutOfRangeException("Converter URI handler is already registered: " + httpMethod + " " + converterUri);
                    }

                    // Adding map for URI mapping (in both directions).
//                    UriMapping.MapClassesInDifferentHierarchies(fromClassFullName, toClassFullName);
//                    UriMapping.MapClassesInDifferentHierarchies(toClassFullName, fromClassFullName);

                    String methodSpaceProcessedFromUriSpace = httpMethod + " " + processedFromUri + " ";

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

                                    // Going through all mapped objects.
                                    List<DbMapInfo> mapInfos = new List<DbMapInfo>();
                                    List<UInt64> createdIds = new List<UInt64>();
                                    foreach (DbMapInfo mapInfo in Db.SQL("SELECT o FROM Starcounter.Extensions.DbMapInfo o WHERE o.FromClassFullName = ?", fromClassFullName)) {
                                        mapInfos.Add(mapInfo);
                                        createdIds.Add(fromOid);
                                    }

                                    for (Int32 i = 0; i < mapInfos.Count; i++) {

                                        DbMapInfo mapInfo = mapInfos[i];
                                        UInt64 createdId = createdIds[i];

                                        // Checking if we already have processed this class.
                                        if (!touchedClasses_.ContainsKey(mapInfo.ToClassFullName)) {

                                            // Adding class as touched.
                                            touchedClasses_.Add(mapInfo.ToClassFullName, true);

                                            // Checking if mapped object exists.
                                            ObjectCheckState state = CheckMappedObjectExistence(mapInfo.FromClassFullName, createdId, mapInfo.ToClassFullName, true);

                                            // We create a new object only if the mapped object does not exist.
                                            if (ObjectCheckState.ObjectRelationAndMappedObjectExists == state ||
                                                ObjectCheckState.MappedObjectClassIsNotLoaded == state) {

                                                continue;
                                            }
                                                
                                            Response resp = null;

                                            // Calling the converter.
                                            try {

                                                MapConfig.Enabled = false;

                                                StarcounterEnvironment.RunWithinApplication(null, () => {
                                                    // Calling the converter.
                                                    resp = Self.POST("/" + mapInfo.FromClassFullName + "/" + createdId.ToString() + "/" + mapInfo.ToClassFullName + "/0", null, null, null, 0, ho);
                                                });

                                            } finally {
                                                MapConfig.Enabled = true;
                                            }

                                            // Checking if we have result.
                                            if (null == resp)
                                                continue;

                                            // Getting new created related object id.
                                            UInt64 toOid = UInt64.Parse(resp.Body);

                                            // Adding chained mappings.
                                            foreach (DbMapInfo mi in Db.SQL("SELECT o FROM Starcounter.Extensions.DbMapInfo o WHERE o.FromClassFullName = ?", mapInfo.ToClassFullName)) {

                                                // Checking if we already have processed this class.
                                                if (!touchedClasses_.ContainsKey(mi.ToClassFullName)) {
                                                    mapInfos.Add(mi);
                                                    createdIds.Add(toOid);
                                                }
                                            }

                                            // Checking if object id is real.
                                            if ((0 != toOid) && (UInt64.MaxValue != toOid)) {

                                                Boolean curDbMappingFlag = MapConfig.Enabled;
                                                MapConfig.Enabled = false;

                                                try {

                                                    // Creating a relation between two objects.
                                                    DbMappingRelation relTo = new DbMappingRelation() {
                                                        FromOid = createdId,
                                                        ToOid = toOid,
                                                        ToClassFullName = mapInfo.ToClassFullName
                                                    };

                                                    // Creating a relation between two objects.
                                                    DbMappingRelation relFrom = new DbMappingRelation() {
                                                        FromOid = toOid,
                                                        ToOid = createdId,
                                                        ToClassFullName = mapInfo.FromClassFullName,
                                                        MirrorRelationRef = relTo
                                                    };

                                                    // Setting relation back.
                                                    relTo.MirrorRelationRef = relFrom;

                                                } finally {

                                                    MapConfig.Enabled = curDbMappingFlag;
                                                }
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

                                    // Touching myself here.
                                    touchedClasses_.Add(fromClassFullName, true);
                                }

                                try {

                                    // Going through all mapped objects.
                                    List<DbMappingRelation> rels = new List<DbMappingRelation>();
                                    foreach (DbMappingRelation rel in Db.SQL("SELECT o FROM Starcounter.Extensions.DbMappingRelation o WHERE o.FromOid = ?", fromOid)) {
                                        rels.Add(rel);
                                    }

                                    for (Int32 i = 0; i < rels.Count; i++) {

                                        DbMappingRelation rel = rels[i];

                                        // Checking if we already have processed this class.
                                        if (!touchedClasses_.ContainsKey(rel.ToClassFullName)) {

                                            // Adding class as touched.
                                            touchedClasses_.Add(rel.ToClassFullName, true);

                                            Response resp = null;

                                            // Checking that both objects exist.
                                            if (null == DbHelper.FromID(rel.FromOid) || null == DbHelper.FromID(rel.ToOid)) {
                                                continue;
                                            }

                                            // Calling the converter.
                                            try {
                                                MapConfig.Enabled = false;

                                                StarcounterEnvironment.RunWithinApplication(null, () => {
                                                    // Calling the converter.
                                                    resp = Self.PUT("/" + rel.MirrorRelationRef.ToClassFullName + "/" + rel.FromOid.ToString() + "/" + rel.ToClassFullName + "/" + rel.ToOid, null, null, null, 0, ho);
                                                });

                                            } finally {
                                                MapConfig.Enabled = true;
                                            }

                                            // Adding chained mappings.
                                            foreach (DbMappingRelation r in Db.SQL("SELECT o FROM Starcounter.Extensions.DbMappingRelation o WHERE o.FromOid = ?", rel.ToOid)) {

                                                // Checking if we already have processed this class.
                                                if (!touchedClasses_.ContainsKey(r.ToClassFullName)) {
                                                    rels.Add(r);
                                                }
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

                                // Checking if we have processed this class.
                                if (null != touchedClasses_) {
                                    if (touchedClasses_.ContainsKey(fromClassFullName)) {
                                        return 200;
                                    }
                                }

                                Boolean isRootHierarchy = false;

                                if (null == touchedClasses_) {

                                    isRootHierarchy = true;
                                    touchedClasses_ = new Dictionary<String, Boolean>();
                                }

                                // Touching myself here.
                                touchedClasses_.Add(fromClassFullName, true);

                                try {

                                    // Going through all mapped objects.
                                    foreach (DbMappingRelation rel in Db.SQL("SELECT o FROM Starcounter.Extensions.DbMappingRelation o WHERE o.FromOid = ?", fromOid)) {

                                        // Checking if we already have processed this class.
                                        if (!touchedClasses_.ContainsKey(rel.ToClassFullName)) {

                                            // Calling the deletion delegate.
                                            Response resp = null;

                                            // Checking that both objects exist.
                                            if (null != DbHelper.FromID(rel.ToOid)) {

                                                StarcounterEnvironment.RunWithinApplication(null, () => {
                                                    // Calling the converter.
                                                    resp = Self.DELETE("/" + rel.MirrorRelationRef.ToClassFullName + "/" + rel.FromOid.ToString() + "/" + rel.ToClassFullName + "/" + rel.ToOid, null, null, null, 0, ho);
                                                });

                                                // Checking if we have result.
                                                if (null == resp)
                                                    continue;
                                            }

                                            // First deleting other direction map.
                                            rel.MirrorRelationRef.Delete();

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
                    if (null == Db.SQL("SELECT o FROM Starcounter.Extensions.DbMapInfo o WHERE o.FromClassFullName = ? AND o.ToClassFullName = ?",
                        fromClassFullName, toClassFullName).First) {

                        // NOTE: Skipping triggers call when creating new mapping info.
                        Boolean curDbMappingFlag = MapConfig.Enabled;
                        MapConfig.Enabled = false;

                        try {

                            Db.Transact(() => {

                                DbMapInfo dbi = new DbMapInfo() {
                                    ToClassFullName = toClassFullName,
                                    FromClassFullName = fromClassFullName
                                };
                            });

                        } finally {

                            MapConfig.Enabled = curDbMappingFlag;
                        }
                    }
                });
            }
        }
    }
}