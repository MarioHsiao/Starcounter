
using System;

namespace Starcounter
{

    public interface ITypeBinding
    {

        /// <summary>
        /// Type binding name.
        /// </summary>
        String Name { get; }

        /// <summary>
        /// Gets the property binding for the property with the specified name.
        /// </summary>
        /// <returns>
        /// A property binding. Returns null is no property with the specified
        /// name exists.
        /// </returns>
        IPropertyBinding GetPropertyBinding(String name);
    }
}
