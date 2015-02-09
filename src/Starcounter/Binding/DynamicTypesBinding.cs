﻿using Starcounter.Advanced;
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
        
        public static void DiscoverNewTypes(TypeDef[] unregisteredTypes) {
            if (defaultTypeBinding == null) {
                defaultTypeBinding = Bindings.GetTypeBinding(typeof(Entity).FullName);
            }
            Db.Transact(() => {
                DiscoverTypesAndAssureThem(unregisteredTypes);
            });
        }

        static void DiscoverTypesAndAssureThem(TypeDef[] unregisteredTypes) {
            foreach (var typeDef in unregisteredTypes) {
                ProcessType(typeDef);
            }
        }

        static void ProcessType(TypeDef typeDef) {
            if (typeDef.RuntimeDefaultTypeRef.ObjectID != 0) {
                return;
            }

            if (typeDef.BaseName != null) {
                var parent = Bindings.GetTypeDef(typeDef.BaseName);
                ProcessType(parent);
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

            ulong oid = 0, addr = 0;
            var binding = defaultTypeBinding;
            if (userDeclaredType) {
                Trace.Assert(declaredType != null);
                binding = declaredType;
            } else {
                var rawViewProxy = (IObjectProxy)rawView;
                oid = rawViewProxy.Identity;
                addr = rawViewProxy.ThisHandle;
            }

            DbState.SystemInsert(binding.TableId, ref oid, ref addr);
            var proxy = binding.NewInstance(addr, oid);
            var tuple = TupleHelper.ToTuple(proxy);
            tuple.Name = typeDef.Name;
            tuple.IsType = true;
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