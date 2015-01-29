﻿// ***********************************************************************
// <copyright file="TypeLoader.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Reflection;

namespace Starcounter.Binding
{

    /// <summary>
    /// Class TypeLoader
    /// </summary>
    public class TypeLoader
    {
        static readonly AssemblyName StarcounterAssemblyName = new AssemblyName("Starcounter");

        /// <summary>
        /// The assembly name_
        /// </summary>
        private readonly AssemblyName assemblyName_;
        /// <summary>
        /// The type name_
        /// </summary>
        private readonly string typeName_;

        /// <summary>
        /// Gets the assembly name of the current instance.
        /// </summary>
        public AssemblyName AssemblyName {
            get { return assemblyName_; }
        }

        /// <summary>
        /// Gets the name of the current type, scoped by
        /// the assembly.
        /// </summary>
        public string ScopedName {
            get {
                var version = assemblyName_.Version == null ? "null" : assemblyName_.Version.ToString();
                return string.Format("{0}, ({1}, Version={2})", typeName_, assemblyName_.Name, version);
            }
        }

        /// <summary>
        /// Gets a value that indicates if the type to load is to be loaded from
        /// one of the Starcounter assemblies.
        /// </summary>
        internal bool LoadsFromStarcounterAssembly {
            get {
                return object.ReferenceEquals(assemblyName_, StarcounterAssemblyName);
            }
        }

        /// <summary>
        /// Factory method that creates a type loader for a type hosted
        /// in the Starcounter assembly.
        /// </summary>
        /// <param name="name">The name of the type to create the loader
        /// from.</param>
        /// <returns>A type loader responsble for loading the given
        /// Starcounter type.</returns>
        public static TypeLoader ForStarcounterType(string name) {
            return new TypeLoader(StarcounterAssemblyName, name);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeLoader" /> class.
        /// </summary>
        /// <param name="assemblyName">Name of the assembly.</param>
        /// <param name="typeName">Name of the type.</param>
        public TypeLoader(AssemblyName assemblyName, string typeName)
        {
            assemblyName_ = assemblyName;
            typeName_ = typeName;
        }

        /// <summary>
        /// Loads this instance.
        /// </summary>
        /// <returns>Type.</returns>
        public Type Load()
        {
            Assembly a = Assembly.Load(assemblyName_);
            Type t = a.GetType(typeName_, true);
            return t;
        }
    }
}
