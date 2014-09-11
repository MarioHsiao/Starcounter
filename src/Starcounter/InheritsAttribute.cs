using System;

namespace Starcounter {
    /// <summary>
    /// Custom attribute that, when applied to a non-transient field or a property
    /// in a database class, instructs Starcounter to treat the tagged target as a
    /// reference to a base type (forming a hierarchy).
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class InheritsAttribute : Attribute {
    }
}