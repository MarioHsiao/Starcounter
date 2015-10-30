
using System;

namespace Starcounter {
    /// <summary>
    /// Custom attribute that, when applied to a field or auto-implemented
    /// property in a database class, makes the field/property transient, 
    /// meaning it will not be stored in the database nor accessible using
    /// SQL.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class TransientAttribute : Attribute {
    }
}