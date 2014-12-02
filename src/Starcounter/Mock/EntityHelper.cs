
namespace Starcounter {

    /// <summary>
    /// Helper class to access dynamic types properties and fields
    /// for database classes defined only with the [Database] attribute,
    /// and not inheriting <see cref="Entity"/>.
    /// </summary>
    public static class EntityHelper {

        public static IEntity2 GetType(object obj) {
            return Entity.From(obj).Type;
        }

        public static void SetType(object obj, object type) {
            IEntity2 e = null;
            if (type != null) {
                e = type as IEntity2;
                if (e == null) {
                    e = Entity.From(type);
                }
            }

            Entity.From(obj).Type = e;
        }

        public static string GetName(object obj) {
            return Entity.From(obj).Name;
        }

        public static void SetName(object obj, string name) {
            Entity.From(obj).Name = name;
        }

        public static IEntity2 GetInherits(object obj) {
            return Entity.From(obj).Inherits;
        }

        public static void SetInherits(object obj, object type) {
            IEntity2 e = null;
            if (type != null) {
                e = type as IEntity2;
                if (e == null) {
                    e = Entity.From(type);
                }
            }

            Entity.From(obj).Inherits = e;
        }

        public static bool IsType(object obj) {
            return Entity.From(obj).IsType;
        }

        public static void SetIsType(object obj, bool value) {
            Entity.From(obj).IsType = value;
        }
    }
}
