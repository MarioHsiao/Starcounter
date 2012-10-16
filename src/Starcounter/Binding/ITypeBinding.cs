

namespace Starcounter.Binding
{

    public interface ITypeBinding
    {

        /// <summary>
        /// Type binding name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Returns number of properties bindings.
        /// </summary>
        int PropertyCount { get; }

        /// <summary>
        /// Gets the property binding for the property with the specified name.
        /// </summary>
        /// <returns>
        /// A property binding. Returns null is no property with the specified
        /// name exists.
        /// </returns>
        IPropertyBinding GetPropertyBinding(string name);

        /// <summary>
        /// Gets the property binding for the property at the specified index.
        /// </summary>
        /// <param name="index">Index of the property binding</param>
        /// <returns>
        /// A property binding. Returns null is no property with the specified
        /// name exists.
        /// </returns>
        IPropertyBinding GetPropertyBinding(int index);
    }
}
