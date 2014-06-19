
using System;

namespace staradmin {
    
    /// <summary>
    /// Defines well-known object types, supported by
    /// staradmin for various operations.
    /// </summary>
    internal enum ObjectType {
        Unknown,
        Database,
        Application,
        ServerLog,
        CodeHost
    }

    internal static class ObjectTypeExtensions {
        internal static ObjectType ToObjectType(this string value) {
            switch (value.ToLowerInvariant()) {
                case "app":
                case "apps":
                case "application":
                case "applications":
                    return ObjectType.Application;
                case "db":
                case "dbs":
                case "database":
                case "databases":
                    return ObjectType.Database;
                case "log":
                case "logs":
                case "serverlog":
                    return ObjectType.ServerLog;
                case "host":
                case "codehost":
                    return ObjectType.CodeHost;
                default:
                    return ObjectType.Unknown;
            }
        }

        internal static string ToString(this ObjectType type) {
            return Enum.GetName(typeof(ObjectType), type);
        }
    }
}