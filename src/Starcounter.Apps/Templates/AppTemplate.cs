

namespace Starcounter.Templates {

    /// <summary>
    /// Defines the properties of an App instance.
    /// </summary>
    public class AppTemplate : ObjTemplate {

        /// <summary>
        /// Creates a new App-instance based on this template.
        /// </summary>
        /// <param name="parent">The parent for the new app</param>
        /// <returns></returns>
        public override object CreateInstance(Container parent) {
            return new App() { Template = this, Parent = parent };
        }
    }
}