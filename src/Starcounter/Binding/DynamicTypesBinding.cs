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
            
            var tb = GetDeclaredTargetType(typeDef);
            if (tb == null) {
                return;
            }

            // Check if the type already exist. If it does, we should not
            // create a new one.

            var rawView = Db.SQL<RawView>("SELECT r FROM RawView r WHERE FullName = ?", typeDef.Name).First;
            if (rawView.AutoTypeInstance > 0) {
                var existingType = DbHelper.FromID(rawView.AutoTypeInstance);
                // Here, we should handle possible refactoring
                // of the type tree.
                // TODO:
                Trace.Assert(existingType.TypeBinding == tb);
                typesDiscovered[typeDef.Name] = rawView.AutoTypeInstance;
                return;
            }
            
            // Check if the type is abstract. If so, what should we
            // do? Have this being an error?
            // See issue #2482
            // TODO:

            // We must enforce in the weaver that hierarchies of
            // types are correct. If there is an "explicit type", such
            // as in our "Car/CarModel" sample, then "CarModel" can
            // not derive just anything.
            // TODO:

            // Create the instance, store the ID of it for future
            // reference in the dictionary and finally update the
            // metadata to reference the auto created instance.
            
            ulong oid = 0, addr = 0;
            DbState.Insert(tb.TableId, ref oid, ref addr);
            var proxy = tb.NewInstance(addr, oid);
            var entity = EntityHelper.ToEntity(proxy);
            entity.Name = typeDef.Name;
            entity.IsType = true;
            if (typeDef.BaseName != null) {
                ulong baseID = typesDiscovered[typeDef.BaseName];
                if (baseID != ulong.MaxValue) {
                    var baseType = DbHelper.FromID(baseID);
                    EntityHelper.SetInherits(entity, baseType);
                }
            }

            typesDiscovered[typeDef.Name] = oid;
            rawView.AutoTypeInstance = oid;
        }

        static TypeBinding GetDeclaredTargetType(TypeDef typeDef) {
            TypeBinding result = null;

            if (typeDef.TypePropertyIndex != -1) {
                // class Foo {
                //   [Type] public Entity MyType;
                // }
                var prop = typeDef.PropertyDefs[typeDef.TypePropertyIndex];
                result = Bindings.GetTypeBinding(prop.TargetTypeName);
            }

            return result;
        }
    }
}