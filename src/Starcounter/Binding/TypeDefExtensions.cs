
using System;

namespace Starcounter.Binding {

    internal static class TypeDefExtensions {
        /// <summary>
        /// Return <c>true</c> if <paramref name="property"/>, resolved
        /// from <paramref name="resolvedFrom"/> is defined by the user, or
        /// inherited from a Starcounter base type.
        /// </summary>
        public static bool IsUserDefinedProperty(this TypeDef resolvedFrom, int property) {
            if (resolvedFrom.IsStarcounterType) {
                return false;
            }

            if (resolvedFrom.BaseName == null) {
                return true;
            }

            var prop = resolvedFrom.PropertyDefs[property];
            var baseDef = Bindings.GetTypeDef(resolvedFrom.BaseName);
            if (property < baseDef.PropertyDefs.Length) {
                return baseDef.IsUserDefinedProperty(property);
            }

            return true;
        }

        /// <summary>
        /// Returns a value indicating if <paramref name="type"/> defines the
        /// property with index <paramref name="property"/>. If it's defined
        /// in a base type, this method returns false. If it's out-of-range,
        /// a corresponding exception is raised.
        /// </summary>
        /// <param name="resolvedFrom"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        public static bool DefinesProperty(this TypeDef type, int property) {
            if (property >= type.PropertyDefs.Length) {
                throw new ArgumentOutOfRangeException();
            }
            if (type.BaseName == null) {
                return true;
            }
            var baseDef = Bindings.GetTypeDef(type.BaseName);
            return property >= baseDef.PropertyDefs.Length;
        }
    }
}
