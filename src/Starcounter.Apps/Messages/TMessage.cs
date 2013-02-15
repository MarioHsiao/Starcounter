

namespace Starcounter.Templates {

    /// <summary>
    /// Defines the schema (properties) for a Message object.
    /// </summary>
    public class TMessage : TObj {

        /// <summary>
        /// Creates a new Message using the schema defined by this template
        /// </summary>
        /// <param name="parent">The parent for the new message (if any)</param>
        /// <returns>The new message</returns>
        public override object CreateInstance(Container parent) {
            return new Message() { Template = this, Parent = parent };
        }
    }
}