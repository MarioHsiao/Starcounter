using System;

namespace Starcounter.Internal {
    /// <summary>
    /// Custom attrbute that is used initially just as a tag set
    /// on assemblies targeting Starcounter to assure the compiler
    /// does not omit adding a reference to the Starcounter assembly,
    /// something that is required for assemblies that are to be
    /// processed by the weaver.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
    public sealed class StarcounterAssemblyAttribute : Attribute {
        /// <summary>
        /// Gets or sets a value dictating if database types are to be qualified
        /// only by their full name. Default is <c>false</c>.
        /// </summary>
        public bool QualifyTypesOnlyByFullNameInQueries { get; set; }
    }
}