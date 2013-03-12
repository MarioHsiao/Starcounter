// ***********************************************************************
// <copyright file="TypeBinding.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Starcounter.Binding
{

    /// <summary>
    /// Enum TypeBindingFlags
    /// </summary>
    [Flags]
    internal enum TypeBindingFlags
    {

        /// <summary>
        /// The callback_ on delete
        /// </summary>
        Callback_OnDelete = 1
    }

    /// <summary>
    /// Class TypeBinding
    /// </summary>
    public abstract class TypeBinding : ITypeBinding
    {

        /// <summary>
        /// The flags
        /// </summary>
        internal TypeBindingFlags Flags;

        /// <summary>
        /// The type def
        /// </summary>
        internal TypeDef TypeDef;

        /// <summary>
        /// The property bindings_
        /// </summary>
        private PropertyBinding[] propertyBindings_;
        /// <summary>
        /// The property bindings by name_
        /// </summary>
        private Dictionary<string, PropertyBinding> propertyBindingsByName_;

        /// <summary>
        /// The name_
        /// </summary>
        private string name_;

        /// <summary>
        /// Type binding name.
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get { return name_; }
            internal set
            {
                name_ = value;
                lowername_ = value.ToLower();
                int pos = value.LastIndexOf('.');
                if (pos == -1)
                    shortname_ = value;
                else
                    shortname_ = value.Substring(pos + 1).ToLower();
            }
        }

        /// <summary>
        /// The table id_
        /// </summary>
        private ushort tableId_;

        /// <summary>
        /// Gets the table id.
        /// </summary>
        /// <value>The table id.</value>
        public ushort TableId { get { return tableId_; } internal set { tableId_ = value; } }

        /// <summary>
        /// Short name is class name, i.e., last name of the full name.
        /// </summary>
        private string shortname_;
        /// <summary>
        /// The uppername_
        /// </summary>
        private string lowername_;

        /// <summary>
        /// Gets the name of the lower.
        /// </summary>
        /// <value>The name of the lower.</value>
        public string LowerName { get { return lowername_; } internal set { lowername_ = value; } }

        /// <summary>
        /// Gets the class name without namespaces in lowercase.
        /// </summary>
        public string ShortName { get { return shortname_; } internal set { shortname_ = value; } }

        private ushort[] currentAndBaseTableIds_; // Sorted lowest to highest.

        internal void SetCurrentAndBaseTableIds(ushort[] currentAndBaseTableIds)
        {
            currentAndBaseTableIds_ = currentAndBaseTableIds;
        }

        /// <summary>
        /// News the uninitialized inst.
        /// </summary>
        /// <returns>Entity.</returns>
        protected abstract IObjectView NewUninitializedInst();

        /// <summary>
        /// News the instance uninit.
        /// </summary>
        /// <returns>Entity.</returns>
#if ERIK_TEST
        public Entity NewInstanceUninit()
#else
        internal IObjectView NewInstanceUninit()
#endif
        {
            return NewUninitializedInst();
        }

        /// <summary>
        /// News the instance.
        /// </summary>
        /// <param name="addr">The addr.</param>
        /// <param name="oid">The oid.</param>
        /// <returns>Entity.</returns>
        internal IObjectView NewInstance(ulong addr, ulong oid)
        {
            IObjectView obj = NewUninitializedInst();
            obj.Attach(addr, oid, this);
            return obj;
        }

        /// <summary>
        /// Controls if this type-binding represents a subtype of the type of the input type-binding (superTypeBind).
        /// </summary>
        /// <param name="superTypeBind">Input type-binding.</param>
        /// <returns>True, if this type-binding represents a subtype of (or equals) the type of the input type-binding (superTypeBind),
        /// otherwise false.</returns>
        public bool SubTypeOf(TypeBinding superTypeBind)
        {
            try
            {
                var superTableId = superTypeBind.tableId_;
                var currentAndBaseTableIds = currentAndBaseTableIds_;
                for (var i = 0; i < currentAndBaseTableIds.Length; i++)
                {
                    var tableId = currentAndBaseTableIds[i];
                    if (tableId == superTableId) return true;
                    if (tableId > superTableId) return false;
                }
                return false;
            }
            catch (NullReferenceException)
            {
                if (superTypeBind == null) return false;
                throw;
            }
        }

        /// <summary>
        /// Gets the property binding.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>PropertyBinding.</returns>
        internal PropertyBinding GetPropertyBinding(int index)
        {
            return propertyBindings_[index];
        }

        /// <summary>
        /// Gets the property binding.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>PropertyBinding.</returns>
        internal PropertyBinding GetPropertyBinding(string name)
        {
            PropertyBinding pb;
            propertyBindingsByName_.TryGetValue(name, out pb);
            return pb;
        }

        /// <summary>
        /// Gets the property binding.
        /// </summary>
        /// <param name="name">The name, which case does not need to match.</param>
        /// <returns>PropertyBinding.</returns>
        internal PropertyBinding GetPropertyBindingInsensitive(string name) {
            PropertyBinding pb;
            propertyBindingsByName_.TryGetValue(name.ToLower(), out pb);
            return pb;
        }

        /// <summary>
        /// Gets all index infos.
        /// </summary>
        /// <returns>IndexInfo[][].</returns>
        internal IndexInfo[] GetAllIndexInfos()
        {
            return TypeDef.TableDef.GetAllIndexInfos();
        }

        /// <summary>
        /// Gets all index infos including base classes.
        /// </summary>
        /// <returns>Array of index infos.</returns>
        internal IndexInfo[] GetAllInheritedIndexInfos() {
            IndexInfo[] thisInfos = TypeDef.TableDef.GetAllIndexInfos();
            // Get parent indexes
            if (TypeDef.BaseName == null)
                return thisInfos;
            IndexInfo[] parentInfos = Bindings.GetTypeBinding(TypeDef.BaseName).GetAllInheritedIndexInfos();
            // Merge into one array
            IndexInfo[] resultInfos = new IndexInfo[thisInfos.Length + parentInfos.Length];
            thisInfos.CopyTo(resultInfos, 0);
            parentInfos.CopyTo(resultInfos, thisInfos.Length);
            return resultInfos;
        }

        /// <summary>
        /// Gets the index info.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>IndexInfo.</returns>
        internal IndexInfo GetIndexInfo(string name)
        {
            return TypeDef.TableDef.GetIndexInfo(name);
        }

        /// <summary>
        /// Gets the index info for the given index in this type or its supertypes.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>IndexInfo.</returns>
        internal IndexInfo GetInheritedIndexInfo(string name) {
            IndexInfo indx = TypeDef.TableDef.GetIndexInfo(name);
            if (indx != null)
                return indx;
            if (TypeDef.BaseName == null)
                return null;
            return Bindings.GetTypeBinding(TypeDef.BaseName).GetInheritedIndexInfo(name);
        }

        /// <summary>
        /// Sets the property bindings.
        /// </summary>
        /// <param name="propertyBindings">The property bindings.</param>
        internal void SetPropertyBindings(PropertyBinding[] propertyBindings)
        {
            propertyBindings_ = propertyBindings;

            propertyBindingsByName_ = new Dictionary<string, PropertyBinding>(propertyBindings.Length);
            for (int i = 0; i < propertyBindings.Length; i++)
            {
                PropertyBinding pb = propertyBindings[i];
                propertyBindingsByName_.Add(pb.Name, pb);
                if (pb.Name != pb.LowerName)
                    propertyBindingsByName_.Add(pb.LowerName, pb);
            }
        }

        /// <summary>
        /// Gets the property binding for the property with the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>A property binding. Returns null is no property with the specified
        /// name exists.</returns>
        IPropertyBinding ITypeBinding.GetPropertyBinding(string name)
        {
            return GetPropertyBinding(name);
        }

        /// <summary>
        /// Gets the property binding for the property at the specified index.
        /// </summary>
        /// <param name="index">Index of the property binding</param>
        /// <returns>A property binding. Returns null is no property with the specified
        /// name exists.</returns>
        IPropertyBinding ITypeBinding.GetPropertyBinding(int index)
        {
            if (index < propertyBindings_.Length)
                return GetPropertyBinding(index);
            else
                return null;
        }

        /// <summary>
        /// Returns number of properties bindings.
        /// </summary>
        /// <value>The property count.</value>
        int ITypeBinding.PropertyCount { get { return propertyBindings_.Length; } }
    }
}
