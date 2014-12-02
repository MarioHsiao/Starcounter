
using Sc.Server.Weaver;
using Starcounter.Binding;
using Starcounter.Internal;
using System;

namespace Starcounter {

    /// <summary>
    /// Helper class to access dynamic types properties and fields
    /// for database classes defined only with the [Database] attribute,
    /// and not inheriting <see cref="Entity"/>.
    /// </summary>
    public abstract class EntityHelper {

        static IEntity2 EntityFromObject(object obj) {
            return Entity.From(obj);
        }

        public static IEntity2 GetType(object obj) {
            return Entity.From(obj).Type;
        }

        public static void SetType(object obj, object type) {
            var wrapper = EntityFromObject(obj);
            wrapper.Type = (IEntity2)type;
        }

        public static string GetTypeName(object obj) {
            var wrapper = EntityFromObject(obj);
            return wrapper.TypeName;
        }

        public static void SetTypeName(object obj, string name) {
            var wrapper = EntityFromObject(obj);
            wrapper.TypeName = name;
        }

        public static IEntity2 GetInherits(object obj) {
            var wrapper = EntityFromObject(obj);
            return wrapper.Inherits;
        }

        public static void SetInherits(object obj, object type) {
            var wrapper = EntityFromObject(obj);
            wrapper.Inherits = (IEntity2)type;
        }

        public static bool IsType(object obj) {
            var wrapper = EntityFromObject(obj);
            return wrapper.IsType;
        }

        public static void SetIsType(object obj, bool value) {
            var wrapper = EntityFromObject(obj);
            wrapper.IsType = value;
        }
    }
}
