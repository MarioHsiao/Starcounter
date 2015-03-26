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
        static TypeBinding defaultTypeBinding;
        static Dictionary<string, TypeDef> customTypeClasses;
        
        public static void DiscoverNewTypes(TypeDef[] unregisteredTypes, TypeDef[] customTypes) {
            if (defaultTypeBinding == null) {
                defaultTypeBinding = Bindings.GetTypeBinding(typeof(Entity).FullName);
                customTypeClasses = new Dictionary<string, TypeDef>();
            }

            Db.Transact(() => {
                RegisterCustomTypeClasses(customTypes);
                DiscoverTypesAndAssureThem(unregisteredTypes);
            });
        }

        static void RegisterCustomTypeClasses(TypeDef[] customTypes) {
            foreach (var t in customTypes) {
                if (!customTypeClasses.ContainsKey(t.Name)) {
                    customTypeClasses.Add(t.Name, t);
                }
            }
        }

        static bool IsCustomTypeClass(TypeDef t) {
            return customTypeClasses.ContainsKey(t.Name);
        }

        static void DiscoverTypesAndAssureThem(TypeDef[] unregisteredTypes) {
            foreach (var typeDef in unregisteredTypes) {
                if (!IsImplicitType(typeDef)) {
                    ProcessType(typeDef);
                }
            }
        }

        static void ProcessType(TypeDef typeDef) {
            if (typeDef.RuntimeDefaultTypeRef.ObjectID != 0) {
                return;
            }

            TypeDef parent = null;
            if (HasDeclaredBaseType(typeDef)) {
                parent = Bindings.GetTypeDef(typeDef.BaseName);
                ProcessType(parent);
            }

            var isTypeClass = IsCustomTypeClass(typeDef);
            if (isTypeClass) {
                // Do whatever we need to do.
                // TODO:
            }

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
                Trace.Assert(declaredType == null || existingType.TypeBinding == declaredType);
                
                typeDef.RuntimeDefaultTypeRef.ObjectID = existingType.Identity;
                typeDef.RuntimeDefaultTypeRef.Address = existingType.ThisHandle;
                return;
            }

            // The type is not yet bound to its type. We should assign it
            // to one, dictated by the above declared type binding (if present),
            // or the raw view in case its not declared or inherited from the
            // system.

            var binding = defaultTypeBinding;
            if (userDeclaredType) {
                Trace.Assert(declaredType != null);
                binding = declaredType;
            }

            var tuple = NewSystemAutoType(binding);
            tuple.Name = typeDef.Name;
            tuple.IsType = true;

            IDbTuple baseTuple = null;
            if (parent != null) {
                var baseRef = DbHelper.FromID(parent.RuntimeDefaultTypeRef.ObjectID);
                Trace.Assert(baseRef != null);
                baseTuple = TupleHelper.ToTuple(baseRef);
                tuple.Inherits = baseTuple;
            }

            rawView.AutoTypeInstance = tuple.Proxy.Identity;
            typeDef.RuntimeDefaultTypeRef.ObjectID = tuple.Proxy.Identity;
            typeDef.RuntimeDefaultTypeRef.Address = tuple.Proxy.ThisHandle;

            // We'll also create a "type type", always using the default
            // auto system type binding as the template, and assigning it
            // either no name, or the name of the declared custom type.

            var typeTypetuple = NewSystemAutoType();
            typeTypetuple.Name = userDeclaredType ? declaredType.Name : null;
            typeTypetuple.IsType = true;
            if (baseTuple != null) {
                Trace.Assert(baseTuple.Type != null);
                typeTypetuple.Inherits = baseTuple.Type;
            }
            tuple.Type = typeTypetuple;
        }

        static bool IsImplicitType(TypeDef type) {
            return IsImplicitType(type.Name);
        }

        static bool IsImplicitType(string name) {
            return name == typeof(ImplicitEntity).FullName;
        }

        static bool HasDeclaredBaseType(TypeDef type) {
            return type.BaseName != null && !IsImplicitType(type.BaseName);
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
                userDefined = typeDef.IsUserDefinedProperty(typeDef.TypePropertyIndex);
                result = Bindings.GetTypeBinding(prop.TargetTypeName);
            }

            return result;
        }

        static IDbTuple NewSystemAutoType(TypeBinding binding = null) {
            binding = binding ?? defaultTypeBinding;
            ulong oid = 0, addr = 0;
            DbState.SystemInsert(binding.TableId, ref oid, ref addr);
            var proxy = binding.NewInstance(addr, oid);
            return TupleHelper.ToTuple(proxy);
        }
    }
}