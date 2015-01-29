using Starcounter.Advanced;
using Starcounter.Internal;
using Starcounter.Metadata;
using System.Collections.Generic;
using System.Diagnostics;

namespace Starcounter.Binding {

    /// <summary>
    /// Governs the binding processing needed when "dynamic types"
    /// are in use.
    /// </summary>
    internal static class DynamicTypesBinding {
        // Code host level "cache" that indicates what defined
        // types have already been processed.
        static Dictionary<string, ulong> typesDiscovered = new Dictionary<string, ulong>();

        public static void DiscoverNewTypes(TypeDef[] unregisteredTypes) {
            Db.Transaction(() => {
                DiscoverTypesAndAssureThem(unregisteredTypes);
            });
        }

        static void DiscoverTypesAndAssureThem(TypeDef[] unregisteredTypes) {
            foreach (var typeDef in unregisteredTypes) {
                ProcessType(typeDef);
            }
        }

        static void ProcessType(TypeDef typeDef) {
            if (typesDiscovered.ContainsKey(typeDef.Name)) {
                return;
            }

            if (typeDef.BaseName != null) {
                var parent = Bindings.GetTypeDef(typeDef.BaseName);
                ProcessType(parent);
            }

            typesDiscovered.Add(typeDef.Name, ulong.MaxValue);

            bool userDeclaredType;
            var declaredType = GetDeclaredTargetType(typeDef, out userDeclaredType);

            // Check if the type already exist. If it does, we should not
            // create a new one.

            var rawView = Db.SQL<RawView>("SELECT r FROM RawView r WHERE FullName = ?", typeDef.Name).First;
            if (rawView.AutoTypeInstance > 0) {
                var existingType = DbHelper.FromID(rawView.AutoTypeInstance) as IObjectProxy;

                // Here, we should handle possible refactoring
                // of the type tree.
                // When that is implemented, we can remove the
                // below asserts
                // TODO:

                if (existingType is RawView) {
                    Trace.Assert(declaredType == null || typeDef.IsStarcounterType);
                } else {
                    Trace.Assert(declaredType != null && existingType.TypeBinding == declaredType);
                }
                
                typesDiscovered[typeDef.Name] = rawView.AutoTypeInstance;
                typeDef.RuntimeDefaultTypeRef.ObjectID = existingType.Identity;
                typeDef.RuntimeDefaultTypeRef.Address = existingType.ThisHandle;
                return;
            }

            // The type is not yet bound to its type. We should assign it
            // to one, dictated by the above declared type binding (if present),
            // or the raw view in case its not declared or inherited from the
            // system.

            ulong oid = 0, addr = 0;
            if (userDeclaredType) {
                Trace.Assert(declaredType != null);

                // Check if the type is abstract. If so, what should we
                // do? Have this being an error?
                // See issue #2482
                // TODO:

                // We must enforce in the weaver that hierarchies of
                // types are correct. If there is an "explicit type", such
                // as in our "Car/CarModel" sample, then "CarModel" can
                // not derive just anything.
                // TODO:
                
                DbState.SystemInsert(declaredType.TableId, ref oid, ref addr);
                var proxy = declaredType.NewInstance(addr, oid);
                var tuple = TupleHelper.ToTuple(proxy);
                tuple.Name = typeDef.Name;
                tuple.IsType = true;
                if (typeDef.BaseName != null) {
                    ulong baseID = typesDiscovered[typeDef.BaseName];
                    if (baseID != ulong.MaxValue) {
                        var baseType = DbHelper.FromID(baseID);
                        TupleHelper.SetInherits(tuple, baseType);
                    }
                }

            } else {
                var rawViewProxy = (IObjectProxy)rawView;
                oid = rawViewProxy.Identity;
                addr = rawViewProxy.ThisHandle;
            }

            typesDiscovered[typeDef.Name] = oid;
            rawView.AutoTypeInstance = oid;
            typeDef.RuntimeDefaultTypeRef.ObjectID = oid;
            typeDef.RuntimeDefaultTypeRef.Address = addr;
        }

        static TypeBinding GetDeclaredTargetType(TypeDef typeDef, out bool userDefined) {
            TypeBinding result = null;
            userDefined = false;

            if (typeDef.TypePropertyIndex != -1) {
                // class Foo {
                //   [Type] public Entity MyType;
                // }

                // About user defined types:
                // If the typeDef does extend Entity, we should return
                // FALSE for user-defined type declaration if what is
                // returned is Entity.Type's PropertyDef. If the type
                // does not extend Entity, it's always user defined.

                var prop = typeDef.PropertyDefs[typeDef.TypePropertyIndex];
                userDefined = IsUserDefinedProperty(typeDef.TypePropertyIndex, typeDef);
                result = Bindings.GetTypeBinding(prop.TargetTypeName);
            }

            return result;
        }

        /// <summary>
        /// Return <c>true</c> if <paramref name="property"/>, resolved
        /// from <paramref name="resolvedFrom"/> is defined by the user, or
        /// inherited from a Starcounter base type.
        /// </summary>
        static bool IsUserDefinedProperty(int property, TypeDef resolvedFrom) {
            if (resolvedFrom.IsStarcounterType) {
                return false;
            }

            if (resolvedFrom.BaseName == null) {
                return true;
            }

            var prop = resolvedFrom.PropertyDefs[property];
            var baseDef = Bindings.GetTypeDef(resolvedFrom.BaseName);
            if (property < baseDef.PropertyDefs.Length) {
                return IsUserDefinedProperty(property, baseDef);
            }

            return true;
        }
    }
}