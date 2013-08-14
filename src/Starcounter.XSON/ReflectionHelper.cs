

using System;
using System.Reflection;
namespace Starcounter.Internal.XSON {
    internal static class ReflectionHelper {

        /// <summary>
        /// Finds the first property with the specified name
        /// </summary>
        /// <param name="type">The type to search in</param>
        /// <param name="name">The name of the property</param>
        /// <returns>The reflected property</returns>
        internal static MemberInfo FindPropertyOrField(Type type, string name) {
            PropertyInfo[] pis = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            PropertyInfo pi = null;
            foreach (var tmp in pis) {
                if (tmp.Name == name) {
                    pi = tmp;
                    break;
                }
            }
            if (pi == null) {
                FieldInfo[] fis = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
                FieldInfo fi = null;
                foreach (var tmp2 in fis) {
                    if (tmp2.Name == name) {
                        fi = tmp2;
                        break;
                    }
                }
                return fi;
            }
            return pi;
        }
    }
}
