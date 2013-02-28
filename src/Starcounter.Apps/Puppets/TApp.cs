

using System;
namespace Starcounter.Templates {

    /// <summary>
    /// Defines the schema (properties) for a Puppet object.
    /// </summary>
    public class TPuppet : TObj {

        /// <summary>
        /// Creates a new Puppet using the schema defined by this template
        /// </summary>
        /// <param name="parent">The parent for the new Puppet (if any)</param>
        /// <returns>The new puppet</returns>
        public override object CreateInstance(Container parent) {
            if (_AppType != null) {
                var p = (Puppet)Activator.CreateInstance(_AppType);
                p.Template = this;
                p.Parent = parent;
                return p;
            }
            return new Puppet() { Template = this, Parent = parent };
        }

        /// <summary>
        /// The .NET type of the instance represented by this template.
        /// </summary>
        /// <value>The type of the instance.</value>
        public override Type InstanceType {
            get {
                if (_AppType == null) {
                    return typeof(Puppet);
                }
                return _AppType;
            }
            set { _AppType = value; }
        }

    }
}