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
        /// Adding database mapping.
        /// </summary>
        public static void DbMap(String httpMethod, String fromUri, String toUri, Func<UInt64, UInt64, UInt64> converter) {

            lock (registrationLock_) {

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

                String methodSpaceProcessedFromUriSpace = httpMethod + " " + fromUri.Replace(Handle.UriParameterIndicator, "@l") + " ";

                HandlerOptions ho = new HandlerOptions() { HandlerLevel = HandlerOptions.HandlerLevels.ApplicationExtraLevel };

                switch (httpMethod) {

                    case Handle.POST_METHOD:
                    case Handle.PUT_METHOD:
                    case Handle.DELETE_METHOD: {

                        String converterUri = httpMethod + " " + fromUri + toUri;

                        if (Handle.IsHandlerRegistered(converterUri + " ", ho)) {
                            throw new ArgumentOutOfRangeException("Converter URI handler is already registered: " + converterUri);
                        }

                        Handle.CUSTOM(converterUri, (UInt64 fromOid, UInt64 toOid) => {
                            UInt64 res = converter(fromOid, toOid);
                            return res.ToString();
                        }, ho);

                        if (!Handle.IsHandlerRegistered(methodSpaceProcessedFromUriSpace, ho)) {

                            Handle.CUSTOM(httpMethod + " " + fromUri, (UInt64 fromOid) => {

                                Boolean isRootHierarchy = false;

                                if (null == touchedClasses_) {

                                    isRootHierarchy = true;
                                    touchedClasses_ = new Dictionary<String, Boolean>();

                                    // Touching myself here.
                                    if (httpMethod == Handle.POST_METHOD) {
                                        touchedClasses_.Add(fromClassFullName, true);
                                    }
                                }

                                try {

                                    // Checking if we create objects.
                                    switch (httpMethod) {

                                        // Creating a new object.
                                        case Handle.POST_METHOD: {

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

                                            break;
                                        }

                                        // Modifying object.
                                        case Handle.PUT_METHOD: {

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