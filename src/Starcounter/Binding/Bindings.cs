// ***********************************************************************
// <copyright file="Bindings.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("QueryProcessingTest, PublicKey=0024000004800000940000000602000000240000525341310004000001000100e758955f5e1537c52891c61cd689a8dd1643807340bd32cc12aee50d2add85eeeaac0b44a796cefb6055fac91836a8a72b5dbf3b44138f508bc2d92798a618ad5791ff0db51b8662d7c936c16b5720b075b2a966bb36844d375c1481c2c7dc4bb54f6d72dbe9d33712aacb6fa0ad84f04bfa6c951f7b9432fe820884c81d67db")]
[assembly: InternalsVisibleTo("IndexQueryTest, PublicKey=0024000004800000940000000602000000240000525341310004000001000100e758955f5e1537c52891c61cd689a8dd1643807340bd32cc12aee50d2add85eeeaac0b44a796cefb6055fac91836a8a72b5dbf3b44138f508bc2d92798a618ad5791ff0db51b8662d7c936c16b5720b075b2a966bb36844d375c1481c2c7dc4bb54f6d72dbe9d33712aacb6fa0ad84f04bfa6c951f7b9432fe820884c81d67db")]

namespace Starcounter.Binding
{

    /// <summary>
    /// Class Bindings
    /// </summary>
    public static class Bindings
    {

        /// <summary>
        /// The type bindings by id_
        /// </summary>
        private static TypeBinding[] typeBindingsById_ = new TypeBinding[0];
        /// <summary>
        /// The type bindings by name_
        /// </summary>
        private static Dictionary<string, TypeBinding> typeBindingsByName_ = new Dictionary<string, TypeBinding>();

        /// <summary>
        /// The type defs by id_
        /// </summary>
        private static TypeDef[] typeDefsById_ = new TypeDef[0];
        /// <summary>
        /// The type defs by name_
        /// </summary>
        private static Dictionary<string, TypeDef> typeDefsByName_ = new Dictionary<string, TypeDef>();

        /// <summary>
        /// The sync root_
        /// </summary>
        private static object syncRoot_ = new object();

        //
        // Note that a type definition must not be registered until the table
        // definition has been synchronized with the database.
        //
        // Only one thread at a time must be allowed to add type definitions.
        //
        /// <summary>
        /// Registers the type defs.
        /// </summary>
        /// <param name="typeDefs">The type defs.</param>
        public static void RegisterTypeDefs(TypeDef[] typeDefs)
        {
            // We don't have to lock here since only one thread at a time will
            // be adding type definitions.

            Dictionary<string, TypeDef> typeDefsByName = new Dictionary<string, TypeDef>(typeDefsByName_);
            TypeDef typeDef;
            for (int i = 0; i < typeDefs.Length; i++)
            {
                typeDef = typeDefs[i];
                // Before adding the unique name, it is necessary to check if it is already there.
                // The only case for this if the unique name has no namespaces and a short name was added before.
                try {
                    typeDefsByName.Add(typeDef.Name, typeDef);
                } catch (ArgumentException) {
#if DEBUG
                    TypeDef alreadyTypeDef;
                    typeDefsByName.TryGetValue(typeDef.Name, out alreadyTypeDef);
                    if (alreadyTypeDef != null)
                        Debug.Assert(alreadyTypeDef.ShortName == typeDef.Name && alreadyTypeDef.Name != typeDef.Name);
#endif
                    typeDefsByName[typeDef.Name] = typeDef;
                }
                // Add lower case name if the name is not already in lower case.
                if (typeDef.Name != typeDef.LowerName)
                    typeDefsByName.Add(typeDef.LowerName, typeDef);
                // Add short name, i.e., without namespaces, if the original name is not the short name.
                // Short name don't need to be unique, since the same class name can be given in different namespaces.
                // It is important to check if the existing short name is actual name of a class with no namespaces.
                if (typeDef.LowerName != typeDef.ShortName) {
                    TypeDef alreadyTypeDef;
                    if (typeDefsByName.TryGetValue(typeDef.ShortName, out alreadyTypeDef)) {
                        if (alreadyTypeDef != null) // Already ambiguous short names
                            if (typeDef.ShortName != alreadyTypeDef.Name) // If equal then stored short name is real name
                                typeDefsByName[typeDef.ShortName] = null; // Ambiguous short name
                    } else typeDefsByName.Add(typeDef.ShortName, typeDef); // New short name
                }
            }

            List<TypeDef> typeDefsById = new List<TypeDef>(typeDefsById_);
            for (int i = 0; i < typeDefs.Length; i++)
            {
                typeDef = typeDefs[i];
                var tableId = typeDef.TableDef.TableId;
                while (typeDefsById.Count <= tableId) typeDefsById.Add(null);
                typeDefsById[tableId] = typeDef;
            }

            // No one will be requesting a type not previously registered so we
            // do not have to worry that the different maps are not in sync.

            typeDefsById_ = typeDefsById.ToArray();
            typeDefsByName_ = typeDefsByName;
        }

        /// <summary>
        /// Gets the type def.
        /// </summary>
        /// <param name="tableId">The table id.</param>
        /// <returns>TypeDef.</returns>
        public static TypeDef GetTypeDef(int tableId)
        {
            return typeDefsById_[tableId];
        }

        /// <summary>
        /// Gets the type def.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>TypeDef.</returns>
        public static TypeDef GetTypeDef(string name)
        {
            TypeDef typeDef;
            typeDefsByName_.TryGetValue(name, out typeDef);
            return typeDef;
        }

        /// <summary>
        /// Gets the type def.
        /// </summary>
        /// <param name="name">The name, which case doesn't need match.</param>
        /// <returns>TypeDef.</returns>
        public static TypeDef GetTypeDefInsensitive(string name) {
            TypeDef typeDef;
            typeDefsByName_.TryGetValue(name.ToLower(), out typeDef);
            return typeDef;
        }

        /// <summary>
        /// Gets all type defs.
        /// </summary>
        /// <returns>IEnumerable{TypeDef}.</returns>
        internal static IEnumerable<TypeDef> GetAllTypeDefs()
        {
            return typeDefsByName_.Values;
        }

        /// <summary>
        /// Gets the type binding.
        /// </summary>
        /// <param name="tableId">The table id.</param>
        /// <returns>TypeBinding.</returns>
#if ERIK_TEST
        public static TypeBinding GetTypeBinding(int tableId)
#else
        internal static TypeBinding GetTypeBinding(int tableId)
#endif
        {
            // TODO:
            // Can we make this so that not found always raised exception?
            TypeBinding tb;
            try
            {
                tb = typeBindingsById_[tableId];
            }
            catch (IndexOutOfRangeException)
            {
                tb = null;
            }

            if (tb != null) return tb;

            return BuildTypeBindingFromTypeDef(tableId);
        }

        /// <summary>
        /// Gets the type binding.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>TypeBinding.</returns>
        internal static TypeBinding GetTypeBinding(string name)
        {
            try
            {
                return typeBindingsByName_[name];
            }
            catch (KeyNotFoundException) { }

            return BuildTypeBindingFromTypeDef(name);
        }

        /// <summary>
        /// Gets the type binding.
        /// </summary>
        /// <param name="name">The name, which case doesn't need to match.</param>
        /// <returns>TypeBinding.</returns>
        internal static TypeBinding GetTypeBindingInsensitive(string name) {
            try {
                return typeBindingsByName_[name.ToLower()];
            } catch (KeyNotFoundException) { }

            return BuildTypeBindingFromTypeDef(name.ToLower());
        }

        /// <summary>
        /// Builds the type binding from type def.
        /// </summary>
        /// <param name="tableId">The table id.</param>
        /// <returns>TypeBinding.</returns>
        private static TypeBinding BuildTypeBindingFromTypeDef(int tableId)
        {
            try
            {
                return BuildTypeBindingFromTypeDef(typeDefsById_[tableId]);
            }
            catch (IndexOutOfRangeException)
            {
                throw CreateExceptionOnTypeDefNotFound();
            }
        }

        /// <summary>
        /// Builds the type binding from type def.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>TypeBinding.</returns>
        private static TypeBinding BuildTypeBindingFromTypeDef(string name)
        {
            TypeDef typeDef;
            typeDefsByName_.TryGetValue(name, out typeDef);
            return BuildTypeBindingFromTypeDef(typeDef);
        }

        /// <summary>
        /// Builds the type binding from type def.
        /// </summary>
        /// <param name="typeDef">The type def.</param>
        /// <returns>TypeBinding.</returns>
        private static TypeBinding BuildTypeBindingFromTypeDef(TypeDef typeDef)
        {
            if (typeDef == null) throw CreateExceptionOnTypeDefNotFound();

            var currentAndBaseTableIds = BuildCurrentAndBaseTableIdArray(typeDef);

            lock (syncRoot_)
            {
                return LockedBuildTypeBindingFromTypeDef(typeDef, currentAndBaseTableIds);
            }
        }

        private static ushort[] BuildCurrentAndBaseTableIdArray(TypeDef typeDef)
        {
            // Output is expected to be sorted lesser to greater.

            var currentAndBaseTableIdList = new List<ushort>();
            var typeDef2 = typeDef;
            for (; ; )
            {
                currentAndBaseTableIdList.Add(typeDef2.TableDef.TableId);
                if (typeDef2.BaseName == null) break;
                typeDef2 = GetTypeDef(typeDef2.BaseName);
            }
            currentAndBaseTableIdList.Sort();
            var currentAndBaseTableIds = currentAndBaseTableIdList.ToArray();
            return currentAndBaseTableIds;
        }

        private static TypeBinding LockedBuildTypeBindingFromTypeDef(TypeDef typeDef, ushort[] currentAndBaseTableIds)
        {
            // Check if some other thread has added a type binding for the
            // specific type while we where waiting to acquire the lock.

            TypeBinding tb = null;
            var tableId = typeDef.TableDef.TableId;
            if (typeBindingsById_.Length > tableId)
            {
                tb = typeBindingsById_[tableId];
            }

            if (tb == null)
            {
                BindingBuilder builder = new BindingBuilder(typeDef, currentAndBaseTableIds);
                tb = builder.CreateTypeBinding();
#if false
                builder.WriteAssemblyToDisk();
#endif
                AddTypeBinding(tb);
            }

            return tb;
        }

        /// <summary>
        /// Adds the type binding.
        /// </summary>
        /// <param name="typeBinding">The type binding.</param>
        private static void AddTypeBinding(TypeBinding typeBinding)
        {
            Dictionary<string, TypeBinding> typeBindingsByName = new Dictionary<string, TypeBinding>(typeBindingsByName_);
            typeBindingsByName.Add(typeBinding.Name, typeBinding);
            // Add lower case name if the name is not already in lower case.
            if (typeBinding.Name != typeBinding.LowerName)
                typeBindingsByName.Add(typeBinding.LowerName, typeBinding);

            List<TypeBinding> typeBindingsById = new List<TypeBinding>(typeBindingsById_);
            var tableId = typeBinding.TypeDef.TableDef.TableId;
            while (typeBindingsById.Count <= tableId) typeBindingsById.Add(null);
            typeBindingsById[tableId] = typeBinding;

            typeBindingsById_ = typeBindingsById.ToArray();
            typeBindingsByName_ = typeBindingsByName;
        }

        /// <summary>
        /// Creates the exception on type def not found.
        /// </summary>
        /// <returns>Exception.</returns>
        private static Exception CreateExceptionOnTypeDefNotFound()
        {
            // This should not happen. No one should be requesting type binding
            // for a type that hasn't be registered. Schema must not have been
            // provided properly.

            throw ErrorCode.ToException(Error.SCERRSCHEMACODEMISMATCH);
        }
    }
}
