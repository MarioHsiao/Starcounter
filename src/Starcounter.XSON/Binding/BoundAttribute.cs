using System;

namespace Starcounter.XSON.Binding {
    /// <summary>
    /// Attribute used to tag generated code for properties in TypedJSON that is explicitly
    /// bound to enable compile-time errors and call-graphs from properties in dataobject.
    /// <code>
    /// // Generated code \\
    /// ...
    /// [Bound(nameof(Person.Name))] 
    /// public string Name { get { ... } set {... } }
    /// ...
    /// </code>
    /// </summary>
    /// <remarks>
    /// The attribute itself contains no logic and is not used runtime.
    /// </remarks>
    public class BoundAttribute : Attribute {
        public BoundAttribute(string bindPath) { }
    }
}
