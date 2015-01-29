
using Starcounter.Advanced;
using Starcounter.Binding;
using Starcounter.Metadata;
using System;

namespace Starcounter.Advanced {

    /// <summary>
    /// Helper class to access dynamic types properties and fields
    /// for database classes defined only with the [Database] attribute,
    /// and not inheriting <see cref="Entity"/>.
    /// </summary>
    public static class EntityHelper {

        /// <summary>
        /// Retreives a type that can be used to access properties
        /// defined by Starcounter entity, such as the dynamic type
        /// fields.
        /// </summary>
        /// <param name="obj">An object whose entity properties the
        /// client want to access.</param>
        /// <returns>An instance of a class that allows basic Entity
        /// properties to be accessed.</returns>
        public static IDbTuple ToEntity(object obj) {
            var entity = obj as IDbTuple;
            if (entity != null) {
                return entity;
            }
            var e = obj as Entity;
            if (e != null) {
                return new EntityBasedRuntimeTuple(e);
            }
            var rawView = obj as RawView;
            if (rawView != null) {
                return new RawViewBasedRuntimeEntity(rawView);
            }
            var proxy = obj as IObjectProxy;
            if (proxy != null) {
                return new ProxyBasedRuntimeEntity(proxy);
            }

            // Decide how to report this, and what to allow
            // TODO:
            throw new InvalidOperationException();
        }

        public static IDbTuple GetType(object obj) {
            return ToEntity(obj).Type;
        }

        public static void SetType(object obj, object type) {
            IDbTuple e = null;
            if (type != null) {
                e = type as IDbTuple;
                if (e == null) {
                    e = ToEntity(type);
                }
            }

            ToEntity(obj).Type = e;
        }

        public static string GetName(object obj) {
            return ToEntity(obj).Name;
        }

        public static void SetName(object obj, string name) {
            ToEntity(obj).Name = name;
        }

        public static IDbTuple GetInherits(object obj) {
            return ToEntity(obj).Inherits;
        }

        public static void SetInherits(object obj, object type) {
            IDbTuple e = null;
            if (type != null) {
                e = type as IDbTuple;
                if (e == null) {
                    e = ToEntity(type);
                }
            }

            ToEntity(obj).Inherits = e;
        }

        public static bool IsType(object obj) {
            return ToEntity(obj).IsType;
        }

        public static void SetIsType(object obj, bool value) {
            ToEntity(obj).IsType = value;
        }
    }
}
