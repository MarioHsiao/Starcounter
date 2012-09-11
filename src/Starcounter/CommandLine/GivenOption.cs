
namespace Starcounter.CommandLine
{
    /// <summary>
    /// Represents an option specified on the command line.
    /// </summary>
    internal class GivenOption
    {
        /// <summary>
        /// The name by which the option was given.
        /// </summary>
        internal string SpecifiedName;

        /// <summary>
        /// The options value.
        /// </summary>
        internal string Value;

        /// <summary>
        /// The section in where the option was given.
        /// </summary>
        internal CommandLineSection Section;

        /// <summary>
        /// Gets a value indicating if the given option represents
        /// a flag.
        /// </summary>
        internal bool IsFlag
        {
            get { return string.IsNullOrEmpty(this.Value); }
        }
    }
}