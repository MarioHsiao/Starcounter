
using System;

namespace Starcounter.Hosting {
    /// <summary>
    /// Custom attribute that, when applied to a class, indicates it
    /// is a database class.
    /// </summary>
    /// <remarks>
    /// This class will be moved to the Starcounter root namespace.
    /// It is here to start with, while working with removing the
    /// dependency on the weaver class, since it clashes with the
    /// class with the same name in Sc.Weaver.Schema.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple=false, Inherited=true)]
    public sealed class DatabaseAttribute : Attribute {
    }
}
