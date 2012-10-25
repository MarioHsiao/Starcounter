// ***********************************************************************
// <copyright file="ITypeBinding.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

namespace Starcounter.Binding
{

    /// <summary>
    /// Interface ITypeBinding
    /// </summary>
    public interface ITypeBinding
    {

        /// <summary>
        /// Type binding name.
        /// </summary>
        /// <value>The name.</value>
        string Name { get; }

        /// <summary>
        /// Returns number of properties bindings.
        /// </summary>
        /// <value>The property count.</value>
        int PropertyCount { get; }

        /// <summary>
        /// Gets the property binding for the property with the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>A property binding. Returns null is no property with the specified
        /// name exists.</returns>
        IPropertyBinding GetPropertyBinding(string name);

        /// <summary>
        /// Gets the property binding for the property at the specified index.
        /// </summary>
        /// <param name="index">Index of the property binding</param>
        /// <returns>A property binding. Returns null is no property with the specified
        /// name exists.</returns>
        IPropertyBinding GetPropertyBinding(int index);
    }
}
