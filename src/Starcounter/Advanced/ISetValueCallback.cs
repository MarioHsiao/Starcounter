
namespace Starcounter.Advanced {

    /// <summary>
    /// Causes Starcounter to notify the implementing database
    /// class every time a database attribute is updated.
    /// </summary>
    public interface ISetValueCallback {
        /// <summary>
        /// Invoked by Starcounter when a database attribute is
        /// being assigned.
        /// </summary>
        /// <param name="attributeName">The name of the attriubyte
        /// that was assigned.</param>
        /// <param name="value">The value assigned to the given
        /// database attribute.</param>
        void OnValueSet(string attributeName, object value);
    }
}