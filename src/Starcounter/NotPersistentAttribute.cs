
using System;

namespace Starcounter {
    /// <summary>
    /// Custom attribute that, when applied to a field in a database
    /// class, makes the field "volatile", meaning it will not be stored
    /// in the database.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class NotPersistentAttribute : Attribute {
    }
}