

namespace Starcounter.Templates {

    /// <summary>
    /// Defines the schema (properties) for a Puppet object.
    /// </summary>
    public class TApp : TObj {

        /// <summary>
        /// Creates a new Puppet using the schema defined by this template
        /// </summary>
        /// <param name="parent">The parent for the new Puppet (if any)</param>
        /// <returns>The new puppet</returns>
        public override object CreateInstance(Container parent) {
            return new App() { Template = this, Parent = parent };
        }
    }
}