
using System;

namespace Starcounter {

    /// <summary>
    /// Attribute to allow entity fields to indicate they are mearly synonyms, or "casts",
    /// of an allready defined field, declared in the same class or one of the base-classes
    /// to the class declaring the synonym.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class SynonymousToAttribute : Attribute {

        /// <summary>
        /// Initializes a <see cref="SynonymousToAttribute"/> instance.
        /// </summary>
        public SynonymousToAttribute(string target) {
            if (string.IsNullOrEmpty(target)) {
                throw new ArgumentNullException("target");
            }
            this.Target = target;
        }

        /// <summary>
        /// The name of the field this one is synonymous to.
        /// </summary>
        public string Target { get; set; }
    }
}