
using Starcounter.Advanced;
using Starcounter.Binding;
using Starcounter.Metadata;
using System;

namespace Starcounter.Advanced {

    /// <summary>
    /// Helper class to access common Starcounter tuple data
    /// for any database class, no matter the way it is declared.
    /// </summary>
    public static class TupleHelper {

        /// <summary>
        /// Retreives a type that can be used to access properties
        /// defined by Starcounter tuples, such as the dynamic type
        /// fields.
        /// </summary>
        /// <param name="obj">An object whose tuple properties the
        /// client want to access.</param>
        /// <returns>An instance of a class that allows basic tuple
        /// properties to be accessed.</returns>
        public static IDbTuple ToTuple(object obj) {
            var tuple = obj as IDbTuple;
            if (tuple != null) {
                return tuple;
            }
            var e = obj as Entity;
            if (e != null) {
                return new EntityBasedRuntimeTuple(e);
            }
            var rawView = obj as RawView;
            if (rawView != null) {
                return new RawViewBasedRuntimeTuple(rawView);
            }
            var proxy = obj as IObjectProxy;
            if (proxy != null) {
                return new ProxyBasedRuntimeTuple(proxy);
            }

            // Decide how to report this, and what to allow
            // TODO:
            throw new InvalidOperationException();
        }

        public static IDbTuple GetType(object obj) {
            return ToTuple(obj).Type;
        }

        public static void SetType(object obj, object type) {
            IDbTuple e = null;
            if (type != null) {
                e = type as IDbTuple;
                if (e == null) {
                    e = ToTuple(type);
                }
            }

            ToTuple(obj).Type = e;
        }

        public static string GetName(object obj) {
            return ToTuple(obj).Name;
        }

        public static void SetName(object obj, string name) {
            ToTuple(obj).Name = name;
        }

        public static IDbTuple GetInherits(object obj) {
            return ToTuple(obj).Inherits;
        }

        public static void SetInherits(object obj, object type) {
            IDbTuple e = null;
            if (type != null) {
                e = type as IDbTuple;
                if (e == null) {
                    e = ToTuple(type);
                }
            }

            ToTuple(obj).Inherits = e;
        }

        public static bool IsType(object obj) {
            return ToTuple(obj).IsType;
        }

        public static void SetIsType(object obj, bool value) {
            ToTuple(obj).IsType = value;
        }

        public static IDbTuple New(object obj) {
            return ToTuple(obj).New();
        }

        public static IDbTuple Derive(object obj) {
            return ToTuple(obj).Derive();
        }

        public static bool TupleEquals(object t1, object t2) {
            return TupleEquals(ToTuple(t1), ToTuple(t2));
        }

        public static bool TupleEquals(IDbTuple t1, IDbTuple t2) {
            if (t1 == null) return t2 == null;
            else if (t2 == null) return false;

            return t1.Proxy.Identity == t2.Proxy.Identity;
        }
    }
}