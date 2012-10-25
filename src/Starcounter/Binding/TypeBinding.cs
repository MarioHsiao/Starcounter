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
        Callback_OnDelete
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
                uppername_ = value.ToUpper();
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

        //private string shortname_;
        /// <summary>
        /// The uppername_
        /// </summary>
        private string uppername_;
        /// <summary>
        /// Gets the name of the upper.
        /// </summary>
        /// <value>The name of the upper.</value>
        public string UpperName { get { return uppername_; } internal set { uppername_ = value; } }

        /// <summary>
        /// News the uninitialized inst.
        /// </summary>
        /// <returns>Entity.</returns>
        protected abstract Entity NewUninitializedInst();

        /// <summary>
        /// News the instance uninit.
        /// </summary>
        /// <returns>Entity.</returns>
        internal Entity NewInstanceUninit()
        {
            return NewUninitializedInst();
        }

        /// <summary>
        /// News the instance.
        /// </summary>
        /// <param name="addr">The addr.</param>
        /// <param name="oid">The oid.</param>
        /// <returns>Entity.</returns>
        internal Entity NewInstance(ulong addr, ulong oid)
        {
            Entity obj = NewUninitializedInst();
            obj.Attach(addr, oid, this);
            return obj;
        }

        /// <summary>
        /// Subs the type of.
        /// </summary>
        /// <param name="tb">The tb.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        internal bool SubTypeOf(TypeBinding tb)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Gets the def handle.
        /// </summary>
        /// <value>The def handle.</value>
        internal ulong DefHandle { get { return TypeDef.TableDef.DefinitionAddr; } }

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
        /// Gets all index infos.
        /// </summary>
        /// <returns>IndexInfo[][].</returns>
        internal IndexInfo[] GetAllIndexInfos()
        {
            return TypeDef.TableDef.GetAllIndexInfos();
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
                if (pb.Name != pb.UpperName)
                    propertyBindingsByName_.Add(pb.UpperName, pb);
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
