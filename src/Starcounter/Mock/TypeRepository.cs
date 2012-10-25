// ***********************************************************************
// <copyright file="TypeRepository.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

namespace Starcounter.Binding
{

    /// <summary>
    /// Class TypeRepository
    /// </summary>
    internal static class TypeRepository
    {

        /// <summary>
        /// Gets the type binding.
        /// </summary>
        /// <param name="tableId">The table id.</param>
        /// <returns>TypeBinding.</returns>
        internal static TypeBinding GetTypeBinding(ushort tableId)
        {
            return Bindings.GetTypeBinding(tableId);
        }

        /// <summary>
        /// Gets the type binding.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>TypeBinding.</returns>
        internal static TypeBinding GetTypeBinding(string name)
        {
            return Bindings.GetTypeBinding(name);
        }

        /// <summary>
        /// Tries the short name of the get type binding by.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="typeBind">The type bind.</param>
        /// <returns>System.Int32.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        internal static int TryGetTypeBindingByShortName(string name, out TypeBinding typeBind)
        {
            throw new System.NotImplementedException();
        }
    }
}
