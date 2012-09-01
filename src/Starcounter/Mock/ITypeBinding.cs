
using System;

namespace Starcounter
{

    public interface ITypeBinding
    {

        /// <summary>
        /// Type binding name.
        /// </summary>
        String Name
        {
            get;
        }

        /// <summary>
        /// Number of properties.
        /// </summary>
        Int32 PropertyCount
        {
            get;
        }

        /// <summary>
        /// Gets the property index for the property with the specified name.
        /// </summary>
        /// <returns>
        /// Index of the the property. A value of -1 if no property with the
        /// specified name is availible.
        /// </returns>
        Int32 GetPropertyIndex(String name);

        /// <summary>
        /// Gets the property binding for the property with the specified
        /// index.
        /// </summary>
        /// <returns>
        /// A property binding. Never null, throws an exception is the index is
        /// invalid.
        /// </returns>
        IPropertyBinding GetPropertyBinding(Int32 index);

        /// <summary>
        /// Gets the property binding for the property with the specified name.
        /// </summary>
        /// <returns>
        /// A property binding. Returns null is no property with the specified
        /// name exists.
        /// </returns>
        IPropertyBinding GetPropertyBinding(String name);

        //PI110503
        ///// <summary>
        ///// Gets the property binding for the property with the name specified  
        ///// in upper case.
        ///// </summary>
        ///// <returns>
        ///// A property binding. Returns null if no property with the specified
        ///// name exists.
        ///// </returns>
        //IPropertyBinding GetPropertyBindingByUpperCaseName(String name);
    }
}
